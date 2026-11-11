using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
public static class ForgeMaterialShopUI {
 static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);return r;}
 public static void ApplyTo(ForgeTreePanel panel){
 if(panel.materialStockBar)UnityEngine.Object.DestroyImmediate(panel.materialStockBar);
 var bar=Rect("MaterialStockBar",panel.transform);bar.anchorMin=new Vector2(0,1);bar.anchorMax=new Vector2(1,1);bar.pivot=new Vector2(.5f,1);bar.anchoredPosition=new Vector2(0,-150);bar.sizeDelta=new Vector2(0,64);var background=bar.gameObject.AddComponent<Image>();background.color=new Color(.11f,.15f,.21f);background.raycastTarget=true;
 var row=bar.gameObject.AddComponent<HorizontalLayoutGroup>();row.padding=new RectOffset(16,16,4,4);row.spacing=4;row.childControlWidth=row.childControlHeight=true;row.childForceExpandWidth=false;row.childForceExpandHeight=true;
 panel.materialStockBar=bar.gameObject;panel.materialStockIcons=new RawImage[9];panel.materialStockCounts=new Text[9];
 for(int i=0;i<9;i++){
 var tile=Rect("Stock"+(i+1),bar);tile.gameObject.AddComponent<LayoutElement>().preferredWidth=78;
 var icon=Rect("Icon",tile);icon.anchorMin=icon.anchorMax=new Vector2(.5f,1);icon.pivot=new Vector2(.5f,.5f);icon.anchoredPosition=new Vector2(0,-16);icon.sizeDelta=new Vector2(27,27);panel.materialStockIcons[i]=icon.gameObject.AddComponent<RawImage>();panel.materialStockIcons[i].raycastTarget=false;
 var r=Rect("Count",tile);r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-34);r.sizeDelta=new Vector2(0,22);var t=r.gameObject.AddComponent<Text>();t.font=panel.message.font;t.fontSize=14;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.resizeTextForBestFit=true;t.resizeTextMinSize=12;t.resizeTextMaxSize=14;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;t.text="0";panel.materialStockCounts[i]=t;
 }
 var page=(RectTransform)panel.pages[1].transform;page.offsetMax=new Vector2(page.offsetMax.x,-214);bar.gameObject.SetActive(false);
 }
 public static string Apply(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var root=PrefabUtility.LoadPrefabContents("Assets/UI/ForgeResearchUI.prefab");try{ApplyTo(root.GetComponent<ForgeTreePanel>());PrefabUtility.SaveAsPrefabAsset(root,"Assets/UI/ForgeResearchUI.prefab");}finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();return "Material stock header saved";}
}
