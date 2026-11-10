using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;
public sealed class ForgeUIWindow:MonoBehaviour,IPointerDownHandler {
 public RectTransform rect;public GameObject body,resize;public Button fold;public float foldedHeight=42;float height;bool collapsed;
 void Awake(){fold.onClick.AddListener(()=>{collapsed=!collapsed;if(collapsed){height=rect.sizeDelta.y;rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,foldedHeight);}else rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);body.SetActive(!collapsed);resize.SetActive(!collapsed);});}
 public void OnPointerDown(PointerEventData e){rect.SetAsLastSibling();}
}
