using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class SmithMotionChecks
{
 public static async Task<string> Run(){
 if(!EditorApplication.isPlaying)throw new Exception("Play mode required");bool background=Application.runInBackground;Application.runInBackground=true;
 var root=new GameObject("Motion anatomy checks");var v=root.AddComponent<ForgeWorkshopVisuals>();v.Initialize(Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets"));root.transform.position=new Vector3(30,0,0);
 Transform Find(string n)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
 var smith=Find("Blacksmith");var camera=new GameObject("Motion camera").AddComponent<Camera>();camera.transform.position=new Vector3(35,3.5f,-5);camera.transform.LookAt(new Vector3(32.2f,.9f,1));camera.orthographic=true;camera.orthographicSize=2.7f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.27f,.32f);
 var light=new GameObject("Motion light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.transform.rotation=Quaternion.Euler(40,-25,0);
 string report="";
 try{
 var eval=ForgeMvp.Evaluate("p = 1(1)\n2(p, 7)\n3(p)\n4(p, 3)\n2(p, 7)\n3(p)\n4(p, 0)",100,10,materialStocks:new Dictionary<int,long>{{3,10}});
 var forgeRenderers=Find("Forge").GetComponentsInChildren<Renderer>();var forgeBounds=forgeRenderers[0].bounds;foreach(var r in forgeRenderers)forgeBounds.Encapsulate(r.bounds);
 int index=0;foreach(var step in eval.steps){step.duration=2;bool done=false;Exception failure=null;float elapsed=0,maxGripError=0,minFront=10,minFoot=10,minForgeClearance=10,maxHammerError=0;int shots=0;
 IEnumerator Run(){var action=v.Play(step);try{while(action.MoveNext()){
 elapsed+=Time.deltaTime;
 var onPlane=smith.position;onPlane.y=forgeBounds.center.y;minForgeClearance=Mathf.Min(minForgeClearance,Vector3.Distance(onPlane,forgeBounds.ClosestPoint(onPlane)));
 if(elapsed>step.duration*.46f){
 var piece=Find("Workpiece "+step.pieceId);var socket=Find("LeftHandSocket");var expected=piece.position+(step.process==2?new Vector3(0,.12f,-.4f):Vector3.zero);
 maxGripError=Mathf.Max(maxGripError,Vector3.Distance(socket.position,expected));minFront=Mathf.Min(minFront,Vector3.Dot(socket.position-smith.position,smith.forward));
 minFoot=Mathf.Min(minFoot,Find("LeftFoot").position.y,Find("RightFoot").position.y);
 if(step.process==3){float cycle=Mathf.Repeat((elapsed-Time.deltaTime-step.duration*.4f)/(step.duration*.6f)*2,1);float lift=cycle<.65f?Mathf.SmoothStep(0,1,cycle/.65f):1-Mathf.SmoothStep(0,1,(cycle-.65f)/.35f);var expectedHead=Find("StrikeTarget").position+Vector3.up*(.035f+lift*.33f);maxHammerError=Mathf.Max(maxHammerError,Vector3.Distance(Find("HeldHammer").TransformPoint(new Vector3(.14f,.28f,0)),expectedHead));}
 }
 foreach(var t in smith.GetComponentsInChildren<Transform>())if(!float.IsFinite(t.position.x))throw new Exception("Nonfinite "+t.name);
 if(shots<3&&elapsed>step.duration*(.5f+shots*.2f)){Capture(camera,"Motion_"+index+"_"+step.process+"_"+shots);shots++;}
 yield return action.Current;
 }}finally{(action as IDisposable)?.Dispose();done=true;}}
 IEnumerator Guard(){var run=Run();while(true){bool more=false;try{more=run.MoveNext();}catch(Exception e){failure=e;done=true;}if(!more)break;yield return run.Current;}}
 var coroutine=v.StartCoroutine(Guard());float begin=Time.realtimeSinceStartup;while(!done){if(Time.realtimeSinceStartup-begin>15){v.StopCoroutine(coroutine);throw new Exception("timeout");}await Task.Delay(20);}if(failure!=null)throw failure;
 report+="step="+step.process+" gripError="+maxGripError.ToString("F4")+" front="+minFront.ToString("F4")+" foot="+minFoot.ToString("F4")+" forgeClearance="+minForgeClearance.ToString("F4")+" hammerError="+maxHammerError.ToString("F4")+" position="+smith.position.ToString("F3")+"\n";
 if(maxGripError>.025f||minFront<0||minFoot<.10f||minForgeClearance<.28f||maxHammerError>.025f)throw new Exception("Anatomy/clearance threshold failed: "+report);
 index++;
 }
 File.WriteAllText("Assets/Art/Blacksmith/Documentation/MotionChecks.txt",report);return report;
 }finally{Application.runInBackground=background;UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(light.gameObject);}
 }
 static void Capture(Camera camera,string name){var rt=new RenderTexture(1280,900,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,900),0,0);tex.Apply();File.WriteAllBytes("Assets/Art/Blacksmith/Documentation/"+name+".png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(tex);rt.Release();UnityEngine.Object.Destroy(rt);}
}
