using UnityEngine;using UnityEngine.UI;
public sealed class ForgeLegacyBook:MonoBehaviour {
 public RectTransform rail,detail,index,search,nav;public Button toggle;bool bookmarks=true;
 public void Layout(bool introduction,bool fresh){foreach(var label in nav.GetComponentsInChildren<Text>(true)){label.resizeTextForBestFit=false;label.fontSize=14;label.rectTransform.offsetMin=new Vector2(4,4);label.rectTransform.offsetMax=new Vector2(-4,-4);}search.gameObject.SetActive(!introduction);nav.gameObject.SetActive(!introduction||fresh);nav.anchoredPosition=new Vector2(34,introduction?-72:-108);float top=introduction?(fresh?114:70):150;detail.offsetMax=new Vector2(-22,-top);index.offsetMax=new Vector2(-22,-top);}
 Text[] indexLabels;
 void LateUpdate(){LayoutIndexRows();}
 public void LayoutIndexRows(){
  if(!index)return;if(indexLabels==null)indexLabels=index.GetComponentsInChildren<Text>(true);
  foreach(var label in indexLabels){if(!label||!label.gameObject.activeInHierarchy)continue;var row=label.transform.parent.GetComponent<LayoutElement>();if(!row)continue;float height=Mathf.Max(54,Mathf.Ceil(label.preferredHeight)+12);if(Mathf.Abs(row.preferredHeight-height)>.5f)row.preferredHeight=height;}
 }
 void Awake(){toggle.onClick.AddListener(()=>{bookmarks=!bookmarks;rail.gameObject.SetActive(bookmarks);toggle.GetComponentInChildren<Text>().text=bookmarks?"◀":"▶";});}
}
