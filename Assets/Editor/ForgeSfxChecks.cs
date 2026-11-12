using System;
using System.Linq;
public static class ForgeSfxChecks {
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 public static string Run(){
  foreach(int frames in new[]{1,2,7,60,144}){int hits=0;float last=-1;for(int i=0;i<=frames;i++){float t=(float)i/frames;hits+=ForgeActionAudio.HammerHits(last,t);last=t;}hits+=ForgeActionAudio.HammerHits(last,1);Require(hits==2,"exactly two hammer contacts at frame count "+frames);}
  Require(!ForgeActionAudio.Crossed(.64f,.8f,.64f)&&ForgeActionAudio.Crossed(.2f,.9f,.64f),"coal cue skipped-frame/no-repeat");
  float[] stereo=Enumerable.Range(0,20).SelectMany(i=>new[]{(float)i,(float)-i}).ToArray();
  foreach(float speed in new[]{1f,1.5f,2f,3f,4f,5f}){var data=ForgeSfxLibrary.Resample(stereo,2,speed);Require(data.Length==2*(int)Math.Ceiling(20/speed),"sound duration at "+speed);for(int i=0;i<data.Length/2;i++){float expected=Math.Min(19,i*speed);Require(Math.Abs(data[i*2]-expected)<.001f&&Math.Abs(data[i*2+1]+expected)<.001f,"channel preservation at "+speed);}}
  for(int level=1;level<=5;level++){var e=BlueprintRuntime.Evaluate("222("+level+")\n8(1000)\nr=1(1)\n2(r)\n3(r)\n4(r,3)",0,20,"audio test",1);Require(e.error==null,e.error);float speed=level==1?1.5f:level;Require(e.steps.Where(s=>s.process>0).All(s=>Math.Abs(s.workSpeed-speed)<.001f),"all actions preserve worker speed "+level);}
  return "PASS SFX: frame-independent contacts, coal cue, stereo resampling at 1–5x, worker speed receipts";
 }
}
