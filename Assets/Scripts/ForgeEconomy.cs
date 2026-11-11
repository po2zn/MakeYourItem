using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum ForgeProductKind { IronBlade, Unfinished, Sword, StarBlade }
[Flags]
public enum ForgeTraits { None = 0, AirCooled = 1 }

// Immutable completion snapshot; prices never depend on the source text or extra heating.
public sealed class ForgeProduct
{
    readonly ForgeItemData data;
    public ForgeItemData Data => data.Clone();
    public double ProductionSeconds { get; }
    public int DiamondReward => Kind==ForgeProductKind.Sword && data.luckyDay>0 && !data.cracked && !data.destroyed ? 1+(data.star&&Material==7?1:0)+(ForgeCampaign.IsMasterwork(this)?1:0) : 0;
    public ForgeProductKind Kind { get; }
    public ForgeTraits Traits { get; }
    public int FinalHeat { get; }
    public bool Shaped { get; }
    public int CashCost { get; }
    public int IronConsumed { get; }
    public string BlueprintName { get; }
    public int Material { get; }
    public string DisplayName => Kind == ForgeProductKind.Sword ? "완성 검" : Kind == ForgeProductKind.StarBlade ? "별철 검날" : Kind == ForgeProductKind.IronBlade ? "철 검날" : ForgeMaterialGuide.Find(Material).Name + (Shaped ? " 미완성품" : " 원재료");
    public string StateText => (Shaped ? "성형 완료" : "미성형") + " · 열 " + FinalHeat + ((Traits & ForgeTraits.AirCooled) != 0 ? " · 공기 냉각" : "");

    public ForgeProduct(string blueprintName, int finalHeat, bool shaped, ForgeTraits traits, int cashCost, int ironConsumed, int material = 1, ForgeItemData itemData = null, double productionSeconds = 0)
    {
        if (ForgeMaterialGuide.Find(material) == null) throw new ArgumentException("등록되지 않은 재료입니다.");
        if (cashCost < 0 || ironConsumed < 0) throw new ArgumentOutOfRangeException(nameof(cashCost));
        data=(itemData??new ForgeItemData()).Clone();ProductionSeconds=productionSeconds;
        Material = material;
        Kind = !data.destroyed && !data.recovered && data.seals.Count==0 && (material==1||material==7) && shaped && finalHeat==0 ? data.handle>0?ForgeProductKind.Sword:material==7?ForgeProductKind.StarBlade:ForgeProductKind.IronBlade : ForgeProductKind.Unfinished;
        BlueprintName = blueprintName ?? "";
        FinalHeat = finalHeat; Shaped = shaped; Traits = traits;
        CashCost = cashCost; IronConsumed = ironConsumed;
    }
}

// Used only after a successful resource check; preview owns a separate instance and balances.
public sealed class ForgeProductionLedger
{
    public int CashCost { get; private set; }
    public int IronConsumed { get; private set; }

    public bool TrySpend(int cost, int iron, ref int gold, ref int stock)
    {
        if (cost < 0 || iron < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        if (gold < cost || stock < iron) return false;
        int nextCost = checked(CashCost + cost);
        int nextIron = checked(IronConsumed + iron);
        gold -= cost; stock -= iron;
        CashCost = nextCost; IronConsumed = nextIron;
        return true;
    }

    public ForgeProduct Complete(string owner, int heat, bool shaped, ForgeTraits traits, int material = 1, ForgeItemData data = null, double seconds = 0)
        => new ForgeProduct(owner, heat, shaped, traits, CashCost, IronConsumed, material, data, seconds);
}

public sealed class ForgeQuote
{
    public int BasePrice { get; }
    public int TraitBonus { get; }
    public int SalePrice => checked(BasePrice + TraitBonus);
    public int CashCost { get; }
    public int Profit => SalePrice - CashCost;
    internal ForgeQuote(int basePrice, int bonus, int cost)
    { BasePrice = basePrice; TraitBonus = bonus; CashCost = cost; }
}

public static class ForgePricing
{
    public static ForgeQuote Quote(ForgeProduct product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));

