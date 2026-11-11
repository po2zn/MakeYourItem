using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Retained UI: no OnGUI, no runtime object construction, no per-frame card layout.
public sealed class ForgeTreePanel : MonoBehaviour
{
    [System.Serializable] public sealed class Edge {public Image[] lines;public string parentOffer;public int parentSmith=-1;}
    public GameObject[] pages;
    public GameObject materialStockBar;public RawImage[] materialStockIcons;public Text[] materialStockCounts;
    ForgeMaterialHud stockArtwork;readonly float[] stockPulseUntil=new float[9];
    public ForgeTreePan[] maps;
    public ForgeTreeCard[] cards;
    public Edge[] edges;
    public Button growthButton,materialsButton,smithButton,fitButton,resetButton,detailsButton,backButton,stopJobsButton;
    public Text heading,message,detailsText;
    public RectTransform tooltip;
    public Text tooltipTitle,tooltipBody;
    public ScrollRect tooltipScroll;
    public RectTransform tooltipContent;
    public Canvas canvas;
    ForgeMvp owner;ForgeTreeCard hovered;
    int page;bool bound;int lastStamp;float nextStatus;
    float noticeUntil,nextNoticeTick;
    Vector2 lastPointer=new Vector2(float.NaN,float.NaN);
    public int RefreshCount {get;private set;}
    static readonly Unity.Profiling.ProfilerMarker RefreshMarker=new Unity.Profiling.ProfilerMarker("ForgeTree.Refresh");
    static readonly Color Owned=new Color(.12f,.34f,.24f),Locked=new Color(.12f,.15f,.19f),Available=new Color(.16f,.23f,.32f);
    public void Bind(ForgeMvp game)
    {
        owner=game;if(bound)return;bound=true;
        foreach(var text in GetComponentsInChildren<Text>(true))ForgeGoldGlyph.Attach(text);
        if(materialStockIcons!=null){stockArtwork=new ForgeMaterialHud();for(int i=0;i<materialStockIcons.Length;i++){var icon=materialStockIcons[i];int id=i+1;icon.texture=stockArtwork.GetIcon(id);icon.uvRect=stockArtwork.GetIconUV(id);float aspect=stockArtwork.GetIconAspect(id);icon.rectTransform.sizeDelta=aspect>=1?new Vector2(27,27/aspect):new Vector2(27*aspect,27);}}
        foreach(var card in cards)card.Bind(this);
        growthButton.onClick.AddListener(()=>ShowPage(0));materialsButton.onClick.AddListener(()=>ShowPage(1));smithButton.onClick.AddListener(()=>ShowPage(2));
        fitButton.onClick.AddListener(()=>{if(page==3)ShowPage(2);maps[page].Fit();});resetButton.onClick.AddListener(()=>{if(page==3)ShowPage(2);maps[page].ResetView();});
        detailsButton.onClick.AddListener(()=>ShowPage(page==3?2:3));backButton.onClick.AddListener(()=>owner.CloseHierarchyTree());
        stopJobsButton.onClick.AddListener(()=>{owner.StopHierarchySmithJobs();Refresh();});
    }
    public void Open(){gameObject.SetActive(true);ShowPage(0);}
    public void ShowPage(int index)
    {
        page=index;if(materialStockBar)materialStockBar.SetActive(index==1);HideTooltip();for(int i=0;i<pages.Length;i++)pages[i].SetActive(i==index);
        // Header controls stay identical on every page. Smith details has a button inside its own page.
        owner.HierarchySmithPage=index>=2;Refresh();
    }
    void Update()
    {
        if(owner==null)return;
        AnimateStockPurchase();
        if(Time.unscaledTime<noticeUntil&&Time.unscaledTime>=nextNoticeTick){nextNoticeTick=Time.unscaledTime+.1f;message.color=Color.Lerp(Color.white,new Color(1,.8f,.3f),.5f+.5f*Mathf.Sin(Time.unscaledTime*6));}
        else if(Time.unscaledTime>=noticeUntil&&message.color!=Color.white)message.color=Color.white;
#if ENABLE_INPUT_SYSTEM
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){owner.CloseHierarchyTree();return;}
        Vector2 pointer=Mouse.current==null?lastPointer:Mouse.current.position.ReadValue();
#else
        Vector2 pointer=Input.mousePosition;
        if(Input.GetKeyDown(KeyCode.Escape)){owner.CloseHierarchyTree();return;}
#endif
        // Compare cheap scalar state at 5 Hz; text changes only when gameplay state changed.
        if(Time.unscaledTime>=nextStatus){nextStatus=Time.unscaledTime+.2f;int stamp=owner.HierarchyTreeStamp;if(stamp!=lastStamp||page==3)Refresh();}
        if(hovered!=null&&pointer!=lastPointer){PositionTooltip(pointer);lastPointer=pointer;}
    }
    public void Refresh()
    {
        using(RefreshMarker.Auto()){RefreshCount++;
        lastStamp=owner.HierarchyTreeStamp;
        if(page==1&&materialStockCounts!=null)for(int i=0;i<materialStockCounts.Length;i++)materialStockCounts[i].text=ForgeMaterialHud.FormatCount(owner.HierarchyMaterialCount(i+1));
        heading.text=page>=2?"대장장이 테크 트리 · "+ForgeMaterialHud.FormatCount(owner.HierarchyDiamonds)+"D":"테크 트리 · "+ForgeMaterialHud.FormatCount(owner.HierarchyGold)+"G";
        message.text=owner.HierarchyMessage(page>=2)+(owner.HierarchyGuideHasNew?" · 도면 안내서에 새 내용 추가":"");
        foreach(var card in cards){
            bool owned,locked;string state,price;
            if(card.smithId>=0){owned=owner.HierarchySmithOwned(card.smithId);locked=card.smithId>1&&card.smithId<=5&&!owner.HierarchySmithOwned(card.smithId-1);int cost=owner.HierarchySmithPrice(card.smithId);
                state=owned?"구매 완료":locked?"선행 구매 필요":owner.HierarchyDiamonds<cost?"다이아 부족":"구매 가능";price=owned?"완료":ForgeMaterialHud.FormatCount(cost)+"D";}
            else if(card.offer!=null){var offer=card.offer;owned=owner.HierarchyTechnology.Owns(offer);locked=owner.HierarchyTechnology.MissingPrerequisites(offer)!=null;
                state=offer.Kind==ForgePurchaseKind.Material?"재고 "+owner.HierarchyStock(offer):owned?"구매 완료":locked?"선행 구매 필요":owner.HierarchyBusy?"제작 중":owner.HierarchyGold<offer.Price?"골드 부족":"구매 가능";
                price=owned?"완료":ForgeMaterialHud.FormatCount(offer.Price)+(offer.Kind==ForgePurchaseKind.Material?"G/개":"G");}
            else continue;
            card.surface.color=owned?Owned:locked?Locked:Available;card.state.text=state;card.price.text=price;
        }
        foreach(var edge in edges){bool owned=edge.parentSmith>=0?owner.HierarchySmithOwned(edge.parentSmith):string.IsNullOrEmpty(edge.parentOffer)||owner.HierarchyTechnology.Owns(edge.parentOffer);foreach(var line in edge.lines)line.color=owned?new Color(.25f,.65f,.45f):new Color(.28f,.34f,.42f);}
        if(page==3){detailsText.text=owner.HierarchySmithDetails();float h=detailsText.preferredHeight+32;detailsText.rectTransform.sizeDelta=new Vector2(detailsText.rectTransform.sizeDelta.x,h);}
        if(hovered!=null)RefreshHover(hovered);
        }
    }
    void AnimateStockPurchase(){if(materialStockIcons==null)return;for(int i=0;i<materialStockIcons.Length;i++){float remaining=stockPulseUntil[i]-Time.unscaledTime;float scale=1;if(page==1&&remaining>0){float t=1-remaining/.55f;scale=1+.4f*Mathf.Sin(Mathf.PI*t)*Mathf.Exp(-1.4f*t);}materialStockIcons[i].rectTransform.localScale=Vector3.one*scale;}}
    public void Purchase(ForgeTreeCard card){int material=card.offer!=null&&card.offer.Kind==ForgePurchaseKind.Material?card.offer.Material:0;long before=material>0?owner.HierarchyMaterialCount(material):0;if(card.smithId>=0)owner.HierarchyBuySmith(card.smithId);else if(card.offer!=null)owner.HierarchyBuy(card.offer,card.Quantity);if(material>0&&owner.HierarchyMaterialCount(material)>before)stockPulseUntil[material-1]=Time.unscaledTime+.55f;if(owner.HierarchyGuideHasNew)noticeUntil=Time.unscaledTime+3;Refresh();}
    public void Hover(ForgeTreeCard card,Vector2 position){if(page<3&&maps[page].Dragging)return;hovered=card;RefreshHover(card);PositionTooltip(position);}
    public void Leave(ForgeTreeCard card){if(hovered==card)HideTooltip();}
    public void HideTooltip(){hovered=null;if(tooltip!=null)tooltip.gameObject.SetActive(false);}
    public void RefreshHover(ForgeTreeCard card)
    {
        if(hovered!=card)return;
        tooltipTitle.text=card.title.text;tooltipBody.text=card.smithId>=0?owner.HierarchySmithTooltip(card.smithId):owner.HierarchyTooltip(card.offer,card.Quantity);
        tooltip.gameObject.SetActive(true);
        float scale=canvas.scaleFactor;float width=Mathf.Min(520,Screen.width/scale-24);tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
        tooltipTitle.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-32);
        float titleHeight=tooltipTitle.preferredHeight+12;
        tooltipTitle.rectTransform.sizeDelta=new Vector2(width-32,titleHeight);
        tooltipBody.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-52);
        float bodyHeight=tooltipBody.preferredHeight+20;
        float maxHeight=Screen.height/scale-24;
        float height=Mathf.Min(maxHeight,titleHeight+bodyHeight+36);
        tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        var scrollRect=(RectTransform)tooltipScroll.transform;scrollRect.offsetMax=new Vector2(-12,-titleHeight-20);scrollRect.offsetMin=new Vector2(12,12);
        tooltipContent.sizeDelta=new Vector2(0,bodyHeight);tooltipBody.rectTransform.sizeDelta=new Vector2(-20,bodyHeight);
        tooltipScroll.verticalNormalizedPosition=1;
    }
    void PositionTooltip(Vector2 pointer)
    {
        if(!tooltip.gameObject.activeSelf)return;
        var parent=(RectTransform)tooltip.parent;RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,pointer,null,out var local);
        float x=local.x+18,y=local.y-18;float width=tooltip.rect.width,height=tooltip.rect.height;
        if(x+width>parent.rect.xMax-8)x=local.x-width-18;
        if(y-height<parent.rect.yMin+8)y=local.y+height+18;
        tooltip.anchoredPosition=new Vector2(Mathf.Clamp(x,parent.rect.xMin+8,parent.rect.xMax-width-8),Mathf.Clamp(y,parent.rect.yMin+height+8,parent.rect.yMax-8));
    }
    void OnDisable(){HideTooltip();}
    void OnDestroy(){stockArtwork?.Dispose();}
}
