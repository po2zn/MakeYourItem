using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class ForgeHudRevision
{
    static RectTransform Part(Transform parent,string name,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
    public static void ApplyTo(GameObject root){
        var layout=root.GetComponent<ForgeLegacyLayout>();
        var shop=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="ShopToggle");
        layout.actions=layout.actions.Where(r=>r&&r.name!="ShopToggle").ToArray();if(shop)UnityEngine.Object.DestroyImmediate(shop.gameObject);
        var tile=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="LegacyProducts");
        var old=tile.Find("ProductSwordIcon");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var icon=new GameObject("ProductSwordIcon",typeof(RectTransform));icon.layer=5;var rect=(RectTransform)icon.transform;rect.SetParent(tile,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-7);rect.sizeDelta=new Vector2(44,44);
        var steel=new Color(.82f,.9f,1);Part(rect,"Blade",0,8,10,23,steel);Part(rect,"Edge",3,8,3,23,Color.white);
        var tip=Part(rect,"Tip",0,4,7,7,steel);tip.localRotation=Quaternion.Euler(0,0,45);
        Part(rect,"Guard",0,29,28,5,new Color(.93f,.75f,.35f));Part(rect,"Grip",0,34,6,9,new Color(.55f,.32f,.16f));Part(rect,"Pommel",0,42,10,4,new Color(.93f,.75f,.35f));
        var label=tile.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Products");label.rectTransform.anchorMin=new Vector2(0,0);label.rectTransform.anchorMax=new Vector2(1,1);label.rectTransform.offsetMin=new Vector2(4,4);label.rectTransform.offsetMax=new Vector2(-4,-56);label.fontSize=20;label.resizeTextForBestFit=true;label.resizeTextMinSize=14;label.resizeTextMaxSize=20;label.alignment=TextAnchor.MiddleCenter;
        var hover=tile.GetComponent<ForgeUIHover>()??tile.gameObject.AddComponent<ForgeUIHover>();hover.description="완성품 목록 열기";
    }
    public static void Batch(){try{const string path="Assets/UI/ForgeGameUI.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{ApplyTo(root);var layout=root.GetComponent<ForgeLegacyLayout>();if(layout.actions.Length!=3)throw new Exception("HUD action count");var text=root.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Products");foreach(var count in new[]{"0","999","999.9K","2.1B"}){text.text="완성품\n"+count;if(text.preferredHeight>text.rectTransform.rect.height+1)throw new Exception("Product text overflow");}text.text="완성품\n0";PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}ForgeTextLayoutChecks.Run();Debug.Log("PASS HUD: three toolbar actions, sword icon, product labels fit");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
