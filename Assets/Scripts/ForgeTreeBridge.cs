using System.Text;
using UnityEngine;

public partial class ForgeMvp
{
    [SerializeField] ForgeTreePanel hierarchyTree;
    public ForgeTreePanel HierarchyTree=>hierarchyTree;
    public void SetHierarchyTree(ForgeTreePanel panel){hierarchyTree=panel;}
    void CancelHierarchyTreeDrag(){if(hierarchyTree==null)return;foreach(var map in hierarchyTree.maps)map.CancelDrag();hierarchyTree.HideTooltip();}
    float nextHierarchyGuideSync;
    void SyncHierarchyTree()
    {
        if(hierarchyTree==null)return;
        hierarchyTree.Bind(this);
        bool visible=showTechTree&&!showTitle&&!showGameMenu&&!showTutorial&&!showProgress;
        if(visible&&!hierarchyTree.gameObject.activeSelf)hierarchyTree.Open();
        else if(!visible&&hierarchyTree.gameObject.activeSelf)hierarchyTree.gameObject.SetActive(false);
        if(visible&&Time.unscaledTime>=nextHierarchyGuideSync){nextHierarchyGuideSync=Time.unscaledTime+.5f;guideBook.SyncResearch(technology,blacksmiths);}
    }
    public void CloseHierarchyTree(){showTechTree=false;showSmithTree=false;hierarchyTree.gameObject.SetActive(false);pointerResetPending=true;}
    public bool HierarchySmithPage {set=>showSmithTree=value;}
    public ForgeTechnology HierarchyTechnology=>technology;
    public int HierarchyGold=>gold;
    public long HierarchyMaterialCount(int id){if(id==1)return iron;foreach(var stock in materialStocks)if(stock!=null&&stock.material==id)return stock.count;return 0;}
    public bool HierarchyGuideHasNew=>guideBook.HasNew;
    public int HierarchyDiamonds=>diamonds;
    public bool HierarchyBusy=>busy||executionSession!=null;
    public long HierarchyStock(ForgeTechnology.Offer offer)=>technology.Stock(offer,iron,materialStocks);
    public int HierarchyTreeStamp {get{unchecked{int hash=gold*397^iron^diamonds*17^(HierarchyBusy?1:0);for(int i=0;i<ForgeTechnology.Offers.Count;i++){var offer=ForgeTechnology.Offers[i];if(technology.Owns(offer))hash=hash*31+i;}foreach(var stock in materialStocks)if(stock!=null)hash=hash*31+stock.count.GetHashCode();hash=hash*31+blacksmiths.hireTier;return hash^(blacksmiths.favorUnlocked?1024:0)^(blacksmiths.positionUnlocked?2048:0)^(blacksmiths.demandUnlocked?4096:0);}}}
    public string HierarchyMessage(bool smith)=>smith?(string.IsNullOrEmpty(smithMessage)?"항목에 마우스: 설명 · 클릭: 구매 · 빈 곳/우클릭 드래그: 이동 · 휠: 확대/축소":smithMessage):(string.IsNullOrEmpty(purchaseMessage)?"항목에 마우스: 설명 · 클릭: 구매 · 빈 곳/우클릭 드래그: 이동 · 휠: 확대/축소":purchaseMessage);
    public void HierarchyBuy(ForgeTechnology.Offer offer,int quantity){PurchaseTechnology(offer,quantity);guideBook.SyncResearch(technology,blacksmiths);}
    public void HierarchyBuySmith(int id){PurchaseSmithNode(id);guideBook.SyncResearch(technology,blacksmiths);}
    public bool HierarchySmithOwned(int id)=>SmithNodeOwned(id);
    public int HierarchySmithPrice(int id)=>SmithNodePrice(id);
    public static string HierarchySmithTitle(int id)=>id==0?"기본 대장장이 · 1명":id<=5?"대장장이 "+ForgeBlacksmiths.Counts[id]+"명 고용":id==6?"호감도 조회 · 90902()":id==7?"위치 조회 · 90903()":"요구 수량 조회 · 90909()";
    public string HierarchySmithTooltip(int id)=>SmithNodeDescription(id)+(id==0?"":"\n\n선행 구매 필요: "+SmithNodeTitle(id<=5?id-1:0)+"\n"+(SmithNodeOwned(id)?"구매 완료":ForgeMaterialHud.FormatCount(SmithNodePrice(id))+"D · 클릭하여 구매"));
    public string HierarchyTooltip(ForgeTechnology.Offer offer,int quantity)
    {
        if(offer==null)return "기본 제공";
        string reason=technology.BlockReason(offer,gold,iron,materialStocks,HierarchyBusy,quantity);
        string text=ForgeMaterialHud.FormatCount(offer.Price)+"G\n\n"+offer.Description+"\n\n"+technology.PrerequisiteText(offer);
        if(offer.Kind==ForgePurchaseKind.Material)text+="\n재고 "+HierarchyStock(offer)+" · 수량 "+quantity+" · 합계 "+ForgeMaterialHud.FormatCount(ForgeTechnology.TotalPrice(offer,quantity))+"G";
        if(reason!=technology.MissingPrerequisites(offer))text+="\n\n"+(reason??"클릭하여 구매");
        return text;
    }
    public void StopHierarchySmithJobs(){StopSmithJobs();smithMessage="이미 소비한 재료는 유지됩니다.";}
    public string HierarchySmithDetails()
    {
        var text=new StringBuilder("선물: 일치 +15 / 불일치 -10 · 요구 수량 1~9\n호감도: 게임 2시간마다 -1\n불타는 대장장이: 대성공 50%, 속도 변화 없음\n\n거대한 룬: 90910(룬 번호, x, y)\n룬 각인 연구 후 수정 1개 + 다이아 25/50/75개\n자신과 상하좌우 한 칸에 적용\n\n");
        foreach(var rune in blacksmiths.runes)text.AppendLine("거대한 룬 "+rune.rune+" · ("+rune.x+", "+rune.y+")");
        for(int i=0;i<blacksmiths.smiths.Count;i++){blacksmiths.Chances(i,SmithHours,out double destruction,out double great);text.AppendLine(SmithInfo(i));text.AppendLine("파괴 "+(destruction*100).ToString("0.0")+"% · 대성공 "+(great*100).ToString("0.0")+"%\n");}
        return text.ToString();
    }
}
