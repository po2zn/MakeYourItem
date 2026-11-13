using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Native Unity verification; unlike the pure interpreter tests, this needs an Editor license.
public static class ForgeStarlightUIChecks
{
 const string Output="Artifacts/Starlight/UI";
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 [MenuItem("Tools/Validation/Star Iron UI and Persistence")]
 public static void Run(){
  Directory.CreateDirectory(Output);var report=new StringBuilder();
  var data=new ForgeItemData{starlight=new ForgeStarlight(new[]{5,0,2,5,2})};data.starlight.Read(0);data.starlight.Swap(0,2,.4);data.starlight.Remove(1,.25);
  var loaded=JsonUtility.FromJson<ForgeItemData>(JsonUtility.ToJson(data));
  Require(loaded.starlight.Initial.SequenceEqual(data.starlight.Initial)&&loaded.starlight.Values.SequenceEqual(data.starlight.Values)&&loaded.starlight.Revealed(1)&&loaded.starlight.Swaps==1&&loaded.starlight.Removals==1&&Math.Abs(loaded.starlight.Seconds-.65)<.0001,"serialized state");
  var cameraRoot=new GameObject("Star UI validation camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.18f);camera.orthographic=true;
  var host=new GameObject("Star guide validation",typeof(RectTransform),typeof(Canvas));var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab");var original=prefab.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="Guide");var guide=UnityEngine.Object.Instantiate(original,host.transform,false);guide.gameObject.SetActive(true);guide.anchorMin=guide.anchorMax=guide.pivot=new Vector2(.5f,.5f);guide.anchoredPosition=Vector2.zero;
  var nodes=guide.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.Last());nodes["GuideIndex"].gameObject.SetActive(false);nodes["GuideDetail"].gameObject.SetActive(true);nodes["GuideNew"].gameObject.SetActive(false);nodes["GuideCopy"].gameObject.SetActive(true);nodes["GuideCopy"].GetComponentInChildren<Text>().text="별철 연습";
  var title=nodes["GuideHeading"].GetComponent<Text>();var body=nodes["GuideBody"].GetComponent<Text>();guide.GetComponent<ForgeLegacyBook>().Layout(false,false);ForgeLegacyUIFont.Apply(guide);
  var minimum=guide.GetComponentsInChildren<ForgeUIDrag>(true).First(d=>d.resize).minimum;
  var panel=ForgeStarlightPanel.Get();typeof(ForgeStarlightPanel).GetField("owner",Private).SetValue(panel,null);panel.GetComponent<CanvasScaler>().enabled=false;var panelCanvas=panel.GetComponent<Canvas>();panelCanvas.renderMode=RenderMode.ScreenSpaceCamera;panelCanvas.worldCamera=camera;panelCanvas.planeDistance=5;
  try{
   foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160),new Vector2Int(640,480)}){
    var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
    foreach(bool english in new[]{false,true})foreach(bool small in new[]{false,true}){
     panelCanvas.enabled=false;host.SetActive(true);guide.sizeDelta=small?minimum:new Vector2(Mathf.Min(850,size.x-100),Mathf.Min(660,size.y-30));title.text=ForgeMaterialGuide.Find(7).Heading;body.text=BlueprintResearchGuide.MaterialInfo(7,english)+"\n\n"+BlueprintResearchGuide.FormatGold(ForgeMaterialGuide.Find(7).Description);
     for(int i=0;i<4;i++){Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)body.transform.parent);}
     Require(body.preferredHeight<=body.rectTransform.rect.height+2,"guide body height "+size);Require(title.preferredHeight<=title.rectTransform.rect.height+2,"guide title");
     var scroll=guide.GetComponentsInChildren<ScrollRect>(true).First(s=>s.content&&body.transform.IsChildOf(s.content));
     foreach(float position in new[]{1f,.5f,0f}){scroll.verticalNormalizedPosition=position;Canvas.ForceUpdateCanvases();Capture(camera,target,"guide-"+size.x+"-"+english+"-"+small+"-"+position.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));}
     report.AppendLine("Guide "+size+" English="+english+" minimum="+small+" body="+body.rectTransform.rect.height+" preferred="+body.preferredHeight);
    }
    host.SetActive(false);panelCanvas.scaleFactor=Mathf.Sqrt(size.x/800f*size.y/600f);panel.OpenPractice(data.starlight);
    for(int i=0;i<5;i++){Canvas.ForceUpdateCanvases();panel.SendMessage("LateUpdate");foreach(var group in panel.GetComponentsInChildren<VerticalLayoutGroup>())LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);}
    foreach(var button in panel.GetComponentsInChildren<Button>()){var label=button.GetComponentInChildren<Text>();Require(!string.IsNullOrEmpty(label.text)&&label.preferredHeight<=label.rectTransform.rect.height+2,"practice button "+button.name);}
    var state=panel.GetComponentsInChildren<Text>().Single(t=>t.name=="별빛 상태");Require(state.text.Contains("교환 0회")&&state.text.Contains("제거 0회")&&state.text.Contains("0.00초")&&state.text.Contains("+0G")&&!state.text.Contains("【0"),"reset presentation");
    foreach(var label in panel.GetComponentsInChildren<Text>().Where(t=>t.name=="제목"||t.name=="별빛 상태"))Require(label.preferredHeight<=label.rectTransform.rect.height+2,"practice prose clipping "+size);
    Capture(camera,target,"practice-"+size.x);report.AppendLine("Practice "+size+" button/prose metrics and reset state passed");camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);
   }
   File.WriteAllText(Output+"/results.txt",report+"PASS serialization and UI metrics. PNGs require visual review.");
  }finally{UnityEngine.Object.DestroyImmediate(panel.gameObject);UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(cameraRoot);}
 }
 static void Capture(Camera camera,RenderTexture target,string name){camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
 public static void RunBatch(){try{Run();EditorApplication.Exit(0);}catch(Exception error){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/failure.txt",error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}}
}
