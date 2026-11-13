using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pointer feedback survives callers replacing onClick when recycling buttons.
public sealed class ForgeSfxButton:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,IPointerClickHandler,ISubmitHandler {
 Button button;Image press;bool held;float until;
 bool Available {get{if(!button)button=GetComponent<Button>();return button&&button.IsActive()&&button.IsInteractable();}}
 void Show(){
  if(!press){var child=new GameObject("ButtonPressFeedback",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(LayoutElement));child.layer=gameObject.layer;child.transform.SetParent(transform,false);child.GetComponent<LayoutElement>().ignoreLayout=true;press=child.GetComponent<Image>();press.raycastTarget=false;var rect=press.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.one*2;rect.offsetMax=Vector2.one*-2;}
  press.transform.SetAsLastSibling();press.color=new Color(.45f,.22f,.05f,.3f);press.gameObject.SetActive(true);
 }
 void Sound(){if(!GetComponent<ForgeTreeCard>())ForgeSoundEffects.Click();}
 public void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left||!Available)return;held=true;Sound();Show();}
 public void OnPointerUp(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;held=false;until=Time.unscaledTime+.12f;}
 public void OnPointerExit(PointerEventData e){held=false;until=0;if(press)press.gameObject.SetActive(false);}
 public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&Available){Show();until=Time.unscaledTime+.12f;}}
 public void OnSubmit(BaseEventData e){if(!Available)return;Sound();Show();until=Time.unscaledTime+.15f;}
 void Update(){if(press&&press.gameObject.activeSelf&&(!Available||!held&&Time.unscaledTime>=until))press.gameObject.SetActive(false);}
 void OnDisable(){held=false;until=0;if(press)press.gameObject.SetActive(false);}
 public static void Bind(Component root){foreach(var button in root.GetComponentsInChildren<Button>(true))if(!button.GetComponent<ForgeSfxButton>())button.gameObject.AddComponent<ForgeSfxButton>();}
}
