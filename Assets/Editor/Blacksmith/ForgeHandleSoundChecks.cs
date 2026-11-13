using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class ForgeHandleSoundChecks {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 public static void RunBatch(){try{
 ForgeHandleSoundSetup.Build();var lib=ForgeSfxLibrary.Instance;Check(lib.handle&&lib.handle.samples>0,"clip assigned");
 var data=ForgeSfxLibrary.HandleSamples(44100);Check(data.All(v=>!float.IsNaN(v)&&Math.Abs(v)<1),"finite unclipped audio");Check(data.Max(v=>Math.Abs(v))>.05&&data[0]==0&&Math.Abs(data[data.Length-1])<.00001,"audible and faded endpoints");
 foreach(float speed in new[]{1f,1.5f,2f,3f,4f,5f}){var clip=lib.AtSpeed(lib.handle,speed);Check(clip&&clip.length>0,"speed clip "+speed);}
 Check(lib.AtSpeed(lib.handle,4)==lib.AtSpeed(lib.handle,5),"4x and 5x same sound");
 foreach(int frames in new[]{1,2,3,8,60}){float previous=-1;int cues=0;for(int i=0;i<=frames;i++){float t=i/(float)frames;if(ForgeActionAudio.Crossed(previous,t,ForgeActionAudio.HandleContactTime))cues++;previous=t;}Check(cues==1,"one cue at "+frames+" frames");}
 var root=new GameObject("Handle audio verification");var audio=root.AddComponent<ForgeActionAudio>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var source=(AudioSource)typeof(ForgeActionAudio).GetField("effects",flags).GetValue(audio);if(!source)typeof(ForgeActionAudio).GetMethod("Awake",flags).Invoke(audio,null);
 source=(AudioSource)typeof(ForgeActionAudio).GetField("effects",flags).GetValue(audio);
 audio.Begin(new ForgeMvp.Step{process=5,workSpeed=1,data=new ForgeItemData{handle=2}});audio.Frame(0,false);audio.Frame(ForgeActionAudio.HandleContactTime,false);Check(source.isPlaying,"actual fitting sound playback");audio.Stop();Check(!source.isPlaying,"stop cancels sound");
 audio.Begin(new ForgeMvp.Step{process=5,workSpeed=5});audio.Finish();Check(source.isPlaying,"skipped final frame still plays cue");audio.Stop();UnityEngine.Object.DestroyImmediate(root);
 var result=BlueprintRuntime.Evaluate("p = 1(1)\n2(p)\n3(p)\n4(p, 0)\n5(p, 2)\n!p",0,1,"handle",1);Check(result.error==null,result.error);Check(result.steps.Count(s=>s.process==5)==1&&result.product.Data.handle==2,"real 5() emits one handle receipt");
 Debug.Log(ForgeSfxChecks.Run());File.WriteAllText("Artifacts/HandleSound/results.txt","PASS assigned PCM clip, audible/unclipped/faded waveform, 1-5x playback, one cue independent of frames, actual AudioSource playback and stop, real 5() receipt, existing SFX regression.");if(File.Exists("Artifacts/HandleSound/failure.txt"))File.Delete("Artifacts/HandleSound/failure.txt");Debug.Log("HANDLE_SOUND_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);File.WriteAllText("Artifacts/HandleSound/failure.txt",e.ToString());EditorApplication.Exit(1);}}
}
