using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ForgeCoolingUIChecks {
 const string Output="Artifacts/Cooling/UI";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static object Get(object target,string name)=>target.GetType().GetField(name,Flags).GetValue(target);
 static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
 [MenuItem("Tools/Validation/Cooling Blueprint UI and Persistence")]
 public static void Run(){
  Directory.CreateDirectory(Output);var report=new StringBuilder();
  var data=new ForgeItemData{cooling=ForgeCoolingGraph.Tutorial()};var loaded=JsonUtility.FromJson<ForgeItemData>(JsonUtility.ToJson(data));for(int a=0;a<4;a++)for(int b=0;b<4;b++)Require(data.cooling.Cost(a,b)==loaded.cooling.Cost(a,b),"graph save/load");
  var gameObject=new GameObject("Cooling validation owner");gameObject.SetActive(false);var game=gameObject.AddComponent<ForgeMvp>();foreach(string name in new[]{"showTitle","showTutorial","showGameMenu","showTechTree","showProgress"})Set(game,name,false);
  var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();game.SetHierarchyGame(ui);ui.Bind(game);
  foreach(string node in new[]{"Menu","Tutorial","Progress","Ending","Delete","Developer","Sales","Debug","Guide"})ui.Active(node,false);
  var tech=(ForgeTechnology)Get(game,"technology");var panel=ForgeCoolingPanel.Create(game,ui);panel.SyncVisibility(game.CoolingVisible);var toggle=ui.GetComponentsInChildren<Button>(true).Single(b=>b.name=="CoolingBlueprintToggle");Require(!toggle.gameObject.activeSelf,"locked entry hidden");
  tech.Restore(Array.Empty<BlueprintFeature>(),new[]{"material-8",ForgeTechnology.AutomationRoot});panel.SyncVisibility(game.CoolingVisible);Require(toggle.gameObject.activeSelf,"restored unlock visible");
  for(int i=0;i<20;i++)Require(ReferenceEquals(panel,ForgeCoolingPanel.Create(game,ui)),"idempotent panel");Require(ui.GetComponentsInChildren<Button>(true).Count(b=>b.name=="CoolingBlueprintToggle")==1,"one button");
  var window=(GameObject)Get(panel,"window");Require(!window.activeSelf,"unlock does not switch/open panel");
  var cameraRoot=new GameObject("Cooling validation camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.18f);camera.orthographic=true;
  ui.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.canvas.worldCamera=camera;ui.canvas.planeDistance=10;var scaler=ui.GetComponent<CanvasScaler>();if(scaler)scaler.enabled=false;
  try{
   foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160),new Vector2Int(640,480)}){
    var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;Canvas.ForceUpdateCanvases();
    foreach(int count in new[]{4,8})foreach(bool minimum in new[]{false,true}){
     var blade=new ForgeMvp.Piece{id=count,material=1,heat=900,shaped=true,data=new ForgeItemData{cooling=count==4?ForgeCoolingGraph.Tutorial():ForgeCoolingGraph.Create(12,new ForgeCoolingSettings{points=count})}};game.CoolingView.Select(blade);
     var scope=new Dictionary<string,object>{{"MyCosts",Enumerable.Range(0,count).Select(i=>i==0?0:999999).ToList()},{"MyParents",Enumerable.Repeat(-1,count).ToList()},{"MyVisits",Enumerable.Repeat(0,count).ToList()}};
     game.CoolingObservation.Scope(18,null,scope,scope);game.CoolingObservation.Enabled=true;Set(panel,"observing",true);Set(panel,"distance","MyCosts");Set(panel,"previous","MyParents");Set(panel,"visited","MyVisits");Set(panel,"selectedVariable","MyCosts");panel.Open();
     var rect=(RectTransform)window.transform;rect.sizeDelta=minimum?window.GetComponentsInChildren<ForgeUIDrag>(true).First(d=>d.resize).minimum:new Vector2(Mathf.Min(480,size.x-24),size.y-36);rect.anchoredPosition=new Vector2(size.x-rect.sizeDelta.x-12,-12);
     game.CoolingView.A=0;game.CoolingView.B=1;game.CoolingView.Path=blade.data.cooling.DefaultPath().ToArray();game.CoolingView.Progress=.45f;panel.SendMessage("Refresh");Settle(panel,window);
     CheckText(window);Capture(camera,target,"diagram-"+size.x+"-"+count+"-"+minimum);report.AppendLine("Diagram "+size+" nodes="+count+" minimum="+minimum+" text metrics passed");
    }
    panel.SendMessage("StartPractice");panel.SendMessage("ResetPractice");Settle(panel,window);CheckText(window);Capture(camera,target,"practice-"+size.x);panel.Close();
    foreach(bool english in new[]{false,true})foreach(bool advanced in new[]{false,true})foreach(bool minimum in new[]{false,true}){
     tech.Restore(Array.Empty<BlueprintFeature>(),english?new[]{"material-8",ForgeTechnology.AutomationRoot,ForgeTechnology.EnglishResearch}:new[]{"material-8",ForgeTechnology.AutomationRoot});ui.Active("Guide",true);game.GameGuideAction("guide-material:8");if(advanced)game.GameGuideAction("guide-copy");var guide=(RectTransform)ui.Node("Guide");guide.sizeDelta=minimum?guide.GetComponentsInChildren<ForgeUIDrag>(true).First(d=>d.resize).minimum:new Vector2(Mathf.Min(850,size.x-120),size.y-36);guide.anchorMin=guide.anchorMax=guide.pivot=new Vector2(0,1);guide.anchoredPosition=new Vector2(12,-12);
     for(int pass=0;pass<5;pass++){Canvas.ForceUpdateCanvases();foreach(var group in guide.GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);}
     var body=ui.Node("GuideBody").GetComponent<Text>();Require(body.preferredHeight<=body.rectTransform.rect.height+2,"guide body clipped "+size);var copy=ui.Node("GuideCopy").GetComponentInChildren<Text>();Require(copy.preferredHeight<=copy.rectTransform.rect.height+2,"guide button clipped");
     if(advanced){var example=guide.GetComponentsInChildren<Text>().Single(t=>t.name=="GuideExampleText");Require(example.text.Contains("84(Blade, 8, Path)"),"actual advanced code visible");Require(example.preferredHeight<=example.rectTransform.rect.height+2,"code height");}
     Capture(camera,target,"guide-"+size.x+"-"+english+"-"+advanced+"-"+minimum);report.AppendLine("Guide "+size+" English="+english+" advanced="+advanced+" minimum="+minimum+" metrics passed");ui.Active("Guide",false);
    }
    camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);
   }
   File.WriteAllText(Output+"/results.txt",report+"PASS serialized graph, unlock/idempotency, layout metrics. Review PNGs separately for actual visual quality.");
  }finally{UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(gameObject);UnityEngine.Object.DestroyImmediate(cameraRoot);}
 }
 static void Settle(ForgeCoolingPanel panel,GameObject window){for(int i=0;i<5;i++){Canvas.ForceUpdateCanvases();panel.SendMessage("LateUpdate");foreach(var group in window.GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);}}
 static void CheckText(GameObject root){foreach(var text in root.GetComponentsInChildren<Text>()){if(text.GetComponentInParent<InputField>())continue;Require(text.preferredHeight<=text.rectTransform.rect.height+2,"clipped "+text.name+": "+text.preferredHeight+"/"+text.rectTransform.rect.height);}}
 static void Capture(Camera camera,RenderTexture target,string name){camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
 public static void RunBatch(){try{Run();EditorApplication.Exit(0);}catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}
