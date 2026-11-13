using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ForgeRuneUIChecks
{
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Capture(Camera camera,RenderTexture target,string name){camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);try{png.ReadPixels(new Rect(0,0,target.width,target.height),0,0);png.Apply();File.WriteAllBytes("Artifacts/Runes/"+name+".png",png.EncodeToPNG());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(png);}}
 public static void Run(){
  var owner=new GameObject("Rune UI owner");owner.SetActive(false);var game=owner.AddComponent<ForgeMvp>();
  var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();
  var host=new GameObject("Rune guide render",typeof(RectTransform),typeof(Canvas));var cameraRoot=new GameObject("Rune guide camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.18f);
  var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  try{
   game.SetHierarchyGame(ui);ui.Bind(game);var guide=(RectTransform)ui.Node("Guide");guide.SetParent(host.transform,false);guide.gameObject.SetActive(true);ForgeLegacyUIFont.Apply(guide);
   var tech=(ForgeTechnology)typeof(ForgeMvp).GetField("technology",flags).GetValue(game);var minimum=guide.GetComponentsInChildren<ForgeUIDrag>(true).First(d=>d.resize).minimum;
   var report=new System.Text.StringBuilder();
   foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160),new Vector2Int(640,480)}){
    var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
    try{foreach(bool english in new[]{false,true})foreach(bool small in new[]{false,true})foreach(var feature in new[]{BlueprintFeature.Rune,BlueprintFeature.GiantRune,BlueprintFeature.GiantRuneTime}){
     tech.Restore(new[]{BlueprintFeature.Rune},english?new[]{ForgeTechnology.EnglishResearch}:Array.Empty<string>());
     int index=Enumerable.Range(0,BlueprintGuide.Entries.Count).Single(i=>BlueprintGuide.Entries[i].Feature==feature);
     game.GameGuideAction("guide-section:"+BlueprintGuide.Entries[index].Section);game.GameGuideAction("guide-entry:"+index);
     guide.anchorMin=guide.anchorMax=guide.pivot=new Vector2(.5f,.5f);guide.anchoredPosition=Vector2.zero;guide.sizeDelta=small?minimum:new Vector2(Mathf.Min(800,size.x-32),Mathf.Min(660,size.y-32));
     for(int pass=0;pass<6;pass++){Canvas.ForceUpdateCanvases();foreach(var example in guide.GetComponentsInChildren<ForgeGuideExample>())typeof(ForgeGuideExample).GetMethod("LateUpdate",flags).Invoke(example,null);foreach(var layout in guide.GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);}
     var body=ui.Node("GuideBody").GetComponent<Text>();Require(body.text.Contains(feature==BlueprintFeature.GiantRune?"48시간":feature==BlueprintFeature.GiantRuneTime?"남은":"재료 1개"),"actual guide rules "+feature);
     foreach(var text in guide.GetComponentsInChildren<Text>())Require(text.preferredHeight<=text.rectTransform.rect.height+2,"clipped "+text.name+" "+size+" minimum="+small+" feature="+feature);
     var scroll=guide.GetComponentsInChildren<ScrollRect>(true).First(x=>x.content&&body.transform.IsChildOf(x.content));
     if(!english)foreach(float pos in new[]{1f,0f}){scroll.verticalNormalizedPosition=pos;Canvas.ForceUpdateCanvases();Capture(camera,target,"guide-"+size.x+"-"+small+"-"+feature+"-"+pos);}
     report.AppendLine("PASS "+size+" English="+english+" minimum="+small+" "+feature+" text metrics and actual content");
    }}finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);}
   }
   File.WriteAllText("Artifacts/Runes/ui-results.txt",report.ToString());
  }finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(cameraRoot);}
  CheckArrival();
 }
 static void CheckArrival(){
  var root=new GameObject("Rune arrival stage");var cameraRoot=new GameObject("Rune arrival camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5.8f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.18f);camera.transform.position=new Vector3(7,9,-10);camera.transform.LookAt(new Vector3(1.2f,3,0));
  var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform,false);ground.transform.position=new Vector3(1.2f,-.12f,0);ground.transform.localScale=new Vector3(10,.2f,10);var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));groundMaterial.color=new Color(.55f,.5f,.4f);ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
  var rune=GameObject.CreatePrimitive(PrimitiveType.Cube);rune.transform.SetParent(root.transform,false);rune.transform.localScale=new Vector3(2.25f,.02f,2.25f);var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=new Color(.5f,.25f,.9f);rune.GetComponent<Renderer>().sharedMaterial=material;var arrival=rune.AddComponent<ForgeGiantRuneArrival>();var floor=new Vector3(1.2f,.02f,0);arrival.Begin(floor);
  var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
  try{foreach(float time in new[]{0f,.75f,1.5f,1.65f,1.8f}){arrival.Sample(time);Require(Mathf.Abs(rune.transform.localPosition.y-floor.y-ForgeGiantRuneArrival.HeightAt(time))<.001f,"arrival height");Capture(camera,target,"arrival-"+time.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));}Require(!arrival.Playing&&Vector3.Distance(rune.transform.localPosition,floor)<.001f,"landed once");Require(!root.transform.Find("거대한 룬 착지 그림자").gameObject.activeSelf,"shadow cleaned after landing");}
  finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(groundMaterial);}
 }
}
