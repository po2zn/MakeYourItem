using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
public static class BlueprintMatchUIChecks
{
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 public static void RunBatch(){
  try{
   Debug.Log(BlueprintMatchChecks.Run());
   var game=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeGameUI.prefab");
   var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeResearchUI.prefab");
   var panel=tree.GetComponentInChildren<ForgeTreePanel>(true);
   var card=panel.cards.Single(c=>c.offerId=="function-Match");var random=panel.cards.Single(c=>c.offerId=="function-Random");
   Require(panel.edges.Any(e=>e.parentOffer=="function-Random"&&e.lines.All(l=>l)),"Random connection");
   Require(((RectTransform)card.transform).anchoredPosition.x>((RectTransform)random.transform).anchoredPosition.x,"Match card follows random");
   int guideIndex=BlueprintGuide.Entries.ToList().FindIndex(e=>e.Id=="match");
   Require(game.GetComponentsInChildren<ForgeUIAction>(true).Count(a=>a.action=="guide-entry:"+guideIndex)==1,"Guide action serialized");
   Directory.CreateDirectory("Temp/MatchCase/UI");
   var canvasObject=new GameObject("Match UI verification",typeof(RectTransform),typeof(Canvas));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
   var cameraObject=new GameObject("Match UI camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.18f);camera.orthographic=true;canvas.worldCamera=camera;canvas.planeDistance=10;
   var original=game.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="Guide");
   var guide=UnityEngine.Object.Instantiate(original,canvas.transform,false);guide.gameObject.SetActive(true);guide.anchorMin=guide.anchorMax=new Vector2(.5f,.5f);guide.pivot=new Vector2(.5f,.5f);guide.anchoredPosition=Vector2.zero;
   var nodes=guide.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.Last());
   nodes["GuideIndex"].gameObject.SetActive(false);nodes["GuideDetail"].gameObject.SetActive(true);nodes["GuideNew"].gameObject.SetActive(false);
   var title=nodes["GuideHeading"].GetComponent<Text>();var body=nodes["GuideBody"].GetComponent<Text>();var example=ForgeGuideExample.Create(body);
   var entry=BlueprintGuide.Entries[guideIndex];var book=guide.GetComponent<ForgeLegacyBook>();if(book)book.Layout(false,false);
   Vector2 minimum=guide.GetComponentInChildren<ForgeUIDrag>(true).minimum;var resize=guide.GetComponentsInChildren<ForgeUIDrag>(true).FirstOrDefault(d=>d.resize);if(resize)minimum=resize.minimum;
   var report=new System.Text.StringBuilder();
   foreach(var resolution in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)})foreach(bool english in new[]{false,true})foreach(bool small in new[]{false,true}){
    var target=new RenderTexture(resolution.x,resolution.y,24);camera.targetTexture=target;
    guide.sizeDelta=small?minimum:new Vector2(Mathf.Min(850,resolution.x-120),Mathf.Min(660,resolution.y-30));
    title.text=entry.HeadingFor(english);body.text=entry.DescriptionFor(english);example.Show(entry.ExampleFor(english),entry.NoteFor(english));
    for(int pass=0;pass<4;pass++){Canvas.ForceUpdateCanvases();example.SendMessage("Resize");LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)body.transform.parent);}
    Require(body.preferredHeight<=body.rectTransform.rect.height+2,"Body clipped "+resolution+" "+small);Require(title.preferredHeight<=title.rectTransform.rect.height+2,"Title clipped");
    var code=example.GetComponentsInChildren<Text>().First(t=>t.name=="GuideExampleText");Require(code.preferredHeight<=code.rectTransform.rect.height+2,"Example clipped");
    report.AppendLine(resolution+" english="+english+" minimum="+small+" body="+body.preferredHeight+"/"+body.rectTransform.rect.height+" code="+code.preferredHeight+"/"+code.rectTransform.rect.height);
    camera.Render();RenderTexture.active=target;var image=new Texture2D(resolution.x,resolution.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,resolution.x,resolution.y),0,0);image.Apply();File.WriteAllBytes("Temp/MatchCase/UI/guide-"+resolution.x+"-"+english+"-"+small+".png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
   }
   UnityEngine.Object.DestroyImmediate(guide.gameObject);
   var cardClone=UnityEngine.Object.Instantiate(card,canvas.transform,false);cardClone.title.text=ForgeTechnology.Find("function-Match").Title;cardClone.price.text="5KG";cardClone.state.text="선행 구매 필요";var rect=(RectTransform)cardClone.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;Canvas.ForceUpdateCanvases();Require(cardClone.title.preferredHeight<=cardClone.title.rectTransform.rect.height+2,"Card title clipped");
   File.WriteAllText("Temp/MatchCase/UI/results.txt",report.ToString()+"PASS serialized guide/card/edge; new card title fits.");Debug.Log("MATCH_UI_PASS");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/MatchCase/UI-failure.txt",e.ToString());EditorApplication.Exit(1);}
 }
}
