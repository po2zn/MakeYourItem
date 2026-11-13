using UnityEngine;
using UnityEngine.UI;

// Decorative frame stays inside the paper's existing margin and inherits its fade/scale.
public static class ForgeTutorialFrame
{
 public static void Apply(RectTransform panel)
 {
  if(panel.Find("TutorialPaperFrame"))return;
  var root=new GameObject("TutorialPaperFrame",typeof(RectTransform));root.layer=panel.gameObject.layer;
  var rect=(RectTransform)root.transform;rect.SetParent(panel,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
  var dark=new Color(.22f,.16f,.11f);var bronze=new Color(.60f,.43f,.24f);var light=new Color(.88f,.75f,.49f);
  Border(rect,"Outer",0,4,dark);Border(rect,"Brass",4,1,bronze);Border(rect,"Inset",7,1,new Color(.56f,.43f,.26f,.45f));
  foreach(float x in new[]{0f,1f})foreach(float y in new[]{0f,1f}){
   var anchor=new Vector2(x,y);var direction=new Vector2(x==0?1:-1,y==0?1:-1);
   Mark(rect,"Corner plate",anchor,Vector2.Scale(direction,new Vector2(2,2)),new Vector2(22,4),anchor,bronze);
   Mark(rect,"Corner plate",anchor,Vector2.Scale(direction,new Vector2(2,2)),new Vector2(4,22),anchor,bronze);
   var rivet=Mark(rect,"Rivet",anchor,Vector2.Scale(direction,new Vector2(5,5)),new Vector2(3,3),new Vector2(.5f,.5f),light);rivet.localRotation=Quaternion.Euler(0,0,45);
  }
  foreach(float y in new[]{0f,1f}){
   var accent=Mark(rect,"Center inlay",new Vector2(.5f,y),new Vector2(0,y==0?4:-4),new Vector2(5,5),new Vector2(.5f,.5f),light);accent.localRotation=Quaternion.Euler(0,0,45);
  }
 }
 static void Border(RectTransform parent,string name,float inset,float thickness,Color color)
 {
  foreach(bool vertical in new[]{false,true})foreach(float side in new[]{0f,1f}){
   var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.gameObject.layer=parent.gameObject.layer;image.color=color;image.raycastTarget=false;
   var r=image.rectTransform;r.SetParent(parent,false);r.anchorMin=vertical?new Vector2(side,0):new Vector2(0,side);r.anchorMax=vertical?new Vector2(side,1):new Vector2(1,side);r.pivot=new Vector2(.5f,.5f);
   if(vertical){r.sizeDelta=new Vector2(thickness,-2*inset);r.anchoredPosition=new Vector2((side==0?1:-1)*(inset+thickness*.5f),0);}
   else{r.sizeDelta=new Vector2(-2*inset,thickness);r.anchoredPosition=new Vector2(0,(side==0?1:-1)*(inset+thickness*.5f));}
  }
 }
 static RectTransform Mark(RectTransform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Vector2 pivot,Color color)
 {
  var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.gameObject.layer=parent.gameObject.layer;image.color=color;image.raycastTarget=false;
  var r=image.rectTransform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;return r;
 }
}
