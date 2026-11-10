using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;
public sealed class ForgeLegacyCodeTitle:MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler {
 public InputField field;public ForgeUIDrag drag;bool moving;
 void Awake(){field.readOnly=true;field.onEndEdit.AddListener(_=>field.readOnly=true);}
 public void OnPointerClick(PointerEventData e){if(e.clickCount>=2){field.readOnly=false;field.ActivateInputField();}else if(field.readOnly)field.DeactivateInputField();}
 public void OnBeginDrag(PointerEventData e){moving=field.readOnly;if(moving){field.DeactivateInputField();drag.OnBeginDrag(e);}}
 public void OnDrag(PointerEventData e){if(moving)drag.OnDrag(e);}
 public void OnEndDrag(PointerEventData e){if(moving)drag.OnEndDrag(e);moving=false;}
}
