using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ForgeFrostRevealChecks
{
 const string Key="ForgeFrostRevealChecks.Pending";
 const string Folder="Artifacts/FrostReveal";
 static double started;
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 [InitializeOnLoadMethod] static void Resume(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);Start();}};}
 public static void RunBatch(){Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
 static void Start(){Application.runInBackground=true;Time.timeScale=1;Time.captureDeltaTime=1f/60;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watchdog;var host=new GameObject("Model checks").AddComponent<ForgeCraftMotion>();host.StartCoroutine(Guard(Run()));}
 static void Watchdog(){if(EditorApplication.timeSinceStartup-started>180)Fail(new Exception("Model test timeout"));}
 static void Fail(Exception e){File.WriteAllText(Folder+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
 static IEnumerator Guard(IEnumerator iterator){var stack=new Stack<IEnumerator>();stack.Push(iterator);while(stack.Count>0){var top=stack.Peek();bool more=false;try{more=top.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more){(top as IDisposable)?.Dispose();stack.Pop();continue;}if(top.Current is IEnumerator nested)stack.Push(nested);else yield return top.Current;}Debug.Log("FROST_REVEAL_PASS "+DateTime.UtcNow.ToString("O"));File.WriteAllText(Folder+"/results.txt","PASS construction and completion, SFX, repeated unlock, hover bounds, notice cleanup, disable cleanup, shader and guide text.\n");EditorApplication.Exit(0);}
 static void Capture(Camera camera,string name){var rt=new RenderTexture(1280,900,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,900),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(image);rt.Release();UnityEngine.Object.Destroy(rt);}

 static IEnumerator Run(){
 var root=new GameObject("Frost reveal test");var v=root.AddComponent<ForgeWorkshopVisuals>();v.Initialize(Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets"));v.SetFrostUnlocked(true);v.SetFrostUnlocked(true);var reveal=root.GetComponentInChildren<ForgeFrostStationReveal>();Require(reveal&&reveal.Building&&reveal.Attention,"construction starts on first unlock");Require(root.GetComponentsInChildren<ForgeFrostStationReveal>().Length==1,"repeated unlock does not duplicate");Require(v.HitFrostStation(new Ray(v.FrostWorkPosition+Vector3.up*3,Vector3.down),out _),"hover bounds valid during assembly");
 var shader=Resources.Load<Shader>("ForgeFrostOutline");Require(shader&&!UnityEditor.ShaderUtil.ShaderHasError(shader),"outline shader compiles");
 var camera=new GameObject("Frost capture").AddComponent<Camera>();camera.gameObject.AddComponent<AudioListener>();camera.orthographic=true;camera.orthographicSize=1.15f;camera.transform.position=v.FrostWorkPosition+new Vector3(2,1.5f,-3);camera.transform.LookAt(v.FrostWorkPosition);camera.backgroundColor=new Color(.1f,.13f,.18f);camera.clearFlags=CameraClearFlags.SolidColor;
 var lamp=new GameObject("Light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=1.6f;lamp.transform.rotation=Quaternion.Euler(45,-30,0);
 yield return new WaitForSeconds(.45f);Capture(camera,"assembling");var audio=reveal.GetComponent<AudioSource>();Require(audio&&audio.clip==ForgeSfxLibrary.Instance.hammer,"hammer cue assigned");yield return new WaitForSeconds(1.1f);Require(!reveal.Building&&reveal.Attention,"assembled and hint continues");Capture(camera,"yellow-outline");v.SetFrostUnlocked(true);Require(!reveal.Building,"no replay on refresh");reveal.Acknowledge();Require(!reveal.Attention&&reveal.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Yellow hover outline").All(r=>!r.enabled),"hover dismisses hint");v.SetFrostUnlocked(false);Require(!audio.isPlaying,"hide stops sound");v.SetFrostUnlocked(true);Require(!reveal.Building&&!reveal.Attention,"reenable preserves notice and final pose");Require(ForgeMaterialGuide.Find(8).Description.Contains(ForgeCoolingGuide.MinimumRoute),"real material guide contains minimum route objective");
 UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(lamp.gameObject);
 }
}
