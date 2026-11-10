using UnityEngine;
using UnityEngine.EventSystems;
public sealed class ForgeWorkspacePan:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IScrollHandler
{
 public ForgeGameUI ui;Vector2 previous;bool dragging;
 public void OnBeginDrag(PointerEventData e){previous=e.position;dragging=true;}
 public void OnDrag(PointerEventData e){if(!dragging)return;var game=Object.FindAnyObjectByType<ForgeMvp>();game.GameWorkspaceMove(e.position-previous);previous=e.position;}
 public void OnEndDrag(PointerEventData e){dragging=false;}
 public void OnScroll(PointerEventData e){Object.FindAnyObjectByType<ForgeMvp>().GameWorkspaceZoom(e.scrollDelta.y,e.position);}
 void OnDisable(){dragging=false;}void OnApplicationFocus(bool focus){if(!focus)dragging=false;}
}
