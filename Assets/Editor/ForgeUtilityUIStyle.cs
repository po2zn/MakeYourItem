using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// Shared authored treatment for the workshop's utility windows.
public static class ForgeUtilityUIStyle {
 static readonly Color Ink=new Color(.18f,.15f,.12f), Paper=new Color(.96f,.92f,.82f), Header=new Color(.16f,.18f,.22f,.98f), Frame=new Color(.25f,.16f,.11f);
 static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
 static void Fill(RectTransform r,float left,float bottom,float right,float top){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
 static void ButtonStyle(Button b,bool chrome=false){
 var image=b.GetComponent<Image>();image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");image.type=Image.Type.Sliced;image.color=chrome?new Color(.29f,.31f,.35f):new Color(.84f,.78f,.65f);b.targetGraphic=image;
 var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.08f,1.08f,1.08f);colors.pressedColor=new Color(.8f,.8f,.8f);colors.disabledColor=new Color(.8f,.8f,.8f,.65f);b.colors=colors;
 var t=b.GetComponentInChildren<Text>(true);t.color=chrome?new Color(.96f,.94f,.88f):Ink;t.fontSize=t.resizeTextMaxSize=18;t.resizeTextMinSize=14;t.resizeTextForBestFit=true;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;Fill(t.rectTransform,10,5,10,5);
 }
 public static void ApplyTo(GameObject root){
 var layout=root.GetComponent<ForgeLegacyLayout>();
 root.GetComponent<ForgeGameUI>().workspace.SetAsLastSibling();
 if(layout&&layout.materialGrid){
 layout.materialGrid.cellSize=new Vector2(72,74);layout.materialGrid.spacing=new Vector2(6,6);layout.materialGrid.padding=new RectOffset();
 foreach(var icon in root.GetComponentsInChildren<RawImage>(true).Where(i=>i.name.StartsWith("MaterialIcon"))){
 var r=icon.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(14,0);r.sizeDelta=new Vector2(44,44);
 }
 foreach(var count in root.GetComponentsInChildren<Text>(true).Where(t=>t.name.StartsWith("MaterialCount"))){
 var r=count.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(0,-48);r.sizeDelta=new Vector2(72,24);count.fontSize=20;count.alignment=TextAnchor.MiddleCenter;count.resizeTextForBestFit=true;count.resizeTextMinSize=12;count.resizeTextMaxSize=20;count.horizontalOverflow=HorizontalWrapMode.Wrap;count.verticalOverflow=VerticalWrapMode.Truncate;
 }
 }
 foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="LegacyZoom"||t.name=="Zoom").ToArray())if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);
 if(layout)layout.states=layout.states.Where(t=>t).ToArray();
 foreach(string id in new[]{"Sales","Debug","Developer"}){
 var window=(RectTransform)Find(root.transform,id);var controller=window.GetComponent<ForgeUIWindow>();controller.foldedHeight=54;
 var frame=window.GetComponent<Image>();frame.sprite=null;frame.color=Frame;
 var body=controller.body.transform;var paper=body.Find("UtilityPaper") as RectTransform;
 if(!paper){paper=new GameObject("UtilityPaper",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();paper.gameObject.layer=5;paper.SetParent(body,false);paper.SetAsFirstSibling();}
 Fill(paper,8,8,8,54);paper.GetComponent<Image>().color=Paper;paper.GetComponent<Image>().raycastTarget=false;
 var header=(RectTransform)Find(window,id+"Drag");Fill(header,0,0,0,0);header.anchorMin=new Vector2(0,1);header.sizeDelta=new Vector2(0,54);header.anchoredPosition=Vector2.zero;header.GetComponent<Image>().sprite=null;header.GetComponent<Image>().color=Header;
 var title=Find(window,id+"Title").GetComponent<Text>();title.text=id=="Sales"?"상점":id=="Debug"?"디버그 출력":"디버그 도구";title.fontSize=22;title.fontStyle=FontStyle.Bold;title.color=new Color(.93f,.94f,.96f);Fill(title.rectTransform,14,4,100,4);
 foreach(var b in window.GetComponentsInChildren<Button>(true))ButtonStyle(b,b.transform.IsChildOf(header));
 foreach(string suffix in new[]{"Close","Fold"}){var r=(RectTransform)Find(window,id+suffix);r.anchorMin=r.anchorMax=r.pivot=Vector2.one;r.anchoredPosition=new Vector2(suffix=="Close"?-6:-50,-6);r.sizeDelta=new Vector2(40,42);}
 foreach(var t in body.GetComponentsInChildren<Text>(true)){t.color=Ink;if(!t.GetComponentInParent<Button>()){t.fontSize=18;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;}}
 var scroll=(RectTransform)Find(window,id+"Scroll");Fill(scroll,10,id=="Sales"?76:20,10,64);
 var content=scroll.GetComponent<ScrollRect>().content;var vertical=content.GetComponent<VerticalLayoutGroup>();if(vertical){vertical.padding=new RectOffset(12,12,8,12);vertical.spacing=14;}
 var resize=(RectTransform)Find(window,id+"Resize");resize.SetAsLastSibling();resize.sizeDelta=new Vector2(24,24);resize.GetComponent<Image>().color=new Color(.25f,.16f,.11f,.9f);ForgeResizeArrow.AddTo(resize);resize.GetComponent<ForgeUIHover>().description="드래그하여 창 크기 조절";resize.GetComponent<ForgeUIDrag>().minimum=new Vector2(id=="Developer"?380:340,id=="Sales"?280:240);
 }
 var sell=(RectTransform)Find(root.transform,"Sell");Fill(sell,22,22,22,0);sell.anchorMax=new Vector2(1,0);sell.sizeDelta=new Vector2(-44,44);
 var sales=(RectTransform)Find(root.transform,"Sales");sales.sizeDelta=new Vector2(Mathf.Max(400,sales.sizeDelta.x),Mathf.Max(360,sales.sizeDelta.y));
 var debug=(RectTransform)Find(root.transform,"Debug");debug.sizeDelta=new Vector2(Mathf.Max(400,debug.sizeDelta.x),Mathf.Max(300,debug.sizeDelta.y));
 }
 public static string Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
 var root=PrefabUtility.LoadPrefabContents("Assets/UI/ForgeGameUI.prefab");try{ApplyTo(root);PrefabUtility.SaveAsPrefabAsset(root,"Assets/UI/ForgeGameUI.prefab");}finally{PrefabUtility.UnloadPrefabContents(root);}
 var sale=PrefabUtility.LoadPrefabContents("Assets/UI/ForgeSaleItem.prefab");try{ButtonStyle(sale.GetComponent<Button>());PrefabUtility.SaveAsPrefabAsset(sale,"Assets/UI/ForgeSaleItem.prefab");}finally{PrefabUtility.UnloadPrefabContents(sale);}
 AssetDatabase.SaveAssets();return "Utility windows restyled; zoom tile removed.";
 }
}
