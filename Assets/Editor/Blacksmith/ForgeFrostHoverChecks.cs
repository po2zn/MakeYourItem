using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ForgeFrostHoverChecks
{
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){
  ForgeFrostSoundSetup.Build();var lib=ForgeSfxLibrary.Instance;
  Require(lib.frostDrop&&lib.frostCooling,"frost sound assets assigned");foreach(var clip in new[]{lib.frostDrop,lib.frostCooling}){clip.LoadAudioData();var samples=new float[clip.samples];clip.GetData(samples,0);Require(samples.All(x=>float.IsFinite(x)&&Math.Abs(x)<.8f)&&samples.Any(x=>Math.Abs(x)>.02f),"audible unclipped frost clip");Require(lib.AtSpeed(clip,4)==lib.AtSpeed(clip,5),"5x shares 4x audio");}
  foreach(int frames in new[]{1,2,60,144}){int count=0;float previous=-1;for(int i=0;i<=frames;i++){float now=(float)i/frames;if(ForgeActionAudio.Crossed(previous,now,ForgeActionAudio.FrostDropTime))count++;previous=now;}Require(count==1,"exactly one drop cue");}
  Require(ForgeActionAudio.IsFrost(new ForgeMvp.Step{process=4,consumed=new System.Collections.Generic.Dictionary<int,int>{{8,1}}})&&!ForgeActionAudio.IsFrost(new ForgeMvp.Step{process=4,consumed=new System.Collections.Generic.Dictionary<int,int>{{3,1}}}),"frost differs from water");
  Directory.CreateDirectory("Artifacts/FrostHover");var cameraRoot=new GameObject("Frost hover camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.13f,.1f);
  var owner=new GameObject("Hover owner");owner.SetActive(false);var game=owner.AddComponent<ForgeMvp>();var motion=owner.AddComponent<ForgeCraftMotion>();
  var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();
  try{
   ui.Bind(game);ui.gameObject.SetActive(true);ui.GetComponent<ForgeLegacyLayout>().enabled=false;ui.GetComponent<CanvasScaler>().enabled=false;foreach(Transform child in ui.transform)child.gameObject.SetActive(false);ui.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.canvas.worldCamera=camera;ui.canvas.planeDistance=10;
   var popup=ForgeFrostStationHover.Create(ui);var report=new System.Text.StringBuilder();
   foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160),new Vector2Int(640,480)})foreach(int count in new[]{4,8}){
    var rt=new RenderTexture(size.x,size.y,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();
    try{
     var graph=ForgeCoolingGraph.Create(55,new ForgeCoolingSettings{points=count});var piece=new ForgeMvp.Piece{id=17,material=7,heat=900,shaped=true,data=new ForgeItemData{cooling=graph}};motion.StationCoolingView.Select(piece);
     motion.StationCoolingView.Path=graph.DefaultPath().ToArray();motion.StationCoolingView.Progress=.4f;
     foreach(var pointer in new[]{new Vector2(0,0),new Vector2(size.x,size.y)}){popup.Hide();popup.Show(ui,motion,15,pointer,2147483647);Canvas.ForceUpdateCanvases();var bounds=((RectTransform)ui.transform).rect;var corners=new Vector3[4];((RectTransform)popup.transform).GetWorldCorners(corners);foreach(var corner in corners)Require(bounds.Contains(ui.transform.InverseTransformPoint(corner)),"popup on screen "+size+" bounds="+bounds+" corner="+ui.transform.InverseTransformPoint(corner));foreach(var text in popup.GetComponentsInChildren<Text>())Require(text.preferredHeight<=text.rectTransform.rect.height+2,"text fits "+text.name+" "+size);}
     Capture(camera,rt,size.x+"-"+count);motion.StationCoolingView.Clear();popup.Hide();popup.Show(ui,motion,0,Vector2.zero,0);Require(popup.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("칼날이 없습니다")),"empty station guidance");popup.Hide();Require(!popup.gameObject.activeSelf,"leave hides preview");report.AppendLine("PASS "+size+" nodes="+count+" edge positioning/text metrics/empty state");
    }finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);}
   }
   File.WriteAllText("Artifacts/FrostHover/results.txt",report.ToString());Debug.Log("FROST_HOVER_AUDIO_PASS");
  }finally{UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(cameraRoot);}
 }
 static void Capture(Camera camera,RenderTexture rt,string name){camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes("Artifacts/FrostHover/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
 public static void RunBatch(){try{Run();EditorApplication.Exit(0);}catch(Exception e){Directory.CreateDirectory("Artifacts/FrostHover");File.WriteAllText("Artifacts/FrostHover/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}
