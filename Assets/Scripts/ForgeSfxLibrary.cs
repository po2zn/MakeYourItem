using System.Collections.Generic;
using UnityEngine;
public sealed class ForgeSfxLibrary:ScriptableObject {
 public AudioClip click,ui,place,coal,walk,hammer,water,air,seal,upgrade,polish,sparkle;
 public AudioClip frostDrop,frostCooling;
 static ForgeSfxLibrary instance;
 readonly Dictionary<string,AudioClip> sped=new Dictionary<string,AudioClip>();
 public static ForgeSfxLibrary Instance=>instance?instance:instance=Resources.Load<ForgeSfxLibrary>("ForgeSfxLibrary");
 public AudioClip AtSpeed(AudioClip clip,float speed){
  // Level 5 keeps its gameplay timing but shares the level 4 sound and cache entry.
  if(!clip)return null;speed=Mathf.Clamp(speed,1,4);
  if(clip.loadState==AudioDataLoadState.Unloaded)clip.LoadAudioData();
  bool soft=clip==water||clip==polish||clip==sparkle||clip==frostCooling||clip==frostDrop;
  if(speed==1&&!soft)return clip;
  string key=clip.name+"/"+speed.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);
  if(sped.TryGetValue(key,out var result)&&result)return result;
  if(clip.loadState!=AudioDataLoadState.Loaded)return null;
  float[] data;
  if(clip==sparkle)data=SparkleSamples(clip.frequency,speed); // Shorter envelopes, unchanged gentle bell pitches.
  else{
   var original=new float[clip.samples*clip.channels];if(!clip.GetData(original,0))return null;
   // Filter before speeding up, so the output cutoff stays fixed at every audio speed and avoids harsh aliasing.
   if(soft)original=LowPass(original,clip.channels,clip.frequency,(clip==water?1200:1500)/speed);
   data=Resample(original,clip.channels,speed);
   if(soft)for(int i=0;i<data.Length;i++)data[i]*=clip==water?.72f:.65f;
  }
  int frames=data.Length/clip.channels;
  result=AudioClip.Create(key,frames,clip.channels,clip.frequency,false);result.SetData(data,0);sped[key]=result;return result;
 }
 // Resample rather than relying on AudioSource.pitch, which cannot represent 4x/5x.
 public static float[] Resample(float[] original,int channels,float speed){
  if(original==null||channels<1||original.Length<channels||original.Length%channels!=0||float.IsNaN(speed)||float.IsInfinity(speed)||speed<=0)throw new System.ArgumentException("Invalid audio samples or speed");
  int samples=original.Length/channels,frames=System.Math.Max(1,(int)System.Math.Ceiling(samples/speed));var data=new float[frames*channels];
  for(int i=0;i<frames;i++){float position=System.Math.Min(samples-1,i*speed);int a=(int)position,b=System.Math.Min(a+1,samples-1);for(int c=0;c<channels;c++){float from=original[a*channels+c];data[i*channels+c]=from+(original[b*channels+c]-from)*(position-a);}}
  return data;
 }
 // Fourth-order Butterworth low-pass; each channel keeps its own filter history.
 public static float[] LowPass(float[] samples,int channels,int rate,double cutoff){
  if(samples==null||channels<1||samples.Length%channels!=0||rate<=0||cutoff<=0||cutoff>=rate*.5)throw new System.ArgumentException("Invalid low-pass parameters");
  var data=(float[])samples.Clone();
  foreach(double q in new[]{.541196100146197,1.306562964876377}){
   double w=2*System.Math.PI*cutoff/rate,cos=System.Math.Cos(w),alpha=System.Math.Sin(w)/(2*q),a0=1+alpha;
   double b0=(1-cos)/2/a0,b1=(1-cos)/a0,b2=b0,a1=-2*cos/a0,a2=(1-alpha)/a0;
   for(int c=0;c<channels;c++){double x1=0,x2=0,y1=0,y2=0;for(int i=c;i<data.Length;i+=channels){double x=data[i],y=b0*x+b1*x1+b2*x2-a1*y1-a2*y2;data[i]=(float)y;x2=x1;x1=x;y2=y1;y1=y;}}
  }
  return data;
 }
 public static float[] SparkleSamples(int rate,float speed){
  if(rate<8000||speed<1||speed>5||float.IsNaN(speed))throw new System.ArgumentException("Invalid sparkle parameters");
  var samples=new float[(int)System.Math.Ceiling(.65*rate/speed)];
  double[] notes={392,493.883,587.33,783.991};
  for(int i=0;i<samples.Length;i++){
   double t=(double)i/rate,sum=0;
   for(int note=0;note<notes.Length;note++){
    double age=t-note*.085/speed;if(age<0)continue;
    double envelope=(1-System.Math.Exp(-age*speed/0.012))*System.Math.Exp(-age*speed/.095);
    sum+=.18*envelope*(System.Math.Sin(2*System.Math.PI*notes[note]*age)+.035*System.Math.Sin(4*System.Math.PI*notes[note]*age));
   }
   double fade=System.Math.Min(1,(samples.Length-1-i)/(rate*.025/speed));samples[i]=(float)(sum*System.Math.Max(0,fade));
  }
  return samples;
 }
 public static float[] FrostSamples(int rate,bool drop){
  var data=new float[(int)(rate*(drop?.34f:.8f))];uint seed=83117;double filtered=0;
  for(int i=0;i<data.Length;i++){double t=(double)i/rate;seed=seed*1664525u+1013904223u;double noise=(seed>>8)/(double)0xffffff*2-1;filtered+=.13*(noise-filtered);
   double fade=System.Math.Min(1,i/(rate*.012))*System.Math.Min(1,(data.Length-1-i)/(rate*.035));
   double value=drop?(.24*System.Math.Sin(2*System.Math.PI*740*t)*System.Math.Exp(-t*18)+.12*System.Math.Sin(2*System.Math.PI*1174*t)*System.Math.Exp(-t*26)+filtered*.38*System.Math.Exp(-t*12)):filtered*.65*(.8+.2*System.Math.Sin(2*System.Math.PI*4*t))+.035*System.Math.Sin(2*System.Math.PI*(180*t+16*System.Math.Sin(t*7)));
   data[i]=(float)(value*fade);
  }return data;
 }
 void OnDisable(){foreach(var clip in sped.Values)if(clip)Destroy(clip);sped.Clear();if(instance==this)instance=null;}
}
