using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ForgeFinishingMotionChecks
{
 const string Key="ForgeFinishingMotionChecks.Pending";
 const string Folder="Artifacts/FinishingMotion";
 static double started;
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 [InitializeOnLoadMethod] static void Resume(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);Start();}};}
 public static void RunBatch(){Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
 static void Start(){Application.runInBackground=true;Time.timeScale=1;Time.captureDeltaTime=1f/60;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watchdog;var host=new GameObject("Model checks").AddComponent<ForgeCraftMotion>();host.StartCoroutine(Guard(Run()));}
 static void Watchdog(){if(EditorApplication.timeSinceStartup-started>180)Fail(new Exception("Model test timeout"));}
 static void Fail(Exception e){File.WriteAllText(Folder+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
 static IEnumerator Guard(IEnumerator iterator){var stack=new Stack<IEnumerator>();stack.Push(iterator);while(stack.Count>0){var top=stack.Peek();bool more=false;try{more=top.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more){(top as IDisposable)?.Dispose();stack.Pop();continue;}if(top.Current is IEnumerator nested)stack.Push(nested);else yield return top.Current;}Debug.Log("FINISHING_MOTION_PASS "+DateTime.UtcNow.ToString("O"));File.WriteAllText(Folder+"/results.txt","PASS real Play Mode: forge/air cooling/handle/curse, failed curse, cancellation, finite poses and rendered captures.\n");EditorApplication.Exit(0);}
 static void Capture(Camera camera,string name){var rt=new RenderTexture(1280,900,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,900),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(image);rt.Release();UnityEngine.Object.Destroy(rt);}

 static IEnumerator Run(){
 var root=new GameObject("Finishing animation checks");var v=root.AddComponent<ForgeWorkshopVisuals>();v.Initialize(Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets"));
 Transform Find(string n)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
 var camera=new GameObject("Motion camera").AddComponent<Camera>();camera.gameObject.AddComponent<AudioListener>();camera.orthographic=true;camera.orthographicSize=1.65f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.19f,.23f);
 var light=new GameObject("Motion light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(40,-25,0);
 string report="";
 foreach(int process in new[]{3,4,5,666,666}){
 bool destroyed=process==666&&report.Contains("process=666");var step=new ForgeMvp.Step{process=process,pieceId=1,material=1,shaped=true,heat=process==3?700:0,duration=1.6f,data=new ForgeItemData{handle=process==5||process==666?2:0,destroyed=destroyed}};
 bool star=false,fitBefore=false,fitAfter=false;int shots=0;float elapsed=0,minFront=10,maxGrip=0;var action=v.Play(step);
 try{while(action.MoveNext()){
 float t=elapsed/step.duration;elapsed+=Time.deltaTime;
 var smith=Find("Blacksmith");var piece=Find("Workpiece 1");
 if(t>.45f){minFront=Mathf.Min(minFront,Vector3.Dot(Find("RightHandSocket").position-smith.position,smith.forward));if(process==4)maxGrip=Mathf.Max(maxGrip,Vector3.Distance(Find("LeftHandSocket").position,piece.position));}
 foreach(var bone in smith.GetComponentsInChildren<Transform>())Require(float.IsFinite(bone.position.x)&&float.IsFinite(bone.position.y),"finite pose");
 star|=root.GetComponentsInChildren<Transform>().Any(x=>x.name=="Curse red star");if(process==5&&t>.45f&&t<.58f)fitBefore|=!Find("Grip").gameObject.activeSelf;if(process==5&&t>.65f)fitAfter|=Find("Grip").gameObject.activeSelf;
 if(shots<3&&t>.48f+shots*.17f){camera.transform.position=smith.position+new Vector3(3,2,-4);camera.transform.LookAt(smith.position+Vector3.up*.95f);Capture(camera,process+"-"+destroyed+"-"+shots);camera.transform.position=smith.position+new Vector3(4,1.4f,0);camera.transform.LookAt(smith.position+Vector3.up*.95f);Capture(camera,process+"-"+destroyed+"-side-"+shots);shots++;}yield return action.Current;
 }}finally{(action as IDisposable)?.Dispose();}
 if(process==4)Require(maxGrip<.025f,"air cooled sword stays in hand: "+maxGrip);if(process==666)Require(star,"curse visible even before destruction");if(process==5)Require(fitBefore&&fitAfter,"handle fitted at sound contact");
 Require(!root.GetComponentsInChildren<Transform>().Any(x=>x.name=="Curse red star"),"curse removed at completion");if(destroyed)Require(!Find("Workpiece 1").gameObject.activeSelf,"destroyed sword hidden");
 report+="process="+process+" destroyed="+destroyed+" rightFront="+minFront+" coolingGripError="+maxGrip+"\n";v.Cancel();
 }
 var cancel=v.Play(new ForgeMvp.Step{process=666,pieceId=2,material=1,shaped=true,duration=1,data=new ForgeItemData()});bool seen=false;try{while(cancel.MoveNext()){if(root.GetComponentsInChildren<Transform>().Any(x=>x.name=="Curse red star")){seen=true;v.Cancel();break;}yield return cancel.Current;}}finally{(cancel as IDisposable)?.Dispose();}Require(seen&&!root.GetComponentsInChildren<Transform>().Any(x=>x.name=="Curse red star"),"cancel cleans star");File.WriteAllText(Folder+"/metrics.txt",report);
 UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(light.gameObject);
 }
}
