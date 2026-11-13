using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEditor;
public static class ForgeCodeFeedbackChecks {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void RunBatch(){
 string[] keys={ForgeCodePreferences.SoundKey,ForgeCodePreferences.ForgeKey,ForgeAudioSettings.MasterKey,ForgeAudioSettings.MusicKey,ForgeAudioSettings.SfxKey};
 bool[] exists=keys.Select(PlayerPrefs.HasKey).ToArray();int[] ints=keys.Select(k=>PlayerPrefs.GetInt(k)).ToArray();float[] floats=keys.Select(k=>PlayerPrefs.GetFloat(k)).ToArray();
 GameObject root=null,camRoot=null;int exit=0;
 try{
 PlayerPrefs.DeleteKey(keys[0]);PlayerPrefs.DeleteKey(keys[1]);ForgeCodePreferences.Load();Check(ForgeCodePreferences.TypingSound&&!ForgeCodePreferences.ForgeCode,"defaults");
 ForgeCodePreferences.SetTypingSound(false);ForgeCodePreferences.SetForgeCode(true);ForgeCodePreferences.Load();Check(!ForgeCodePreferences.TypingSound&&ForgeCodePreferences.ForgeCode,"independent persisted toggles");
 var samples=ForgeSoundEffects.TypingSamples(44100);Check(samples.Length>0&&samples.All(v=>!float.IsNaN(v)&&Math.Abs(v)<1)&&samples.Max(v=>Math.Abs(v))>.02&&samples[0]==0&&Math.Abs(samples[samples.Length-1])<.000001,"valid faded keyboard waveform");
 root=new GameObject("Code feedback test",typeof(RectTransform));var input=root.AddComponent<ForgeCodeInput>();var label=new GameObject("Text",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));label.transform.SetParent(root.transform,false);input.textComponent=label.GetComponent<Text>();input.textComponent.font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);
 input.text="abc";Check(!root.GetComponent<ForgeCodeFeedback>(),"programmatic edit produces no feedback");
 input.readOnly=true;ForgeCodeFeedback.Edited(input,"");Check(!root.GetComponent<ForgeCodeFeedback>(),"readonly edit ignored");input.readOnly=false;
 ForgeCodeFeedback.Edited(input,"ab");var effect=root.GetComponent<ForgeCodeFeedback>();Check(effect&&effect.Active,"real changed edit activates effect");
 var original=new Color(.3f,.8f,.6f,.75f);Check(effect.Shade(0,original,0)==original,"unchanged glyph preserved");Check(effect.Shade(2,original,0)!=original&&effect.Shade(2,original,0).a==original.a,"hot glyph and alpha preserved");
 typeof(ForgeCodeFeedback).GetField("began",F).SetValue(effect,Time.unscaledTime-.32f);var steel=effect.Shade(2,original,0);Check(steel.b>steel.r&&steel.g>steel.r,"metallic cooling phase");
 typeof(ForgeCodeFeedback).GetField("began",F).SetValue(effect,Time.unscaledTime-1);Check(effect.Shade(2,original,0)==original&&effect.Offset==Vector2.zero,"full syntax and offset restoration");
 Check(input.text=="abc","source unchanged");Check(ForgeCodeFeedback.ChangedRange("abc","ab")==new Vector2Int(1,2),"backspace range");Check(ForgeCodeFeedback.ChangedRange("",new string('a',1000))==new Vector2Int(904,1000),"paste is bounded");Check(ForgeCodeFeedback.ChangedRange("가","각")==new Vector2Int(0,1),"Korean replacement range");
 camRoot=new GameObject("Camera test",typeof(Camera));var cam=camRoot.GetComponent<Camera>();cam.orthographic=true;cam.transform.position=new Vector3(3,4,-10);var pose=cam.transform.position;ForgeCodeCameraShake.Pulse(cam);var shake=camRoot.GetComponent<ForgeCodeCameraShake>();
 typeof(ForgeCodeCameraShake).GetMethod("Begin",F).Invoke(shake,new object[]{default(ScriptableRenderContext),cam});Check(cam.transform.position!=pose,"render shake applied");typeof(ForgeCodeCameraShake).GetMethod("End",F).Invoke(shake,new object[]{default(ScriptableRenderContext),cam});Check(cam.transform.position==pose,"exact camera restoration");
 ForgeCodePreferences.SetForgeCode(false);Check(!effect.Active&&effect.Shade(2,original,0)==original,"disable restores glyphs immediately");Check(cam.transform.position==pose,"disable preserves camera");for(int i=0;i<100;i++)Check(ForgeCodeCameraShake.PixelOffset(i*.002f).magnitude<1.56f,"bounded camera displacement");
 var soundRoot=new GameObject("Code audio test");var sound=soundRoot.AddComponent<ForgeSoundEffects>();typeof(ForgeSoundEffects).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,sound);typeof(ForgeSoundEffects).GetField("source",F).SetValue(sound,sound.GetComponent<AudioSource>());typeof(ForgeSoundEffects).GetMethod("OnEnable",F).Invoke(sound,null);ForgeAudioSettings.SetVolumes(1,1,.4f);ForgeCodePreferences.SetTypingSound(true);ForgeSoundEffects.CodeTyped();var player=UnityEngine.Object.FindAnyObjectByType<ForgeSoundEffects>();var source=(AudioSource)typeof(ForgeSoundEffects).GetField("typingSource",F).GetValue(player);Check(source&&source.clip&&Mathf.Approximately(source.volume,.18f),"dedicated SFX volume");Check(source.isPlaying,"native AudioSource playback");ForgeAudioSettings.SetVolumes(1,1,0);Check(source.volume==0,"SFX mute immediate");ForgeCodePreferences.SetTypingSound(false);Check(!source.isPlaying,"toggle stops audio");
 var font=input.textComponent.font;var style=new GUIStyle{font=font,fontSize=18,wordWrap=true};foreach(int width in new[]{640,800,1280,1920,3840}){float panel=Mathf.Min(840,width-68),control=panel*.72f;foreach(string help in new[]{"키 입력 소리 · 효과음(SFX) 음량 적용","입력한 코드가 붉게 달아올랐다가 금속빛으로 식으며, 화면이 미세하게 흔들립니다."}){float h=ForgeTextLayout.Height(help,style,control);Check(h>=style.CalcHeight(new GUIContent(help),control),"full wrapped text height "+width);}Check(style.CalcHeight(new GUIContent("코드 입력 소리"),panel*.28f-16)<=48,"row label fits "+width);}
 File.WriteAllText("Artifacts/CodeFeedback/results.txt","PASS compile; defaults/persistence/independent switches; generated waveform; native AudioSource playback and SFX mute; readonly and programmatic input ignored; bounded paste/Korean replacement; hot/metal/syntax restoration; source preserved; render camera restoration and bounded shake; settings wrapped text metrics at 640/800/1280/1920/3840. Live IME, visual screenshots and listening not covered.");Debug.Log("CODE_FEEDBACK_PASS");
 }catch(Exception e){exit=1;Debug.LogException(e);File.WriteAllText("Artifacts/CodeFeedback/failure.txt",e.ToString());}
 finally{if(root)UnityEngine.Object.DestroyImmediate(root);if(camRoot)UnityEngine.Object.DestroyImmediate(camRoot);for(int i=0;i<keys.Length;i++){if(!exists[i])PlayerPrefs.DeleteKey(keys[i]);else if(i<2)PlayerPrefs.SetInt(keys[i],ints[i]);else PlayerPrefs.SetFloat(keys[i],floats[i]);}PlayerPrefs.Save();ForgeCodePreferences.Load();ForgeAudioSettings.Load();}
 EditorApplication.Exit(exit);
 }
}
