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
}
