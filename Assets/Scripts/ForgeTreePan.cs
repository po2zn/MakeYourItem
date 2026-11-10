using UnityEngine;
using UnityEngine.EventSystems;

// One transform carries cards, labels and static connection lines together.
public sealed class ForgeTreePan : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public RectTransform content;
    public Vector2 focusPosition=new Vector2(16,-24);
    public Vector2 mapSize;
    [Range(.01f,.2f)] public float zoomStep=.08f;
    RectTransform View=>(RectTransform)transform;
    bool dragging;Vector2 start,startPosition;
    public bool Dragging=>dragging;
    public void OnBeginDrag(PointerEventData e)
    {
        var card=e.pointerPressRaycast.gameObject==null?null:e.pointerPressRaycast.gameObject.GetComponentInParent<ForgeTreeCard>();
        if(e.button!=PointerEventData.InputButton.Right&&(e.button!=PointerEventData.InputButton.Left||card!=null))return;
        dragging=true;RectTransformUtility.ScreenPointToLocalPointInRectangle(View,e.position,e.pressEventCamera,out start);startPosition=content.anchoredPosition;
        GetComponentInParent<ForgeTreePanel>().HideTooltip();
    }
    public void OnDrag(PointerEventData e){if(!dragging)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(View,e.position,e.pressEventCamera,out var point);content.anchoredPosition=startPosition+point-start;}
    public void OnEndDrag(PointerEventData e){dragging=false;}
    public void OnScroll(PointerEventData e)
    {
        if(dragging)return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(View,e.position,e.pressEventCamera,out var local);
        var cursor=local-new Vector2(View.rect.xMin,View.rect.yMax);
        float ticks=e.scrollDelta.y;
#if ENABLE_INPUT_SYSTEM
        if(e.currentInputModule is UnityEngine.InputSystem.UI.InputSystemUIInputModule inputModule)
            ticks/=Mathf.Max(.001f,inputModule.scrollDeltaPerTick);
#endif
        float before=content.localScale.x,after=Mathf.Clamp(before*Mathf.Pow(1+zoomStep,Mathf.Clamp(ticks,-4,4)),.25f,1.8f);
        content.anchoredPosition=cursor-(cursor-content.anchoredPosition)*(after/before);content.localScale=Vector3.one*after;
        GetComponentInParent<ForgeTreePanel>().HideTooltip();
    }
    public void ResetView(){dragging=false;content.localScale=Vector3.one;content.anchoredPosition=focusPosition;}
    public void Fit(){dragging=false;float zoom=Mathf.Clamp(Mathf.Min((View.rect.width-40)/mapSize.x,(View.rect.height-40)/mapSize.y),.15f,1);content.localScale=Vector3.one*zoom;content.anchoredPosition=new Vector2((View.rect.width-mapSize.x*zoom)/2,-20);}
    public void CancelDrag(){dragging=false;}
    void OnDisable(){dragging=false;}
    void OnApplicationFocus(bool focus){if(!focus)dragging=false;}
}
