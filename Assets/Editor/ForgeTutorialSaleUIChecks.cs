using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ForgeTutorialSaleUIChecks
{
 const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Set(object obj,string key,object value)=>obj.GetType().GetField(key,Flags).SetValue(obj,value);
 static object Get(object obj,string key)=>obj.GetType().GetField(key,Flags).GetValue(obj);
 static void Call(object obj,string key,params object[] args)=>obj.GetType().GetMethod(key,Flags).Invoke(obj,args);
 static void Tick(ForgeGameUI ui){for(int i=0;i<5;i++){Canvas.ForceUpdateCanvases();Call(ui,"UpdateTutorialPresentation");foreach(var layout in ui.Node("Tutorial").GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);}var inv=(ForgeProductInventory)Get(ui,"tutorialInventory");if(inv&&inv.gameObject.activeInHierarchy)Call(inv,"LateUpdate");Canvas.ForceUpdateCanvases();}
 static void Capture(Camera camera,RenderTexture target,string name){camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);try{png.ReadPixels(new Rect(0,0,target.width,target.height),0,0);png.Apply();File.WriteAllBytes("Artifacts/TutorialSale/"+name+".png",png.EncodeToPNG());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(png);}}
 static void CheckText(Transform root,string context){foreach(var text in root.GetComponentsInChildren<Text>()){if(text.GetComponentInParent<InputField>())continue;Require(text.preferredHeight<=text.rectTransform.rect.height+2,"clipped "+text.name+" "+context+" height="+text.rectTransform.rect.height+" preferred="+text.preferredHeight);}}
 [MenuItem("Tools/Validation/Tutorial First Sale")]
 public static void Run()
 {
  ForgeTutorialChecks.Main();Directory.CreateDirectory("Artifacts/TutorialSale");
  var owner=new GameObject("Tutorial test owner");owner.SetActive(false);var game=owner.AddComponent<ForgeMvp>();Set(game,"<DayCycle>k__BackingField",owner.AddComponent<ForgeDayCycle>());Set(game,"craftMotion",owner.AddComponent<ForgeCraftMotion>());
  var ui=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab")).GetComponent<ForgeGameUI>();
  var cameraRoot=new GameObject("Tutorial test camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.18f);
  try{
   Set(game,"showTitle",false);Set(game,"showTutorial",true);Set(game,"tutorialFade",1f);Set(game,"tutorialAnimating",false);Set(game,"gold",1234);game.SetHierarchyGame(ui);ui.Bind(game);ui.gameObject.SetActive(true);
   ui.GetComponent<ForgeLegacyLayout>().enabled=false;var scaler=ui.GetComponent<CanvasScaler>();if(scaler)scaler.enabled=false;
   ui.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.canvas.worldCamera=camera;ui.canvas.planeDistance=10;
   var realShop=(ForgeShop)Get(game,"shop");var tech=(ForgeTechnology)Get(game,"technology");string code=string.Join("\n",ForgeTutorialLesson.Hints.Skip(1))+"\n";
   var report=new System.Text.StringBuilder();
   foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160),new Vector2Int(640,480)})foreach(bool english in new[]{false,true}){
    tech.Restore(Array.Empty<BlueprintFeature>(),english?new[]{ForgeTechnology.EnglishResearch}:Array.Empty<string>());
    Set(game,"showTutorial",true);var lesson=ForgeTutorialLesson.Restore(8,code);Set(game,"lesson",lesson);Set(game,"showProductInventory",false);Set(ui,"tutorialSaleMotion",0f);
    var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
    string prefix=size.x+"-"+english;
    try{
     game.RefreshGameUI();Tick(ui);Call(ui,"UpdateTutorialSalePresentation",.525f);var panel=(RectTransform)ui.Node("TutorialScroll");ForgeTutorialFrame.Apply(panel);Require(panel.Cast<Transform>().Count(t=>t.name=="TutorialPaperFrame")==1,"single tutorial frame");var frame=panel.Find("TutorialPaperFrame");Require(frame.GetComponentsInChildren<Image>().All(image=>!image.raycastTarget),"frame preserves scroll and clicks");Require(frame.GetComponentInParent<CanvasGroup>()==panel.GetComponent<CanvasGroup>(),"frame inherits panel fade");Require(panel.localScale.x<.8f,"shrunk during movement");Require(panel.GetComponent<CanvasGroup>().alpha==0,"panel faded out during movement");if(!english)Capture(camera,target,prefix+"-moving");
     Call(ui,"UpdateTutorialSalePresentation",2f);Tick(ui);Require(Mathf.Approximately(panel.localScale.x,1),"expanded after movement");Require(panel.GetComponent<CanvasGroup>().alpha==1,"panel faded in after movement");Require(panel.anchoredPosition.x>size.x*.45f,"right panel");
     var product=(Button)((RectTransform)Get(ui,"tutorialProductsTile")).GetComponent<Button>();Require(product.GetComponent<ForgeTutorialAttention>().enabled,"products cue");CheckText(ui.Node("Tutorial"),prefix+" entry");if(!english)Capture(camera,target,prefix+"-open");
     product.onClick.Invoke();game.RefreshGameUI();Tick(ui);var inv=(ForgeProductInventory)Get(ui,"tutorialInventory");Require(inv.gameObject.activeInHierarchy&&inv.PoolCount==1,"isolated practice inventory");Require(inv.detailsArea.gameObject.activeSelf,"shared product details visible");Require(((RectTransform)inv.transform).offsetMax.y==-12,"inventory at top");Require(!((RectTransform)Get(ui,"tutorialGoldTile")).gameObject.activeSelf,"no gold gutter in inventory");
     var cell=inv.gridScroll.content.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("VisibleProductCell"));Require(cell.GetComponent<ForgeTutorialAttention>().enabled,"sword cue");CheckText(ui.Node("Tutorial"),prefix+" select");if(!english)Capture(camera,target,prefix+"-select");
     Set(game,"tutorialAnimating",true);cell.onClick.Invoke();Require(lesson.Stage==8,"animation gates selection");Set(game,"tutorialAnimating",false);
     cell.onClick.Invoke();game.RefreshGameUI();Tick(ui);Require(lesson.Stage==9,"actual sword click selects");var sell=cell.GetComponentsInChildren<Button>().First(b=>b.name=="SellThisProduct");Require(sell.GetComponent<ForgeTutorialAttention>().enabled,"sale cue");
     var corners=new Vector3[4];((RectTransform)sell.transform).GetWorldCorners(corners);foreach(var corner in corners)Require(inv.gridScroll.viewport.rect.Contains(inv.gridScroll.viewport.InverseTransformPoint(corner)),"sale action fully visible after selection "+prefix);CheckText(ui.Node("Tutorial"),prefix+" sell");if(!english)Capture(camera,target,prefix+"-sell");
     if(english){Set(inv,"lastClickedAt",Time.unscaledTime);cell.onClick.Invoke();}else sell.onClick.Invoke();game.RefreshGameUI();Tick(ui);Require(lesson.Sold&&!lesson.Complete&&lesson.PracticeShop.Count==0,"sale awaits return");int paid=lesson.PracticeGold;sell.onClick.Invoke();Require(lesson.PracticeGold==paid,"duplicate sale guarded");Require(inv.backButton.GetComponent<ForgeTutorialAttention>().enabled,"return cue");CheckText(ui.Node("Tutorial"),prefix+" sold");if(!english)Capture(camera,target,prefix+"-sold");
     inv.backButton.onClick.Invoke();game.RefreshGameUI();Tick(ui);Require(lesson.Complete&&!inv.gameObject.activeSelf,"return completes tutorial");Require(ui.Node("TutorialClose").GetComponentInChildren<Text>().text=="튜토리얼 마치기","completion exit action");CheckText(ui.Node("Tutorial"),prefix+" complete");if(!english)Capture(camera,target,prefix+"-complete");
     Require((int)Get(game,"gold")==1234&&realShop.Count==0,"real economy untouched");
     Require(ui.Node("Tutorial").GetComponentsInChildren<ForgeProductInventory>(true).Length==1,"one practice inventory");
     ui.Node("TutorialClose").GetComponent<Button>().onClick.Invoke();Require(!(bool)Get(game,"showTutorial")&&!game.DayCycle.Paused,"finish exits tutorial and resumes time");
     report.AppendLine("PASS "+size+" English="+english+" transition, HUD, select/sell/return, cues, text metrics, isolation");
    }finally{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);}
   }
   File.WriteAllText("Artifacts/TutorialSale/results.txt",report.ToString());Debug.Log("TUTORIAL_SALE_UI_PASS "+DateTime.UtcNow.ToString("O"));
  }finally{UnityEngine.Object.DestroyImmediate(ui.gameObject);UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(cameraRoot);}
 }
 public static void RunBatch(){try{Run();EditorApplication.Exit(0);}catch(Exception error){Directory.CreateDirectory("Artifacts/TutorialSale");File.WriteAllText("Artifacts/TutorialSale/failure.txt",error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}}
}
