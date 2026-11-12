using System;
using System.Linq;
public static class ForgeSfxChecks {
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static double Rms(float[] data,int start=0){double sum=0;for(int i=start;i<data.Length;i++)sum+=data[i]*data[i];return Math.Sqrt(sum/Math.Max(1,data.Length-start));}
 static double Tone(float[] data,int rate,double hz){double real=0,imaginary=0;for(int i=0;i<data.Length;i++){double phase=2*Math.PI*hz*i/rate;real+=data[i]*Math.Cos(phase);imaginary+=data[i]*Math.Sin(phase);}return Math.Sqrt(real*real+imaginary*imaginary)/data.Length;}
 public static string Run(){
  const int rate=44100;
  foreach(float speed in new[]{1f,1.5f,2f,3f,4f,5f}){
   var low=Enumerable.Range(0,rate).Select(i=>(float)(.5*Math.Sin(2*Math.PI*(300/speed)*i/rate))).ToArray();
   var high=Enumerable.Range(0,rate).Select(i=>(float)(.5*Math.Sin(2*Math.PI*(4000/speed)*i/rate))).ToArray();
   var softenedLow=ForgeSfxLibrary.Resample(ForgeSfxLibrary.LowPass(low,1,rate,1200/speed),1,speed);
   var softenedHigh=ForgeSfxLibrary.Resample(ForgeSfxLibrary.LowPass(high,1,rate,1200/speed),1,speed);
   Require(Rms(softenedLow,200)>.33,"keep body of sound at "+speed);
   Require(Rms(softenedHigh,200)<.01,"suppress 4kHz after speed conversion at "+speed);
   var bell=ForgeSfxLibrary.SparkleSamples(rate,speed);
   Require(Math.Abs(bell.Length-(int)Math.Ceiling(.65*rate/speed))==0&&bell[0]==0&&bell[bell.Length-1]==0,"sparkle duration and soft endpoints");
   Require(bell.All(x=>!float.IsNaN(x)&&Math.Abs(x)<.8f)&&Rms(bell)>.01,"audible unclipped sparkle");
   Require(Tone(bell,rate,392)>Tone(bell,rate,1960)*5,"sparkle pitch remains low even at 5x");
  }
  var stereoCheck=ForgeSfxLibrary.LowPass(new float[]{1,0,0,0,0,0,0,0},2,rate,1200);
  Require(stereoCheck.Where((x,i)=>i%2==1).All(x=>x==0),"no filter crosstalk");
  var lucky=BlueprintRuntime.Evaluate("r=1(1)\n2(r)\n3(r)\n4(r,0)\n5(r,1)\nr=777(777?(),r)\nr=777(777?(),r)",0,20,"sparkle",1);
  Require(lucky.error==null&&lucky.steps.Count(s=>s.process==777)==1,"successful lucky effect only once per sword");
  foreach(int frames in new[]{1,2,7,60,144}){int hits=0;float last=-1;for(int i=0;i<=frames;i++){float t=(float)i/frames;hits+=ForgeActionAudio.HammerHits(last,t);last=t;}hits+=ForgeActionAudio.HammerHits(last,1);Require(hits==2,"exactly two hammer contacts at frame count "+frames);}
  foreach(int frames in new[]{1,2,7,60,144}){int strokes=0,seals=0;float last=-1;for(int i=0;i<=frames;i++){float t=(float)i/frames;strokes+=ForgeActionAudio.PolishStrokes(last,t);if(ForgeActionAudio.Crossed(last,t,ForgeActionAudio.SealContactTime))seals++;last=t;}strokes+=ForgeActionAudio.PolishStrokes(last,1);Require(strokes==4&&seals==1,"polish four strokes and seal once at frame count "+frames);}
  Require(!ForgeActionAudio.Crossed(.64f,.8f,.64f)&&ForgeActionAudio.Crossed(.2f,.9f,.64f),"coal cue skipped-frame/no-repeat");
  float[] stereo=Enumerable.Range(0,20).SelectMany(i=>new[]{(float)i,(float)-i}).ToArray();
  foreach(float speed in new[]{1f,1.5f,2f,3f,4f,5f}){var data=ForgeSfxLibrary.Resample(stereo,2,speed);Require(data.Length==2*(int)Math.Ceiling(20/speed),"sound duration at "+speed);for(int i=0;i<data.Length/2;i++){float expected=Math.Min(19,i*speed);Require(Math.Abs(data[i*2]-expected)<.001f&&Math.Abs(data[i*2+1]+expected)<.001f,"channel preservation at "+speed);}}
  for(int level=1;level<=5;level++){var e=BlueprintRuntime.Evaluate("222("+level+")\n8(1000)\nr=1(1)\n2(r)\n3(r)\n4(r,3)\n6(r)\n201(r,1)\n203(r)\n202(r,1)",0,20,"audio test",1);Require(e.error==null,e.error);Require(e.steps.Count(s=>s.process==201)==1&&e.steps.Count(s=>s.process==6)==1,"only successful seal/polish actions produce audio receipts");float speed=level==1?1.5f:level;Require(e.steps.Where(s=>s.process>0).All(s=>Math.Abs(s.workSpeed-speed)<.001f),"all actions preserve worker speed "+level);}
  return "PASS SFX: softened frequency response and stable sparkle pitch at 1–5x; frame-independent contacts, coal/seal/polish cues, stereo resampling at 1–5x, worker speed receipts";
 }
}
