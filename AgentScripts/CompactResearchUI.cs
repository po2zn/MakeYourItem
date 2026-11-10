using System.Linq;
using UnityEngine;
using UnityEditor;
public static class CompactResearchUI
{
    public static void Apply()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/UI/ForgeResearchUI.prefab");
        try{
            var p=root.GetComponent<ForgeTreePanel>();var header=(RectTransform)p.heading.transform.parent;header.sizeDelta=new Vector2(0,150);
            p.heading.fontSize=28;p.heading.rectTransform.sizeDelta=new Vector2(-232,40);
            var back=(RectTransform)p.backButton.transform;back.sizeDelta=new Vector2(180,38);
            foreach(var button in new[]{p.growthButton,p.materialsButton,p.smithButton,p.fitButton,p.resetButton}){
                var r=(RectTransform)button.transform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-54);r.sizeDelta=new Vector2(r.sizeDelta.x,40);
                var label=button.GetComponentInChildren<UnityEngine.UI.Text>();label.rectTransform.sizeDelta=new Vector2(r.sizeDelta.x-16,32);
            }
            var backLabel=p.backButton.GetComponentInChildren<UnityEngine.UI.Text>();backLabel.rectTransform.sizeDelta=new Vector2(164,30);
            p.message.fontSize=18;p.message.rectTransform.anchoredPosition=new Vector2(16,-100);p.message.rectTransform.sizeDelta=new Vector2(-32,42);
            foreach(var page in p.pages){var r=(RectTransform)page.transform;r.offsetMax=new Vector2(r.offsetMax.x,-150);}
            var baseSmith=p.cards.First(c=>c.smithId==0);baseSmith.state.gameObject.SetActive(false);baseSmith.price.gameObject.SetActive(false);
            var detail=(RectTransform)p.detailsButton.transform;detail.SetParent(baseSmith.transform,false);detail.anchorMin=detail.anchorMax=new Vector2(0,1);detail.pivot=new Vector2(0,1);detail.anchoredPosition=new Vector2(18,-122);detail.sizeDelta=new Vector2(264,40);
            var detailLabel=p.detailsButton.GetComponentInChildren<UnityEngine.UI.Text>();detailLabel.text="대장장이 현황";detailLabel.rectTransform.sizeDelta=new Vector2(248,32);
            foreach(var map in p.maps)map.zoomStep=.08f;
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/UI/ForgeResearchUI.prefab");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
}