        if ((product.Traits & ~ForgeTraits.AirCooled) != 0) throw new ArgumentException("미지원 완성품 특성");
        // Future quenching/handle bonuses belong here, keyed by traits earned by successful processes.
        // Air cooling has no bonus. Never infer a bonus from cost, heat calls or blueprint names.
        int basePrice=product.Kind==ForgeProductKind.Unfinished?0:product.Material==7?40:28;
        int bonus=0;foreach(var line in Bonuses(product))bonus+=line.Value;
        return new ForgeQuote(basePrice,bonus,product.CashCost);
    }

    public static Dictionary<string,int> Bonuses(ForgeProduct product)
    {
        var result=new Dictionary<string,int>();if(product.Kind==ForgeProductKind.Unfinished)return result;var d=product.Data;
        if(d.hardness>=7)result["담금질 경도 7"]=19;
        if(d.handle>0)result["손잡이"]=d.handle==9?ForgeBalance.DragonHandleBonus:22;
        if(d.polished)result["연마"]=ForgeBalance.PolishBonus;
        if(d.plated)result["은도금"]=ForgeBalance.PlateBonus;
        if(d.star)result["별무늬"]=ForgeBalance.StarBonus;
        if(d.frost)result["서리 문양"]=ForgeBalance.FrostBonus;
        if(d.charge>=3)result["마력 전도"]=ForgeBalance.ChargeBonus;
        if(d.charge>=3&&d.handle==9)result["용의 맥동"]=ForgeBalance.DragonPulseBonus;
        if(d.activated)result["공명 수정"]=ForgeBalance.ResonanceBonus;
        if(d.sealBonus>0)result["결계"]=d.sealBonus*ForgeBalance.SealLayerBonus;
        if(d.magicGrade>0)result["마력 단조"]=d.magicGrade*ForgeBalance.MagicGradeBonus;
        foreach(int rune in d.runes)result["룬 "+rune]=((rune-1)/3+1)*ForgeBalance.RuneGradeBonus;
        if(d.runes.Count==3)result["삼중 공명"]=ForgeBalance.TripleRuneBonus;
        if(d.luckyDay>0)result["오늘의 행운"]=30;
        int value=product.Material==7?40:28;foreach(int b in result.Values)value+=b;
        if(d.cracked){result["균열 감점"]=-value/2;value-=value/2;}
        if(d.cursed){result["저주 배율"]=value*(ForgeBalance.CurseMultiplier-1);value*=ForgeBalance.CurseMultiplier;}
        if(d.smithSaleMultiplier>1)result["대장장이 판매 배율 ×"+d.smithSaleMultiplier.ToString("0.00")]=(int)Math.Round(value*(d.smithSaleMultiplier-1));
        return result;
    }
    public static string Describe(ForgeProduct product, bool preview)
    {
        var quote = Quote(product);
        string prefix = preview ? "예상 " : "";
        int materialValue=0;string materials="";var used=product.Data.materials;
        foreach(var offer in ForgeTechnology.Offers)if(offer.Kind==ForgePurchaseKind.Material&&used[offer.Material]>0){materialValue+=offer.Price*used[offer.Material];materials+=(materials.Length>0?", ":"")+ForgeMaterialGuide.Find(offer.Material).Name+" "+used[offer.Material];}
        string details=product.Data.handle>0?"\n손잡이 재료 · "+ForgeMaterialGuide.Find(product.Data.handle).Name:"";foreach(var bonus in Bonuses(product))details+="\n"+bonus.Key+" "+(bonus.Value>=0?"+":"")+bonus.Value+"G";
        return product.DisplayName + " · " + product.StateText+details
            + "\n기본가 " + quote.BasePrice + "G · 특성 보너스 +" + quote.TraitBonus + "G"
            + "\n" + prefix + "판매가 " + quote.SalePrice + "G"
            + "\n" + prefix + "제작비 " + quote.CashCost + "G · " + prefix + "공정비 차감 판매액 " + quote.Profit + "G"
            + "\n소비 재료: "+(materials.Length==0?"없음":materials)+" · 재구매 기준 "+materialValue+"G · 재료가 차감 예상 이익 "+(quote.Profit-materialValue)+"G"
            + "\n제작 시간 " + product.ProductionSeconds.ToString("0.0") + "초 · 시간당 판매액 " + (product.ProductionSeconds>0?(quote.SalePrice*3600/product.ProductionSeconds).ToString("0"):"—") + "G"
            + "\n행운 부산물 · 다이아몬드 " + product.DiamondReward + "개"
            + "\n" + (preview ? "소비 예정 재료" : "소비 재료") + " · 철 " + product.IronConsumed + "개";
    }
}

public sealed class ForgeShop
{
    public sealed class Entry
    {
        public int Id { get; }
        public ForgeProduct Product { get; }
        public long Order { get; }
        internal Entry(int id, long order, ForgeProduct product) { Id = id; Order = order; Product = product; }
    }
    readonly List<Entry> entries = new List<Entry>();
    readonly ReadOnlyCollection<Entry> view;
    int nextId = 1;
    long nextOrder = 1;
    readonly Dictionary<int,Entry> byId=new Dictionary<int,Entry>();
    public long NextOrder=>nextOrder;
    public int Revision {get;private set;}
    public void RestoreNextOrder(long value){nextOrder=Math.Max(nextOrder,value);if(nextOrder<1||nextOrder==long.MaxValue)throw new ArgumentOutOfRangeException(nameof(value));}
    public ForgeShop() { view = entries.AsReadOnly(); }
    public IReadOnlyList<Entry> Items => view;
    public int Count => entries.Count;
    public void Clear(){entries.Clear();byId.Clear();nextId=1;nextOrder=1;Revision++;}
    public int Add(ForgeProduct product,long order=0)
    {
        ForgePricing.Quote(product); // Reject incomplete/unpriceable records before listing.
        if(order==0)order=nextOrder;
        if(order<nextOrder||order==long.MaxValue)throw new ArgumentOutOfRangeException(nameof(order));
        if(nextId==int.MaxValue)throw new InvalidOperationException("완성품 번호 한도에 도달했습니다.");
        int id = nextId++;nextOrder=order+1;
        var entry=new Entry(id,order,product);entries.Add(entry);byId.Add(id,entry);Revision++;
        return id;
    }
    public Entry Find(int id) => byId.TryGetValue(id,out var entry)?entry:null;

    public bool TrySell(int id, ref int gold, out ForgeProduct sold)
    {
        sold = null;
        int index = entries.FindIndex(entry => entry.Id == id);
        if (index < 0) return false;
        var product = entries[index].Product;
        int newGold = checked(gold + ForgePricing.Quote(product).SalePrice);
        entries.RemoveAt(index);byId.Remove(id);Revision++; // This ID cannot be paid again, including repeated UI events.
        gold = newGold;
        sold = product;
        return true;
    }
}
