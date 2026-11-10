using UnityEngine;
using UnityEngine.EventSystems;
// Pointer ownership is local to the dragged handle, never shared with text selection.
public sealed class ForgeUIDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerDownHandler
{
 public RectTransform target; public bool resize; public Vector2 minimum=new Vector2(340,200); Vector2 start,position,size; bool dragging;
 public void OnPointerDown(PointerEventData e){target.SetAsLastSibling();}
 public void OnBeginDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;dragging=true;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)target.parent,e.position,e.pressEventCamera,out start);position=target.anchoredPosition;size=target.sizeDelta;ForgeMouseTrace.Mark("UI_DRAG_BEGIN",target.name+" resize="+resize);}
 public void OnDrag(PointerEventData e){if(!dragging)return;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)target.parent,e.position,e.pressEventCamera,out var point);var delta=point-start;if(resize)target.sizeDelta=Vector2.Max(minimum,size+new Vector2(delta.x,-delta.y));else target.anchoredPosition=position+delta;}
 public void OnEndDrag(PointerEventData e){dragging=false;ForgeMouseTrace.Mark("UI_DRAG_END",target.name);}
 void OnDisable(){dragging=false;}void OnApplicationFocus(bool focused){if(!focused)dragging=false;}
}
