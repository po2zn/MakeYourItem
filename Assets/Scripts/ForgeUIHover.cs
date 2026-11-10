using UnityEngine;using UnityEngine.EventSystems;
public sealed class ForgeUIHover:MonoBehaviour,IPointerEnterHandler,IPointerMoveHandler,IPointerExitHandler {
 public string description;ForgeGameUI ui;bool inside,owns;float exited;public void Bind(ForgeGameUI panel){ui=panel;}
 public void OnPointerEnter(PointerEventData e){inside=true;if(ui!=null&&!string.IsNullOrEmpty(description)){ui.ShowCodeHelp(description,e.position);owns=true;}}
 public void OnPointerMove(PointerEventData e){OnPointerEnter(e);}public void OnPointerExit(PointerEventData e){inside=false;exited=Time.unscaledTime;}
 void Update(){if(!owns||inside||ui==null||Time.unscaledTime-exited<.25f)return;
#if ENABLE_INPUT_SYSTEM
 var mouse=UnityEngine.InputSystem.Mouse.current;if(mouse!=null&&RectTransformUtility.RectangleContainsScreenPoint(ui.codeHelp,mouse.position.ReadValue()))return;
#endif
 ui.HideCodeHelp();owns=false;}
 void OnDisable(){if(owns)ui?.HideCodeHelp();owns=false;inside=false;}
}
