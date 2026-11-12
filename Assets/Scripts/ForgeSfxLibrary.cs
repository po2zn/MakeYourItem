using System.Collections.Generic;
using UnityEngine;
public sealed class ForgeSfxLibrary:ScriptableObject {
 public AudioClip click,ui,place,coal,walk,hammer,water,air;
 static ForgeSfxLibrary instance;
 readonly Dictionary<string,AudioClip> sped=new Dictionary<string,AudioClip>();
 public static ForgeSfxLibrary Instance=>instance?instance:instance=Resources.Load<ForgeSfxLibrary>("ForgeSfxLibrary");
 public AudioClip AtSpeed(AudioClip clip,float speed){
  if(!clip)return null;speed=Mathf.Clamp(speed,1,5);
  if(clip.loadState==AudioDataLoadState.Unloaded)clip.LoadAudioData();
  if(speed==1)return clip;
  string key=clip.name+"/"+speed.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);
  if(sped.TryGetValue(key,out var result)&&result)return result;
  if(clip.loadState!=AudioDataLoadState.Loaded)return null;
  var original=new float[clip.samples*clip.channels];if(!clip.GetData(original,0))return null;
  var data=Resample(original,clip.channels,speed);int frames=data.Length/clip.channels;
  result=AudioClip.Create(key,frames,clip.channels,clip.frequency,false);result.SetData(data,0);sped[key]=result;return result;
 }
 // Resample rather than relying on AudioSource.pitch, which cannot represent 4x/5x.
 public static float[] Resample(float[] original,int channels,float speed){
  if(original==null||channels<1||original.Length<channels||original.Length%channels!=0||float.IsNaN(speed)||float.IsInfinity(speed)||speed<=0)throw new System.ArgumentException("Invalid audio samples or speed");
  int samples=original.Length/channels,frames=System.Math.Max(1,(int)System.Math.Ceiling(samples/speed));var data=new float[frames*channels];
  for(int i=0;i<frames;i++){float position=System.Math.Min(samples-1,i*speed);int a=(int)position,b=System.Math.Min(a+1,samples-1);for(int c=0;c<channels;c++){float from=original[a*channels+c];data[i*channels+c]=from+(original[b*channels+c]-from)*(position-a);}}
  return data;
 }
 void OnDisable(){foreach(var clip in sped.Values)if(clip)Destroy(clip);sped.Clear();if(instance==this)instance=null;}
}
