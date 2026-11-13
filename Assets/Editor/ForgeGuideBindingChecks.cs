using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ForgeGuideBindingChecks {
 static void Require(bool c,string text){if(!c)throw new Exception(text);}
 public static string Run(){
 var names=new HashSet<string>(Regex.Matches(File.ReadAllText("Assets/UI/ForgeGameUI.prefab"),@"m_Name: (GuideEntry[0-9]+)\r?\n").Cast<Match>().Select(m=>m.Groups[1].Value));
 var missing=ForgeGameUI.MissingGuideEntries(names,BlueprintGuide.Entries.Count);Require(names.Count>0,"actual prefab index");foreach(int i in missing)names.Add("GuideEntry"+i);Require(ForgeGameUI.MissingGuideEntries(names,BlueprintGuide.Entries.Count).Count==0,"idempotent completed registry");
 int last=BlueprintGuide.Entries.Count-1;names.Remove("GuideEntry"+last);names.Remove("GuideEntry0");var gaps=ForgeGameUI.MissingGuideEntries(names,BlueprintGuide.Entries.Count);Require(gaps.SequenceEqual(new[]{0,last}),"missing internal and appended rows");
 Require(ForgeGuideReader.Sections[0]=="게임 소개"&&ForgeGuideReader.Sections.Contains("재료"),"original bookmarks preserved");Require(BlueprintGuide.Entries.Select(e=>e.Id).Distinct().Count()==BlueprintGuide.Entries.Count,"unique guide entries");
 foreach(int material in new[]{7,8,9}){string text=ForgeMaterialGuide.Find(material).Description;Require(text.Length<500&&!text.Contains("연습")&&!text.Contains("예시")&&!text.Contains("예제")&&!text.Contains("실행 표시"),"concise material "+material);}
 Require(ForgeMaterialGuide.Find(7).Description.Contains("70()~73()")&&ForgeMaterialGuide.Find(7).Description.Contains("250G"),"star notation and reward");
 Require(ForgeDragonGuide.Basic.Contains("90() ~ 95()")&&ForgeDragonGuide.Basic.Contains("380G")&&ForgeDragonGuide.Basic.Contains("200G"),"bone notation and rewards");
 Require(ForgeCoolingGuide.Basic.Contains("80()~83() 함수로")&&ForgeCoolingGuide.Basic.Contains("84() 함수로")&&ForgeCoolingGuide.Basic.Contains("배열"),"frost route usage");
 Require(BlueprintAliases.Find(BlueprintFeature.Refine).Description.Contains(ForgeBalance.DragonHandleBonus+"G")&&BlueprintAliases.Find(BlueprintFeature.Curse).Description.Contains("667(Item)"),"refine reward and curse protection");
 var commission=BlueprintGuide.Entries.Single(e=>e.Id=="master-commission");Require(commission.Section=="대장장이와 다이아몬드","commission in smith bookmark");
 foreach(bool english in new[]{false,true}){Require(commission.DescriptionFor(english).Contains("7K")&&commission.DescriptionFor(english).Contains("대장장이 판매 배율을 제외")&&commission.ExampleFor(english)=="","concise commission rules");Require(commission.Matches("마지막 의뢰",english),"commission searchable");}
 Require(BlueprintResearchGuide.VisibleProse(commission.DescriptionFor(false),new ForgeTechnology(),null).Contains("대장장이 판매 배율을 제외"),"unlocked prose preserves commission rule");
 Require(BlueprintResearchGuide.EntryVisible(commission,new ForgeTechnology(),null),"commission explanation available before accepting");
 var functions=BlueprintGuide.Entries.Where(e=>e.Id.StartsWith("material-function-")).ToArray();Require(functions.Length==15,"all 15 added material functions");
 foreach(var entry in functions){int material=ForgeStarlight.IsFeature(entry.Feature)?7:ForgeCoolingGraph.IsFeature(entry.Feature)?8:9;var locked=new ForgeTechnology();Require(!BlueprintResearchGuide.EntryVisible(entry,locked,null),"locked function");var unlocked=new ForgeTechnology();unlocked.Restore(Array.Empty<BlueprintFeature>(),new[]{"material-"+material});Require(BlueprintResearchGuide.EntryVisible(entry,unlocked,null),"restored material unlock");Require(entry.Section=="금속 가공"&&entry.ExampleFor(false)==""&&entry.ExampleFor(true)=="","dedicated concise function page");Require(entry.Matches(entry.Alias.Symbol,false)&&entry.DescriptionFor(false).Contains(entry.Alias.Signature),"search signature");}
 return "Guide binding checks passed: actual prefab missing rows="+string.Join(",",missing)+"; complete registry, idempotency, gaps and original bookmarks";
 }
 [MenuItem("Tools/Validation/Guide Bookmarks and Introduction")]
 public static void RunUnity(){
 Run();var owner=new GameObject("Guide validation owner");owner.SetActive(false);var game=owner.AddComponent<ForgeMvp>();var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();
 try{game.SetHierarchyGame(ui);ui.Bind(game);ui.Bind(game);ui.Active("Guide",true);
 for(int i=0;i<BlueprintGuide.Entries.Count;i++)Require(ui.GetComponentsInChildren<ForgeUIAction>(true).Count(a=>a.action=="guide-entry:"+i)==1,"single index button "+i);
 foreach(string section in ForgeGuideReader.Sections){game.GameGuideAction("guide-section:"+section);for(int i=0;i<ForgeMaterialGuide.Entries.Count;i++)Require(ui.Node("GuideMaterial"+i).gameObject.activeSelf==(section=="재료"&&i<=3),"material filter "+section+"/"+i);}
 ui.Input("GuideSearch","없는 검색어");game.GameGuideSearch();game.GameGuideAction("guide-section:게임 소개");Require(ui.Input("GuideSearch")=="","bookmark clears search");Require(ui.Node("GuideDetail").gameObject.activeSelf&&!ui.Node("GuideIndex").gameObject.activeSelf,"introduction detail restored");Require(ui.Node("GuideHeading").GetComponent<Text>().text==BlueprintGuide.IntroductionTitle&&ui.Node("GuideBody").GetComponent<Text>().text.Contains("기본 공정으로 검"),"original introduction text");
 game.GameGuideAction("guide-section:기본 문법");int entry=Enumerable.Range(0,BlueprintGuide.Entries.Count).Single(i=>BlueprintGuide.Entries[i].Id=="increment");Require(ui.Node("GuideEntry"+entry).gameObject.activeSelf,"new entry visible");game.GameGuideAction("guide-entry:"+entry);Require(ui.Node("GuideHeading").GetComponent<Text>().text.Contains("증감"),"new entry opens");Debug.Log("PASS guide bookmark filtering, introduction, missing index recovery and idempotent binding");
 }finally{UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(owner);}
 }

 [MenuItem("Tools/Validation/Final Commission Guide")]
 public static void RunCommissionUI(){
 const string output="Artifacts/CommissionGuide";Directory.CreateDirectory(output);
 var owner=new GameObject("Commission guide owner");owner.SetActive(false);var game=owner.AddComponent<ForgeMvp>();
 var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();
 var host=new GameObject("Commission guide render",typeof(RectTransform),typeof(Canvas));var cameraRoot=new GameObject("Commission guide camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.18f);
 var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
 try{
  game.SetHierarchyGame(ui);ui.Bind(game);ui.Bind(game);
  int index=Enumerable.Range(0,BlueprintGuide.Entries.Count).Single(i=>BlueprintGuide.Entries[i].Id=="master-commission");
  game.GameGuideAction("guide-section:대장장이와 다이아몬드");Require(ui.Node("GuideEntry"+index).gameObject.activeSelf,"commission visible in smith bookmark before accepting");
  Require(ui.GetComponentsInChildren<ForgeUIAction>(true).Count(a=>a.action=="guide-entry:"+index)==1,"one commission entry");
  var guide=(RectTransform)ui.Node("Guide");guide.SetParent(host.transform,false);guide.gameObject.SetActive(true);ForgeLegacyUIFont.Apply(guide);
  var minimum=guide.GetComponentsInChildren<ForgeUIDrag>(true).First(d=>d.resize).minimum;
  var tech=(ForgeTechnology)typeof(ForgeMvp).GetField("technology",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(game);
  var report=new System.Text.StringBuilder();
  foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160),new Vector2Int(640,480)}){
   var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
   try{foreach(bool english in new[]{false,true})foreach(bool small in new[]{false,true}){
    tech.Restore(Array.Empty<BlueprintFeature>(),english?new[]{ForgeTechnology.EnglishResearch}:Array.Empty<string>());
    game.GameGuideAction("guide-section:대장장이와 다이아몬드");game.GameGuideAction("guide-entry:"+index);
    guide.anchorMin=guide.anchorMax=guide.pivot=new Vector2(.5f,.5f);guide.anchoredPosition=Vector2.zero;guide.sizeDelta=small?minimum:new Vector2(Mathf.Min(800,size.x-32),Mathf.Min(660,size.y-32));
    for(int pass=0;pass<5;pass++){Canvas.ForceUpdateCanvases();foreach(var layout in guide.GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);}
    var body=ui.Node("GuideBody").GetComponent<Text>();Require(body.text.Contains("7K")&&body.text.Contains("대장장이 판매 배율을 제외"),"actual commission detail");Require(!ui.Node("GuideCopy").gameObject.activeSelf,"no empty example button");
    foreach(var text in guide.GetComponentsInChildren<Text>()){if(text.GetComponentInParent<InputField>())continue;Require(text.preferredHeight<=text.rectTransform.rect.height+2,"clipped "+text.name+" at "+size+" font="+text.fontSize+" rect="+text.rectTransform.rect+" preferred="+text.preferredHeight);}
    var scroll=guide.GetComponentsInChildren<ScrollRect>(true).First(x=>x.content&&body.transform.IsChildOf(x.content));
    foreach(float pos in new[]{1f,0f}){scroll.verticalNormalizedPosition=pos;Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);try{png.ReadPixels(new Rect(0,0,size.x,size.y),0,0);png.Apply();File.WriteAllBytes(output+"/guide-"+size.x+"-"+english+"-"+small+"-"+pos+".png",png.EncodeToPNG());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(png);}}
    report.AppendLine("PASS "+size+" English="+english+" minimum="+small+" actual detail and text metrics");
   }}finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);}
  }
  File.WriteAllText(output+"/results.txt",report.ToString());Debug.Log("COMMISSION_GUIDE_UI_PASS "+DateTime.UtcNow.ToString("O"));
 }finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(cameraRoot);}
 }
 public static void RunCommissionBatch(){try{RunCommissionUI();EditorApplication.Exit(0);}catch(Exception error){Directory.CreateDirectory("Artifacts/CommissionGuide");File.WriteAllText("Artifacts/CommissionGuide/failure.txt",error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}}
}
