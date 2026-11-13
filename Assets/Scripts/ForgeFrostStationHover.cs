using UnityEngine;
using UnityEngine.UI;

// Read-only preview; never selects a code variable, changes the open guide, or starts practice.
public sealed class ForgeFrostStationHover : MonoBehaviour
{
    RectTransform panel,graphRect;Text heading,summary,empty;ForgeCoolingDiagram graph;
    readonly BlueprintObservation observation=new BlueprintObservation();
    ForgeCraftMotion owner;int station;float nextRefresh;
    public static ForgeFrostStationHover Create(ForgeGameUI ui){
        var root=new GameObject("서리수 작업장 미리보기",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));root.transform.SetParent(ui.transform,false);
        var result=root.AddComponent<ForgeFrostStationHover>();result.panel=(RectTransform)root.transform;result.panel.anchorMin=result.panel.anchorMax=new Vector2(0,1);result.panel.pivot=new Vector2(0,1);
        var paper=new Color(.96f,.92f,.82f);var ink=new Color(.18f,.15f,.12f);var border=new Color(.25f,.16f,.11f);
        root.GetComponent<Image>().color=paper;root.GetComponent<Image>().raycastTarget=false;var outline=root.AddComponent<Outline>();outline.effectColor=border;outline.effectDistance=new Vector2(3,-3);
        var font=ui.Node("DebugText").GetComponent<Text>().font;
        Text Label(string name,int size){var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));g.transform.SetParent(root.transform,false);var t=g.GetComponent<Text>();t.font=font;t.fontSize=size;t.color=ink;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.rectTransform.anchorMin=t.rectTransform.anchorMax=t.rectTransform.pivot=new Vector2(0,1);return t;}
        result.heading=Label("작업장 제목",20);result.summary=Label("작업장 상태",16);result.empty=Label("빈 작업장 안내",18);
        var diagram=new GameObject("작업장 냉각 경로",typeof(RectTransform),typeof(CanvasRenderer),typeof(ForgeCoolingDiagram));diagram.transform.SetParent(root.transform,false);result.graph=diagram.GetComponent<ForgeCoolingDiagram>();result.graph.Compact=true;result.graph.raycastTarget=false;result.graph.ApplyStyle(ink,paper,border);result.graphRect=(RectTransform)diagram.transform;result.graphRect.anchorMin=result.graphRect.anchorMax=result.graphRect.pivot=new Vector2(0,1);root.SetActive(false);return result;
    }
    public void Hide(){gameObject.SetActive(false);owner=null;}
    public void Show(ForgeGameUI ui,ForgeCraftMotion motion,int id,Vector2 pointer,long water){
        motion.NoticeFrostStation();
        bool changed=owner!=motion||!gameObject.activeSelf;owner=motion;station=id;gameObject.SetActive(true);transform.SetAsLastSibling();
        var bounds=((RectTransform)ui.transform).rect;float width=Mathf.Min(550,bounds.width-24),height=Mathf.Min(560,bounds.height-24);panel.sizeDelta=new Vector2(width,height);
        Camera camera=ui.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:ui.canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ui.transform,pointer,camera,out var local);float x=local.x-bounds.xMin+18,y=bounds.yMax-local.y+18;
        if(x+width>bounds.width-12)x=local.x-bounds.xMin-width-18;
        panel.anchoredPosition=new Vector2(Mathf.Clamp(x,12,bounds.width-width-12),-Mathf.Clamp(y,12,bounds.height-height-12));
        if(!changed&&Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.08f;
        var view=owner.StationCoolingView;var blade=view.Blade;
        heading.text="서리수 작업장 · 대장장이 #"+station;
        summary.text="보유 서리수: "+ForgeMaterialHud.FormatCount(water)+"개 · 통로 숫자 = 소모량"+(blade==null?"":"\n칼날 #"+blade.id+" · "+blade.heat+"도"+(view.Path==null?"":" · 냉각 "+Mathf.RoundToInt(view.Progress*100)+"%"));
        float TextHeight(Text text,float top){text.rectTransform.sizeDelta=new Vector2(width-24,100);float h=text.preferredHeight+6;text.rectTransform.sizeDelta=new Vector2(width-24,h);text.rectTransform.anchoredPosition=new Vector2(12,-top);return h;}
        float top=12+TextHeight(heading,12);top+=TextHeight(summary,top)+6;
        bool ready=blade?.data.cooling!=null;graph.gameObject.SetActive(ready);empty.gameObject.SetActive(!ready);
        empty.text="작업 중인 칼날이 없습니다.\n이 대장장이에게 칼날 제작을 맡기면\n냉각 경로가 여기에 표시됩니다.";TextHeight(empty,top+24);
        graphRect.anchoredPosition=new Vector2(12,-top);graphRect.sizeDelta=new Vector2(width-24,Mathf.Max(200,height-top-12));
        if(ready)graph.Show(view,observation,null,null,null,heading.font);
    }
}
