using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ForgeGuideBookmarkChecks {
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 public static void RunBatch(){try{Run();Debug.Log("GUIDE_BOOKMARKS_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);File.WriteAllText("Artifacts/GuideBookmarks/failure.txt",e.ToString());EditorApplication.Exit(1);}}
 static void Run(){
 ForgeGuideBindingChecks.RunUnity();
 var oldSections=new[]{"출력","기본 문법","완성 예제","문제가 생겼을 때"};
 Check(!BlueprintGuide.Entries.Any(e=>oldSections.Contains(e.Section)),"merged topics only in start");
 Check(BlueprintGuide.Entries.Single(e=>e.Id=="print").Section=="시작하기","print grouped");
 var owner=new GameObject("Bookmark check owner");owner.SetActive(false);var game=owner.AddComponent<ForgeMvp>();
 var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();
 game.SetHierarchyGame(ui);ui.Bind(game);
 var tech=(ForgeTechnology)typeof(ForgeMvp).GetField("technology",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
 tech.Restore(null,ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material).Select(o=>o.Id).ToArray());
 game.GameGuideAction("guide-section:시작하기");
 foreach(int i in new[]{14,16,17,18})Check(!ui.Node("GuideSection"+i).gameObject.activeSelf,"old bookmark hidden "+i);
 foreach(var section in oldSections){game.GameGuideAction("guide-section:"+section);for(int i=0;i<BlueprintGuide.Entries.Count;i++)Check(ui.Node("GuideEntry"+i).gameObject.activeSelf==(BlueprintGuide.Entries[i].Section=="시작하기"),"start/legacy navigation "+section+"/"+i);}
 var label=ui.Node("GuideSection12").GetComponentInChildren<Text>();Check(label.text=="조건","condition label");
 Check(ui.Node("GuideSection1").GetComponentInChildren<Text>().text=="반복","loop label");
 Check(!ui.GetComponentsInChildren<ForgeUIAction>(true).Any(a=>a.action=="guide-section:테크 트리"),"no growth bookmark");
 foreach(var id in new[]{"while","for","break","continue","stages"})Check(BlueprintGuide.Entries.Single(e=>e.Id==id).Section=="반복","loop entry "+id);
 foreach(var id in new[]{"if","elif","else","match"})Check(BlueprintGuide.Entries.Single(e=>e.Id==id).Section=="조건","condition entry "+id);
 foreach(string category in new[]{"조건","반복"}){game.GameGuideAction("guide-section:"+category);for(int i=0;i<BlueprintGuide.Entries.Count;i++)Check(ui.Node("GuideEntry"+i).gameObject.activeSelf==(BlueprintGuide.Entries[i].Section==category),"category filter "+category+"/"+i);}
 var host=new GameObject("Bookmark canvas",typeof(RectTransform),typeof(Canvas));var cameraRoot=new GameObject("Bookmark camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.18f);var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
 var guide=(RectTransform)ui.Node("Guide");guide.SetParent(host.transform,false);guide.gameObject.SetActive(true);ForgeLegacyUIFont.Apply(guide);
 var minimum=guide.GetComponentsInChildren<ForgeUIDrag>(true).First(d=>d.resize).minimum;var report=new System.Text.StringBuilder();
 foreach(var size in new[]{new Vector2Int(640,480),new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)})foreach(bool english in new[]{false,true})foreach(bool small in new[]{false,true})foreach(string category in new[]{"조건","반복"}){
 tech.Restore(null,ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material&&(english||o.Id!=ForgeTechnology.EnglishResearch)).Select(o=>o.Id).ToArray());game.GameGuideAction("guide-section:"+category);
 var rt=new RenderTexture(size.x,size.y,24);camera.targetTexture=rt;guide.anchorMin=guide.anchorMax=guide.pivot=new Vector2(.5f,.5f);guide.anchoredPosition=new Vector2(56,0);guide.sizeDelta=small?minimum:new Vector2(Mathf.Min(800,size.x-144),Mathf.Min(660,size.y-32));
 for(int pass=0;pass<5;pass++){Canvas.ForceUpdateCanvases();guide.GetComponent<ForgeLegacyBook>().LayoutIndexRows();foreach(var layout in guide.GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);}
 foreach(var text in guide.GetComponentsInChildren<Text>()){if(text.GetComponentInParent<InputField>())continue;Check(text.preferredHeight<=text.rectTransform.rect.height+2,"clipped "+text.name+" "+size+" preferred="+text.preferredHeight+" actual="+text.rectTransform.rect.height);}
 var rail=label.GetComponentInParent<ScrollRect>();rail.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var png=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,size.x,size.y),0,0);png.Apply();File.WriteAllBytes("Artifacts/GuideBookmarks/guide-"+category+"-"+size.x+"-"+english+"-"+small+".png",png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);report.AppendLine("PASS "+size+" english="+english+" minimum="+small);
 }
 File.WriteAllText("Artifacts/GuideBookmarks/results.txt",report.ToString());if(File.Exists("Artifacts/GuideBookmarks/failure.txt"))File.Delete("Artifacts/GuideBookmarks/failure.txt");UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(cameraRoot);
 }
}
