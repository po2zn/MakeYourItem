using System;using System.IO;using System.Linq;using System.Reflection;using UnityEditor;using UnityEngine;using UnityEngine.UI;
public static class ForgeStarGuideTextChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void RunBatch(){try{Directory.CreateDirectory("Artifacts/StarGuideText");Run();Debug.Log("STAR_GUIDE_TEXT_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Run(){
 foreach(var section in new[]{"대장장이와 다이아몬드","공방 운영","마력 가공"}){long last=-1;foreach(var e in BlueprintGuide.Entries.Where(e=>e.Section==section)){var digits=new string(e.Title.TakeWhile(char.IsDigit).ToArray());long n=long.TryParse(digits,out var value)?value:-1;Check(n>=last,"numeric bookmark order "+section);last=n;}}

 Check(BlueprintGuide.Entries.ToList().FindIndex(e=>e.Feature==BlueprintFeature.StarlightEngrave)==BlueprintGuide.Entries.ToList().FindIndex(e=>e.Id=="material-function-73")+1,"74 directly follows 73");
 var owner=new GameObject("Bookmark check owner");owner.SetActive(false);var game=owner.AddComponent<ForgeMvp>();
 var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();
 game.SetHierarchyGame(ui);ui.Bind(game);
 var tech=(ForgeTechnology)typeof(ForgeMvp).GetField("technology",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
 tech.Restore(null,ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material).Select(o=>o.Id).ToArray());
 var host=new GameObject("Bookmark canvas",typeof(RectTransform),typeof(Canvas));var cameraRoot=new GameObject("Bookmark camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.18f);var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
 var guide=(RectTransform)ui.Node("Guide");guide.SetParent(host.transform,false);guide.gameObject.SetActive(true);ForgeLegacyUIFont.Apply(guide);
 var minimum=guide.GetComponentsInChildren<ForgeUIDrag>(true).First(d=>d.resize).minimum;var report=new System.Text.StringBuilder();
 foreach(var size in new[]{new Vector2Int(640,480),new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)})foreach(bool english in new[]{false,true})foreach(bool small in new[]{false,true})foreach(string category in new[]{"extension-211","extension-401","extension-501","extension-202","extension-203"}){
 tech.Restore(null,ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material&&(english||o.Id!=ForgeTechnology.EnglishResearch)).Select(o=>o.Id).ToArray());game.GameGuideAction(category=="용뼈"?"guide-material:9":"guide-entry:"+Enumerable.Range(0,BlueprintGuide.Entries.Count).Single(i=>BlueprintGuide.Entries[i].Id==category));
 var rt=new RenderTexture(size.x,size.y,24);camera.targetTexture=rt;guide.anchorMin=guide.anchorMax=guide.pivot=new Vector2(.5f,.5f);guide.anchoredPosition=new Vector2(56,0);guide.sizeDelta=small?minimum:new Vector2(Mathf.Min(800,size.x-144),Mathf.Min(660,size.y-32));
 for(int pass=0;pass<5;pass++){Canvas.ForceUpdateCanvases();guide.GetComponent<ForgeLegacyBook>().LayoutIndexRows();foreach(var layout in guide.GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);}
 foreach(var text in guide.GetComponentsInChildren<Text>()){if(text.GetComponentInParent<InputField>())continue;Check(text.preferredHeight<=text.rectTransform.rect.height+2,"clipped "+text.name+" "+size+" preferred="+text.preferredHeight+" actual="+text.rectTransform.rect.height);}
 Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var png=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,size.x,size.y),0,0);png.Apply();File.WriteAllBytes("Artifacts/StarGuideText/guide-"+category+"-"+size.x+"-"+english+"-"+small+".png",png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);report.AppendLine("PASS "+size+" english="+english+" minimum="+small);
 }
 File.WriteAllText("Artifacts/StarGuideText/results.txt",report.ToString());if(File.Exists("Artifacts/StarGuideText/failure.txt"))File.Delete("Artifacts/StarGuideText/failure.txt");UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(cameraRoot);
 }
}
