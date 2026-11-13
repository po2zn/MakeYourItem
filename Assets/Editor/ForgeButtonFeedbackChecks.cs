using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class ForgeButtonFeedbackChecks {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 public static void RunBatch(){GameObject root=null,audioRoot=null;try{
 Directory.CreateDirectory("Artifacts/ButtonFeedback");
 audioRoot=new GameObject("Button test audio",typeof(AudioSource),typeof(ForgeSoundEffects));var sound=audioRoot.GetComponent<ForgeSoundEffects>();typeof(ForgeSoundEffects).GetField("instance",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,sound);typeof(ForgeSoundEffects).GetField("source",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(sound,audioRoot.GetComponent<AudioSource>());
 root=new GameObject("Button test",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));var button=root.GetComponent<Button>();ForgeSfxButton.Bind(button);ForgeSfxButton.Bind(button);Check(root.GetComponents<ForgeSfxButton>().Length==1,"idempotent binding");var feedback=root.GetComponent<ForgeSfxButton>();button.onClick=new Button.ButtonClickedEvent();var pointer=new PointerEventData(null){button=PointerEventData.InputButton.Left};feedback.OnPointerDown(pointer);var overlay=root.transform.Find("ButtonPressFeedback");Check(overlay&&overlay.gameObject.activeSelf,"pressed after click event replacement");Check(!overlay.GetComponent<Image>().raycastTarget&&overlay.GetComponent<LayoutElement>().ignoreLayout,"overlay does not block input or change layout");feedback.OnPointerUp(pointer);Check(overlay.gameObject.activeSelf,"brief release feedback");feedback.OnPointerExit(pointer);Check(!overlay.gameObject.activeSelf,"exit clears feedback");button.interactable=false;feedback.OnPointerDown(pointer);Check(!overlay.gameObject.activeSelf,"disabled button does not respond");button.interactable=true;feedback.OnSubmit(new BaseEventData(null));Check(overlay.gameObject.activeSelf,"keyboard submission feedback");feedback.enabled=false;typeof(ForgeSfxButton).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(feedback,null);Check(!overlay.gameObject.activeSelf,"disable cleans feedback");
 var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ForgeResearchUI.prefab");var fuel=tree.GetComponentsInChildren<ForgeTreeCard>(true).Single(c=>c.offerId=="function-Fuel");var icon=fuel.GetComponentInChildren<RawImage>(true);var artwork=Resources.Load<ForgeMaterialIcons>("BlacksmithMaterialIcons");Check(icon&&icon.texture==artwork.textures[4]&&icon.uvRect==artwork.Region(4),"fuel uses coal artwork and crop");float aspect=icon.texture.width*icon.uvRect.width/(icon.texture.height*icon.uvRect.height);Check(Math.Abs(icon.rectTransform.rect.width/icon.rectTransform.rect.height-aspect)<.001,"coal aspect preserved");
 File.WriteAllText("Artifacts/ButtonFeedback/results.txt","PASS press/release/exit/keyboard/disabled cleanup, replaced click event, no blocking or layout mutation, coal reference/crop/aspect.");Debug.Log("BUTTON_FEEDBACK_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{if(root)UnityEngine.Object.DestroyImmediate(root);if(audioRoot)UnityEngine.Object.DestroyImmediate(audioRoot);}}
}
