using UnityEngine;
using UnityEngine.UI;

public sealed partial class ForgeGameUI
{
 CanvasGroup tutorialGroup;
 Image tutorialBlackout;
 bool tutorialStyled;
 string tutorialLastHeading;
 Text tutorialHintText;
 static readonly Color TutorialHintGreen=new Color(.08f,.40f,.18f);
 public void ShowTutorialHint(string value,bool visible,bool available)
 {
  var button=buttons["TutorialHint"];
  if(!tutorialHintText){
   var actions=Node("TutorialActions");
   var block=new GameObject("TutorialHintBlock",typeof(RectTransform),typeof(VerticalLayoutGroup));
   block.layer=actions.gameObject.layer;block.transform.SetParent(actions.parent,false);
   block.transform.SetSiblingIndex(actions.GetSiblingIndex());
   var layout=block.GetComponent<VerticalLayoutGroup>();layout.spacing=8;
   layout.childControlWidth=layout.childControlHeight=true;
   layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
   button.transform.SetParent(block.transform,false);
   var label=new GameObject("TutorialHintText",typeof(RectTransform),typeof(Text),typeof(LayoutElement));
   label.layer=block.layer;label.transform.SetParent(block.transform,false);
   tutorialHintText=label.GetComponent<Text>();tutorialHintText.font=texts["TutorialInstructions"].font;
   tutorialHintText.fontSize=20;tutorialHintText.supportRichText=false;tutorialHintText.raycastTarget=false;
   tutorialHintText.horizontalOverflow=HorizontalWrapMode.Wrap;tutorialHintText.verticalOverflow=VerticalWrapMode.Overflow;
  }
  button.transform.parent.gameObject.SetActive(available);
  tutorialHintText.text=value;tutorialHintText.color=TutorialHintGreen;
  tutorialHintText.gameObject.SetActive(available&&visible);
  Label("TutorialHint",visible?"힌트 숨기기":"힌트 보기");
 }
 void UpdateTutorialPresentation()
 {
  if(!game||!nodes.TryGetValue("Tutorial",out var tutorial)||!tutorial)return;
  // Unity missing/destroyed components are not necessarily CLR null.
  if(!tutorialGroup){
   tutorialGroup=tutorial.GetComponent<CanvasGroup>();
   if(!tutorialGroup)tutorialGroup=tutorial.gameObject.AddComponent<CanvasGroup>();
  }
  if(!tutorialStyled||!tutorialBlackout)StyleTutorial();
  tutorialGroup.alpha=game.TutorialOpacity;
  tutorialGroup.interactable=game.TutorialInputReady;
  // Keep the invisible intro modal so clicks cannot reach the workspace.
  tutorialGroup.blocksRaycasts=true;
  tutorialBlackout.color=new Color(0,0,0,game.TutorialBlackout);
  tutorialBlackout.gameObject.SetActive(game.TutorialBlackout>0);
  if(tutorialBlackout.gameObject.activeSelf)tutorialBlackout.transform.SetAsLastSibling();
  if(tutorialGroup.gameObject.activeInHierarchy)FitTutorialText();
 }
 public void ShowTutorialInstructions(ForgeTutorialLesson lesson)
 {
  texts["TutorialInstructions"].supportRichText=!lesson.Crafted;
  Text("TutorialInstructions",lesson.Crafted?lesson.SaleInstructions:(lesson.Stage==0?"<size=28><b>"+ForgeTutorialLesson.Titles[0]+"</b></size>":ForgeTutorialLesson.Titles[lesson.Stage])+"\n\n"+lesson.InstructionBody);
 }
 void StyleTutorial()
 {
  var tutorial=Node("Tutorial");
  var welcome=texts["TutorialWelcomeText"];welcome.text=welcome.text.Replace("\n\n연습용 재료를 사용하므로 실제 재고는 줄지 않습니다.","").Replace("연습용 재료를 사용하므로 실제 재고는 줄지 않습니다.","");
  tutorialGroup.alpha=0;
  tutorial.GetComponent<Image>().color=Color.clear;
  // Match the existing guide's warm paper and dark ink; reuse authored button skins.
  var paper=Node("Guide").Find("LegacyPaper");
  var panel=Node("TutorialScroll").GetComponent<Image>();
  panel.sprite=null;panel.color=paper?paper.GetComponent<Image>().color:new Color(.96f,.92f,.82f);
  var ink=texts["GuideBody"].color;
  foreach(var text in tutorial.GetComponentsInChildren<Text>(true)){
   text.font=texts["GuideBody"].font;
   if(!text.GetComponentInParent<Button>()&&!text.GetComponentInParent<InputField>()){
    text.color=text.name=="TutorialHintText"?TutorialHintGreen:ink;text.fontSize=text.name=="TutorialHeading"?26:20;
    text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
   }
  }
  var prototype=buttons["GuideClose"];
  foreach(var button in tutorial.GetComponentsInChildren<Button>(true)){
   var surface=button.GetComponent<Image>();var source=prototype.GetComponent<Image>();
   if(surface){surface.sprite=source.sprite;surface.type=source.type;surface.color=source.color;}
   button.colors=prototype.colors;
   var label=button.GetComponentInChildren<Text>(true);
   if(label){label.color=prototype.GetComponentInChildren<Text>(true).color;label.fontSize=20;label.resizeTextForBestFit=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;}
  }
  var editor=Node("TutorialEditor").GetComponent<Image>();
  var window=Node("Debug").GetComponent<Image>();
  editor.sprite=window.sprite;editor.type=window.type;editor.color=window.color;
  texts["TutorialEditorTitle"].color=texts["DebugTitle"].color;
  texts["TutorialEditorTitle"].fontSize=20;
  inputs["TutorialInput"].textComponent.fontSize=20;
  var title=texts["TutorialEditorTitle"].rectTransform;title.offsetMax=new Vector2(-12,title.offsetMax.y);

  var existing=transform.Find("TutorialBlackout");
  var overlay=existing?existing.gameObject:new GameObject("TutorialBlackout",typeof(RectTransform),typeof(Image));
  overlay.layer=gameObject.layer;overlay.transform.SetParent(transform,false);
  var rect=(RectTransform)overlay.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
  tutorialBlackout=overlay.GetComponent<Image>();
  if(!tutorialBlackout)tutorialBlackout=overlay.AddComponent<Image>();
  tutorialBlackout.color=Color.clear;tutorialBlackout.raycastTarget=true;
  tutorialStyled=true;
 }
 void FitTutorialText()
 {
  if(tutorialLastHeading!=texts["TutorialHeading"].text){tutorialLastHeading=texts["TutorialHeading"].text;Node("TutorialScroll").GetComponent<ScrollRect>().verticalNormalizedPosition=1;}
  bool changed=false;
  foreach(var text in tutorialGroup.GetComponentsInChildren<Text>(false)){
   if(text.GetComponentInParent<InputField>())continue;
   var button=text.GetComponentInParent<Button>();
   var element=button?button.GetComponent<LayoutElement>():text.GetComponent<LayoutElement>();
   if(!element)continue;
   float width=Mathf.Max(1,text.rectTransform.rect.width);
   var settings=text.GetGenerationSettings(new Vector2(width,0));
   float height=text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text,settings)/text.pixelsPerUnit;
   float wanted=Mathf.Max(button?54:24,height+(button?16:4));
   if(Mathf.Abs(element.preferredHeight-wanted)>.5f){element.minHeight=element.preferredHeight=wanted;changed=true;}
  }
  var actions=Node("TutorialActions");float rowHeight=54;
  foreach(Transform child in actions)if(child.gameObject.activeSelf&&child.TryGetComponent<LayoutElement>(out var item))rowHeight=Mathf.Max(rowHeight,item.preferredHeight);
  var row=actions.GetComponent<LayoutElement>();if(Mathf.Abs(row.preferredHeight-rowHeight)>.5f){row.minHeight=row.preferredHeight=rowHeight;changed=true;}
  if(changed)LayoutRebuilder.ForceRebuildLayoutImmediate(Node("TutorialScroll").GetComponent<ScrollRect>().content);
 }
}
