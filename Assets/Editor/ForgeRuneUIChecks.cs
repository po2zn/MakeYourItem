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
  CheckArrival();CheckExpandedFloor();
 }
 static void CheckExpandedFloor(){
  var root=new GameObject("Rune floor integration");root.SetActive(false);var game=root.AddComponent<ForgeMvp>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  var state=(ForgeBlacksmiths)typeof(ForgeMvp).GetField("blacksmiths",flags).GetValue(game);var rune=new ForgeBlacksmiths.GiantRune{rune=7,x=1,y=1,timed=true,expiresAt=100};state.runes.Add(rune);
  ((HashSet<ForgeBlacksmiths.GiantRune>)typeof(ForgeMvp).GetField("arrivingRunes",flags).GetValue(game)).Add(rune);
  typeof(ForgeMvp).GetMethod("UpdateGiantRuneViews",flags).Invoke(game,null);
  var view=((List<GameObject>)typeof(ForgeMvp).GetField("giantRuneViews",flags).GetValue(game))[0];var ground=root.GetComponentInChildren<ForgeRuneGround>(true);var stage=new GameObject("Visible floor stage");view.transform.SetParent(stage.transform,false);ground.transform.SetParent(stage.transform,false);
  var shadow=root.transform.Find("거대한 룬 착지 그림자");if(shadow)shadow.SetParent(stage.transform,false);
  var assets=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");UnityEngine.Object.Instantiate(assets.floor,stage.transform,false);
  var cameraRoot=new GameObject("Floor camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=7;camera.transform.position=new Vector3(12,12,-10);camera.transform.LookAt(new Vector3(4,1,2));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.18f);
  var light=new GameObject("Floor light",typeof(Light));light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-30,0);
  var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
  try{var arrival=view.GetComponent<ForgeGiantRuneArrival>();float previous=0;foreach(float t in new[]{0f,.3f,.6f,.8f}){arrival.Sample(t);ground.Advance(t-previous);previous=t;Capture(camera,target,"expanded-floor-"+t.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));}Require(ground.Extents.z>=7&&ground.Extents.w>=4.75f,"actual model covered by extended floor");}
  finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(stage);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);UnityEngine.Object.DestroyImmediate(light);UnityEngine.Object.DestroyImmediate(target);}
 }
 static void CheckArrival(){
  var root=new GameObject("Rune arrival stage");var cameraRoot=new GameObject("Rune arrival camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5.8f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.18f);camera.transform.position=new Vector3(7,9,-10);camera.transform.LookAt(new Vector3(1.2f,3,0));
  var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform,false);ground.transform.position=new Vector3(1.2f,-.12f,0);ground.transform.localScale=new Vector3(10,.2f,10);var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));groundMaterial.color=new Color(.55f,.5f,.4f);ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
  var rune=GameObject.CreatePrimitive(PrimitiveType.Cube);rune.transform.SetParent(root.transform,false);rune.transform.localScale=new Vector3(2.25f,.02f,2.25f);var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=new Color(.5f,.25f,.9f);rune.GetComponent<Renderer>().sharedMaterial=material;var arrival=rune.AddComponent<ForgeGiantRuneArrival>();var floor=new Vector3(1.2f,.02f,0);arrival.Begin(floor);
  var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
  try{foreach(float time in new[]{0f,.3f,.6f,.7f,.8f}){arrival.Sample(time);Require(Mathf.Abs(rune.transform.localPosition.y-floor.y-ForgeGiantRuneArrival.HeightAt(time))<.001f,"arrival height");Capture(camera,target,"arrival-"+time.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));}Require(!arrival.Playing&&Vector3.Distance(rune.transform.localPosition,floor)<.001f,"landed once");Require(!root.transform.Find("거대한 룬 착지 그림자").gameObject.activeSelf,"shadow cleaned after landing");}
  finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraRoot);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(groundMaterial);}
 }
}
