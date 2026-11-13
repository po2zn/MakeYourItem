using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.UI;
public static class ForgeEngravingUIChecks {
 static void Require(bool c,string text){if(!c)throw new Exception(text);}
 static void CheckTree(){var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeResearchUI.prefab").GetComponent<ForgeTreePanel>();var protect=tree.cards.Single(c=>c.offerId=="function-Protect");Require(((RectTransform)protect.transform).anchoredPosition==new Vector2(3424,-1024),"Protect position");var edge=tree.edges.Single(e=>e.parentOffer=="function-Curse");Require(edge.lines[0].rectTransform.anchoredPosition==new Vector2(3384,-1112)&&edge.lines[2].rectTransform.anchoredPosition.x+edge.lines[2].rectTransform.rect.width==3424,"Curse -> Protect line");Require(!tree.edges.Any(e=>e.parentOffer=="function-Protect"),"no old Protect -> Curse edge");}
 [MenuItem("Tools/Validation/Starlight Engraving Guide")]
 public static void RunUI(){
 const string output="Artifacts/StarlightEngraving/UI";Directory.CreateDirectory(output);
 var owner=new GameObject("Commission guide owner");owner.SetActive(false);var game=owner.AddComponent<ForgeMvp>();
 var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();
 var host=new GameObject("Commission guide render",typeof(RectTransform),typeof(Canvas));var cameraRoot=new GameObject("Commission guide camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.18f);
 var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
 try{
  game.SetHierarchyGame(ui);ui.Bind(game);ui.Bind(game);
  int index=Enumerable.Range(0,BlueprintGuide.Entries.Count).Single(i=>BlueprintGuide.Entries[i].Id=="starlight-engrave");

  Require(ui.GetComponentsInChildren<ForgeUIAction>(true).Count(a=>a.action=="guide-entry:"+index)==1,"one commission entry");
  var guide=(RectTransform)ui.Node("Guide");guide.SetParent(host.transform,false);guide.gameObject.SetActive(true);ForgeLegacyUIFont.Apply(guide);
  var minimum=guide.GetComponentsInChildren<ForgeUIDrag>(true).First(d=>d.resize).minimum;
  var tech=(ForgeTechnology)typeof(ForgeMvp).GetField("technology",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(game);
  var report=new System.Text.StringBuilder();
  foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160),new Vector2Int(640,480)}){
   var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
   try{foreach(var feature in new[]{BlueprintFeature.Speed,BlueprintFeature.AutoSell,BlueprintFeature.Refine,BlueprintFeature.Curse,BlueprintFeature.StarlightEngrave})foreach(bool english in new[]{false,true})foreach(bool small in new[]{false,true}){
    index=Enumerable.Range(0,BlueprintGuide.Entries.Count).Single(i=>BlueprintGuide.Entries[i].Feature==feature);
    tech.Restore(null,ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material&&(english||o.Id!=ForgeTechnology.EnglishResearch)).Select(o=>o.Id).ToArray());
    game.GameGuideAction("guide-section:"+BlueprintGuide.Entries[index].Section);game.GameGuideAction("guide-entry:"+index);
    guide.anchorMin=guide.anchorMax=guide.pivot=new Vector2(.5f,.5f);guide.anchoredPosition=new Vector2(56,0);guide.sizeDelta=small?minimum:new Vector2(Mathf.Min(800,size.x-144),Mathf.Min(660,size.y-32));
    for(int pass=0;pass<5;pass++){Canvas.ForceUpdateCanvases();foreach(var layout in guide.GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);}
    var body=ui.Node("GuideBody").GetComponent<Text>();Require(ui.Node("GuideHeading").GetComponent<Text>().text==BlueprintGuide.Entries[index].HeadingFor(english),"actual function heading");if(feature==BlueprintFeature.StarlightEngrave)Require(body.text.Contains("100G")&&body.text.Contains("05:00"),"engraving detail");
    foreach(var text in guide.GetComponentsInChildren<Text>()){if(text.GetComponentInParent<InputField>())continue;Require(text.preferredHeight<=text.rectTransform.rect.height+2,"clipped "+text.name+" at "+size+" font="+text.fontSize+" rect="+text.rectTransform.rect+" preferred="+text.preferredHeight);}
    var scroll=guide.GetComponentsInChildren<ScrollRect>(true).First(x=>x.content&&body.transform.IsChildOf(x.content));
    foreach(float pos in !english&&!small?new[]{1f}:Array.Empty<float>()){scroll.verticalNormalizedPosition=pos;Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);try{png.ReadPixels(new Rect(0,0,size.x,size.y),0,0);png.Apply();File.WriteAllBytes(output+"/guide-"+feature+"-"+size.x+"-"+english+"-"+small+"-"+pos+".png",png.EncodeToPNG());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(png);}}
    report.AppendLine("PASS "+size+" English="+english+" minimum="+small+" actual detail and text metrics");
   }}finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);}
  }
  File.WriteAllText(output+"/results.txt",report.ToString());Debug.Log("ENGRAVING_UI_PASS "+DateTime.UtcNow.ToString("O"));
 }finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(cameraRoot);}
 }
 public static void RunBatch(){try{Debug.Log(ForgeEngravingChecks.Run());ForgeEngravingChecks.UnityChecks();CheckTree();RunUI();EditorApplication.Exit(0);}catch(Exception error){Directory.CreateDirectory("Artifacts/StarlightEngraving/UI");File.WriteAllText("Artifacts/StarlightEngraving/UI/failure.txt",error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}}
}
