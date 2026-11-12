using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

// Guide purchase information comes from the same registries as the two trees.
public static class BlueprintResearchGuide
{
    public static bool FeatureVisible(BlueprintFeature feature,ForgeTechnology tech,ForgeBlacksmiths smiths)
    {
        if(tech==null)return false;
        if(ForgeTechnology.RequiresPurchase(feature))return tech.Owns(feature);
        switch(feature){
            case BlueprintFeature.SmithFavor:return smiths!=null&&smiths.favorUnlocked;
            case BlueprintFeature.SmithPosition:return smiths!=null&&smiths.positionUnlocked;
            case BlueprintFeature.SmithDemand:return smiths!=null&&smiths.demandUnlocked;
            case BlueprintFeature.GiantRune:case BlueprintFeature.RuneList:case BlueprintFeature.RuneFamily:case BlueprintFeature.RuneGrade:case BlueprintFeature.Slots:return tech.Owns(BlueprintFeature.Rune);
            case BlueprintFeature.Unwrap:case BlueprintFeature.SealCount:return tech.Owns(BlueprintFeature.Wrap);
            case BlueprintFeature.Strength:return tech.Owns(BlueprintFeature.Charge);
            default:return true;
        }
    }
    static readonly Dictionary<string,BlueprintFeature[]> requirements=new Dictionary<string,BlueprintFeature[]>();
    public static bool EntryVisible(BlueprintGuide.Entry entry,ForgeTechnology tech,ForgeBlacksmiths smiths)
    {
        if(!FeatureVisible(entry.Feature,tech,smiths))return false;
        if(!requirements.TryGetValue(entry.Id,out var features)){
            features=BlueprintLexer.Scan(entry.ExampleFor(true),true).Concat(BlueprintLexer.Scan(entry.ExampleFor(false),true)).Select(t=>t.Feature).Distinct().ToArray();
            requirements.Add(entry.Id,features);
        }
        if(features.Any(f=>!FeatureVisible(f,tech,smiths)))return false;
        if(entry.Id=="import"&&!tech.Owns(ForgeTechnology.ImportResearch))return false;
        if(entry.Feature==BlueprintFeature.SmithRun&&(smiths==null||smiths.hireTier<1))return false;
        if(entry.Id=="masterwork-example"&&!tech.Owns(ForgeTechnology.MasterCommission))return false;
        foreach(Match match in Regex.Matches(entry.ExampleFor(false),@"(?<![0-9])1\(\s*([4-9])\s*\)"))
            if(!tech.MaterialAvailable(int.Parse(match.Groups[1].Value)))return false;
        return true;
    }
    public static string UnlockedOverview(ForgeTechnology tech,bool english)
    {
        var text=new StringBuilder("구매한 연구가 이곳에 기록됩니다. 새 연구를 구매하면 관련 함수와 예제가 안내서에 추가됩니다.\n");
        foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material&&tech.Owns(o))){
            text.AppendLine();text.AppendLine(OfferInfo(offer,english));text.AppendLine(VisibleProse(Display(offer.Description,english),tech,null));
        }
        return text.ToString();
    }
    public static string VisibleProse(string text,ForgeTechnology tech,ForgeBlacksmiths smiths)
    {
        if(tech==null)return "";
        var parts=Regex.Split(text,@"(?<=\.)\s+|\r?\n");
        return string.Join("\n",parts.Where(part=>{
            foreach(var alias in BlueprintAliases.All){
                if(FeatureVisible(alias.Id,tech,smiths))continue;
                if(Regex.IsMatch(part,@"(?<![A-Za-z0-9_])"+Regex.Escape(alias.English)+@"(?![A-Za-z0-9_])"))return false;
                if(alias.Symbol.Length>0&&Regex.IsMatch(part,@"(?<![0-9])"+Regex.Escape(alias.Symbol)+@"\s*\("))return false;
            }
            for(int i=4;i<=9;i++)if(!tech.MaterialAvailable(i)&&part.Contains(ForgeMaterialGuide.Find(i).Name))return false;
            return true;
        }));
    }
    public static string SectionFor(BlueprintFeature feature,string fallback)
    {
        switch(feature){
            case BlueprintFeature.Heat:case BlueprintFeature.Polish:case BlueprintFeature.Plate:case BlueprintFeature.Refine:return "금속 가공";
            case BlueprintFeature.Lucky:case BlueprintFeature.LuckyCode:return "행운";
            case BlueprintFeature.Inspect:case BlueprintFeature.Charge:case BlueprintFeature.Rune:case BlueprintFeature.Magic:case BlueprintFeature.Resonate:case BlueprintFeature.Protect:case BlueprintFeature.Curse:
            case BlueprintFeature.RuneList:case BlueprintFeature.RuneFamily:case BlueprintFeature.RuneGrade:case BlueprintFeature.Slots:case BlueprintFeature.Strength:return "마력 가공";
            case BlueprintFeature.Fuel:case BlueprintFeature.Recover:case BlueprintFeature.Wrap:case BlueprintFeature.Unwrap:case BlueprintFeature.SealCount:case BlueprintFeature.Speed:case BlueprintFeature.AutoSell:case BlueprintFeature.Wait:return "공방 운영";
            case BlueprintFeature.Abs:case BlueprintFeature.Len:case BlueprintFeature.Min:case BlueprintFeature.Max:case BlueprintFeature.Random:case BlueprintFeature.Set:return "기술 연구";
            case BlueprintFeature.Append:case BlueprintFeature.Pop:case BlueprintFeature.SetAdd:return "자료 묶음";
            default:return fallback;
        }
    }
    public static string Display(string text,bool english)
    {
        if(english)return text;
        foreach(var alias in BlueprintAliases.All.Where(a=>a.Symbol.Length>0).OrderByDescending(a=>a.English.Length))
            text=Regex.Replace(text,@"(?<![A-Za-z0-9_])"+Regex.Escape(alias.English)+@"(?![A-Za-z0-9_])",_=>alias.Symbol);
        return text;
    }
    public static string OfferInfo(ForgeTechnology.Offer offer,bool english)
    {
        string parents=offer.Parents.Count==0?"기본 공방 (제공)":string.Join(" + ",offer.Parents.Select(id=>Display(ForgeTechnology.Find(id).Title,english)));
        string result=Display(offer.Title,english)+" · "+offer.Price+"G\n선행 구매 필요: "+parents;
        if(offer.Features.Count>1)result+="\n함께 해금: "+string.Join(" / ",offer.Features.Select(f=>{var a=BlueprintAliases.Find(f);return a.Symbol+(english?" · "+a.English:"");}));
        return result;
    }
    static ForgeTechnology.Offer FeatureOffer(BlueprintFeature feature)=>feature==BlueprintFeature.None?null:ForgeTechnology.Find("function-"+feature);
    public static string EntryResearch(BlueprintGuide.Entry entry,bool english)
    {
        var own=FeatureOffer(entry.Feature);var lines=new List<string>();
        if(own!=null){
            lines.Add(OfferInfo(own,english));
            if(entry.Feature==BlueprintFeature.Speed)lines.Add("설정 가능한 최대 배율은 구매한 단계까지입니다. 예제의 222(2)는 스피드 2단계 구매 후 사용하세요.");
            if(entry.Feature==BlueprintFeature.AutoSell)lines.Add("스피드 5단계는 선행 조건입니다. 자동판매 노드를 별도로 구매하면 이 안내서와 225()가 해금되고 자동판매가 켜집니다.");
        }else if(entry.Feature==BlueprintFeature.SmithFavor)lines.Add("대장장이 트리 · 호감도 조회 · "+ForgeBlacksmiths.FavorResearchCost+" 다이아 · 선행: 기본 대장장이");
        else if(entry.Feature==BlueprintFeature.SmithPosition)lines.Add("대장장이 트리 · 위치 조회 · "+ForgeBlacksmiths.PositionResearchCost+" 다이아 · 선행: 기본 대장장이");
        else if(entry.Feature==BlueprintFeature.SmithDemand)lines.Add("대장장이 트리 · 요구 수량 조회 · "+ForgeBlacksmiths.DemandResearchCost+" 다이아 · 선행: 기본 대장장이");
        else if(entry.Feature==BlueprintFeature.GiantRune)lines.Add("거대한 룬은 성장 트리의 룬 각인 연구가 필요합니다. 설치할 때마다 수정 1개와 등급별 25/50/75 다이아를 소비합니다.");
        else if(entry.Feature!=BlueprintFeature.None||new[]{"1","2","3","4","finish"}.Contains(entry.Id))lines.Add("기본 제공 · 이 명령 자체의 연구 구매 비용은 없습니다.");
        var needed=new List<ForgeTechnology.Offer>();
        foreach(var token in BlueprintLexer.Scan(entry.ExampleFor(english),true)){
            var offer=FeatureOffer(token.Feature);
            if(offer!=null&&offer!=own&&!needed.Contains(offer))needed.Add(offer);
            if(token.Feature==BlueprintFeature.GiantRune){offer=FeatureOffer(BlueprintFeature.Rune);if(!needed.Contains(offer))needed.Add(offer);}
        }
        if(entry.Id=="import"&&!needed.Any(o=>o.Id==ForgeTechnology.ImportResearch))needed.Add(ForgeTechnology.Find(ForgeTechnology.ImportResearch));
        if(needed.Count>0)lines.Add("예제에 필요한 추가 연구 (각 선행 조건 포함):\n"+string.Join("\n",needed.Select(o=>"• "+OfferInfo(o,english))));
        if(entry.Feature==BlueprintFeature.SmithRun)lines.Add("예제는 대장장이 1번을 사용합니다. 2명 고용("+ForgeBlacksmiths.HireCosts[0]+" 다이아)과 제어문 해금이 필요합니다. 번호는 0부터 시작합니다.");
        if(entry.Id=="masterwork-example")lines.Add(OfferInfo(ForgeTechnology.Find(ForgeTechnology.MasterCommission),english));
        if(lines.Count==0)lines.Add("별도 연구가 없는 기본 문법 예제입니다.");
        lines.Add("명령 해금과 재료 보유는 별개입니다. 예제에 쓰는 재료의 공급을 해금하고 실제 재고를 준비하세요.");
        return Display(string.Join("\n\n",lines),english);
    }
    public static string MaterialInfo(int material,bool english)
    {
        if(material==0)return "공기 · 기본 제공 · 재고와 구매 비용 없음";
        var stock=ForgeTechnology.Find("stock-"+material);
        string text="재료 상점: 개당 "+stock.Price+"G · 수량을 입력하고 카드를 클릭해 구매";
        if(material<=3)return "기본 공급 재료 · 별도 공급 해금 없음\n"+text;
        var permit=ForgeTechnology.Find("material-"+material);
        string permitInfo=material==4?Display(permit.Title,english)+" · "+permit.Price+"G":OfferInfo(permit,english);
        return permitInfo+"\n해금비에는 재료가 포함되지 않습니다.\n"+text;
    }
    public static string Overview(bool english)
    {
        var text=new StringBuilder();
        text.AppendLine("1. 기본 공방에서 시작");
        text.AppendLine("1~5 공정과 ! 제출, 변수·계산·print는 기본 제공됩니다. 철·나무·물은 공급 연구 없이 재료 상점에서 구매합니다. 처음에는 기본 공정으로 제작하고 상점에서 판매해 연구 자금을 모으세요. 공정의 골드 비용 0G와 재료 구매비는 서로 다릅니다.\n");
        text.AppendLine("2. 제어문 해금으로 자동화 시작");
        text.AppendLine(OfferInfo(ForgeTechnology.Find(ForgeTechnology.AutomationRoot),english));
        text.AppendLine("조건·반복·반복 제어·사용자 함수를 한 번에 해금합니다. 기본 계산과 컬렉션 자체는 제공되지만 연구 함수와 제어문을 넣은 예제에는 해당 연구가 필요합니다.\n");
        text.AppendLine("3. 성장 트리 구매 방법");
        text.AppendLine("카드에 마우스를 올리면 설명과 선행 조건이 보입니다. 카드를 클릭하면 구매합니다. 연결된 선행 항목은 모두 구매해야 합니다. 구매 완료 카드는 초록색입니다. 제작 중에도 연구와 재료를 구매할 수 있습니다. 빈 곳/우클릭 드래그로 이동하고 휠로 확대·축소합니다.\n");
        foreach(string section in new[]{"금속 가공","행운","마력 가공","공방 운영","시간 조회","기술 연구"}){
            text.AppendLine("【"+section+"】");
            foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material&&o.Id!=ForgeTechnology.AutomationRoot&&o.Id!=ForgeTechnology.MasterCommission).Where(o=>OfferSection(o)==section).OrderBy(o=>o.Tier).ThenBy(o=>o.Lane)){
                text.AppendLine(OfferInfo(offer,english));text.AppendLine(Display(offer.Description,english));text.AppendLine();
            }
        }
        text.AppendLine("【재료 상점】");foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind==ForgePurchaseKind.Material))text.AppendLine(ForgeMaterialGuide.Find(offer.Material).Heading+" · "+offer.Price+"G/개");
        text.AppendLine("공급 해금비와 재료 구매비는 별개입니다. 재료 카드에서 수량을 입력한 후 클릭하세요.\n");
        text.AppendLine("【명장의 마지막 의뢰】");text.AppendLine(OfferInfo(ForgeTechnology.Find(ForgeTechnology.MasterCommission),english));text.AppendLine(ForgeTechnology.Find(ForgeTechnology.MasterCommission).Description);
        text.AppendLine("\n【대장장이 트리 · 다이아】");
        for(int i=0;i<ForgeBlacksmiths.HireCosts.Length;i++)text.AppendLine(ForgeBlacksmiths.Counts[i]+" → "+ForgeBlacksmiths.Counts[i+1]+"명 · "+ForgeBlacksmiths.HireCosts[i]+" 다이아 · 이전 고용 단계 필요");
        text.AppendLine("호감도 조회 90902: "+ForgeBlacksmiths.FavorResearchCost+" 다이아 / 위치 조회 90903: "+ForgeBlacksmiths.PositionResearchCost+" 다이아 / 요구 수량 조회 90909: "+ForgeBlacksmiths.DemandResearchCost+" 다이아. 세 연구는 기본 대장장이에서 각각 분기합니다.");
        text.AppendLine("선물 90901, 병렬 명령 90904, 인원 90905, 자기 번호 90906, 작업 여부 90907은 기본 제공됩니다. 병렬 명령에 전달하는 사용자 함수는 성장 트리의 제어문 해금이 필요합니다. 자세한 사용법은 대장장이 책갈피에서 확인하세요.");
        return Display(text.ToString(),english);
    }
    static string OfferSection(ForgeTechnology.Offer offer)
    {
        if(offer.Kind==ForgePurchaseKind.MaterialUnlock)return offer.Material==4?"공방 운영":offer.Material==6?"마력 가공":"금속 가공";
        return SectionFor(offer.Feature,offer.Feature==BlueprintFeature.IsDay?"시간 조회":"기술 연구");
    }
}
