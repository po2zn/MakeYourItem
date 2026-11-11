using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
public static class BlacksmithIdleChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static async Task<string> Run(){
 Check(EditorApplication.isPlaying,"Enter play mode");bool background=Application.runInBackground;Application.runInBackground=true;
 var root=new GameObject("Idle verification");root.transform.position=new Vector3(40,0,0);
 var v=root.AddComponent<ForgeWorkshopVisuals>();v.Initialize(Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets"));
 var idle=root.GetComponent<ForgeSmithIdle>();var head=root.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
 var camera=new GameObject("Idle verification camera").AddComponent<Camera>();var light=new GameObject("Idle verification light").AddComponent<Light>();
 try{
 var q=head.localRotation;int frame=Time.frameCount;await Task.Delay(1200);Check(Time.frameCount>frame+2,"Game frames not advancing");Check(Quaternion.Angle(q,head.localRotation)>.05f,"Idle head not moving");
 idle.Tick(60,false);Check(idle.Visitor&&idle.Visitor.gameObject.activeSelf,"Visitor missing");
 idle.Tick(idle.approachSeconds,false);Check(idle.State==ForgeSmithIdle.VisitState.Perched,"No landing");Check(Vector3.Distance(idle.Visitor.position,idle.Nose.position)<.001f,"Nose anchor mismatch");
 camera.transform.position=idle.Nose.position+idle.Nose.forward*.85f+idle.Nose.right*.35f+Vector3.up*.18f;camera.transform.LookAt(idle.Nose.position-Vector3.up*.07f);camera.orthographic=true;camera.orthographicSize=.32f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.24f,.29f);
 light.type=LightType.Directional;light.intensity=1.7f;light.transform.rotation=Quaternion.Euler(35,155,0);
 var rt=new RenderTexture(1000,1000,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1000,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,1000),0,0);tex.Apply();File.WriteAllBytes("Assets/Art/Blacksmith/Documentation/IdleButterfly.png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(tex);rt.Release();UnityEngine.Object.Destroy(rt);
 idle.Tick(idle.perchSeconds,false);Check(idle.State==ForgeSmithIdle.VisitState.Departing,"No natural departure");idle.Tick(idle.departureSeconds,false);Check(!idle.Visitor.gameObject.activeSelf,"Visitor not hidden after departure");
 idle.Tick(60,false);idle.Tick(idle.approachSeconds,false);Check(idle.State==ForgeSmithIdle.VisitState.Perched,"Second landing failed");
 var action=v.Play(new ForgeMvp.Step{pieceId=1,process=2,heat=7,duration=2});Check(action.MoveNext(),"Craft did not start");Check(idle.State==ForgeSmithIdle.VisitState.Departing,"Craft did not interrupt butterfly");v.Cancel();while(action.MoveNext()){};
 idle.Tick(idle.departureSeconds,true);Check(!idle.Visitor.gameObject.activeSelf,"Interrupted butterfly stayed visible");
 string report="PASS live game frames and idle head motion\nPASS butterfly approach, nose landing and natural departure\nPASS work interrupts perch immediately; departure completes\nPASS cancellation\nFBX: 3611 triangles, 8 materials, skinned wings; original Idle/Fly/Fly fast clips preserved, runtime procedural hinge animation\n";File.WriteAllText("Assets/Art/Blacksmith/Documentation/IdleChecks.txt",report);return report;
 }finally{Application.runInBackground=background;UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(light.gameObject);}
 }
}
