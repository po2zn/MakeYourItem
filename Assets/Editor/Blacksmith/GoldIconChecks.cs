using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
public static class GoldIconChecks
{
 public static async Task<string> Run(){
 if(!EditorApplication.isPlaying)throw new Exception("Play required");var game=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(ForgeMvp);var title=type.GetField("showTitle",f);var tree=type.GetField("showTechTree",f);var titlePage=type.GetField("titlePage",f);
 bool oldTitle=(bool)title.GetValue(game),oldTree=(bool)tree.GetValue(game),background=Application.runInBackground;int page=(int)titlePage.GetValue(game);string report="";
 try{
 Application.runInBackground=true;title.SetValue(game,false);tree.SetValue(game,false);await Task.Delay(350);Canvas.ForceUpdateCanvases();
 var gold=game.HierarchyGame.Node("Gold").GetComponent<Text>();var effect=gold.GetComponent<ForgeGoldGlyph>();report+="HUD text="+gold.text+" glyphs="+(effect?effect.ReplacedGlyphs:-1)+" vertices="+gold.cachedTextGenerator.vertexCount+"\n";
 if(!effect||effect.ReplacedGlyphs!=1)throw new Exception(report);
 ScreenCapture.CaptureScreenshot("Assets/Art/Blacksmith/Documentation/GoldHud.png");await Task.Delay(300);
 tree.SetValue(game,true);await Task.Delay(300);var panel=game.HierarchyTree;panel.ShowPage(1);panel.maps[1].Fit();Canvas.ForceUpdateCanvases();await Task.Delay(200);
 foreach(var card in panel.cards.Where(c=>c.offer!=null&&c.offer.Kind==ForgePurchaseKind.Material)){
 var glyph=card.price.GetComponent<ForgeGoldGlyph>();if(!glyph||glyph.ReplacedGlyphs!=1)throw new Exception("Missing price coin "+card.offerId);report+="PASS price coin "+card.offerId+"\n";
 }
 ScreenCapture.CaptureScreenshot("Assets/Art/Blacksmith/Documentation/GoldTree.png");await Task.Delay(300);
 var sample=panel.tooltipBody;string prior=sample.text;var priorActive=panel.tooltip.gameObject.activeSelf;panel.tooltip.gameObject.SetActive(true);sample.text="100G\n10KG · 1.5MG · 2BG\nG 변수 · 10GHz";Canvas.ForceUpdateCanvases();await Task.Delay(100);
 var sampleGlyph=sample.GetComponent<ForgeGoldGlyph>();int count=sampleGlyph?sampleGlyph.ReplacedGlyphs:-1;sample.text=prior;panel.tooltip.gameObject.SetActive(priorActive);if(count!=4)throw new Exception("Multiline currency count "+count);report+="PASS multiline K/M/B and non-currency G/GHz exclusion\n";
 tree.SetValue(game,false);title.SetValue(game,true);titlePage.SetValue(game,3);await Task.Delay(250);ScreenCapture.CaptureScreenshot("Assets/Art/Blacksmith/Documentation/GoldTitle.png");await Task.Delay(300);
 File.WriteAllText("Assets/Art/Blacksmith/Documentation/GoldIconChecks.txt",report);return report;
 }finally{title.SetValue(game,oldTitle);tree.SetValue(game,oldTree);titlePage.SetValue(game,page);Application.runInBackground=background;}
 }
}
