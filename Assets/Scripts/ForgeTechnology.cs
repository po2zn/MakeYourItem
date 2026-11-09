using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum ForgePurchaseKind { Material, Function }

// Session progression. Prices and offers live here, independently of the screen.
public sealed class ForgeTechnology
{
    public sealed class Offer
    {
        public readonly ForgePurchaseKind Kind;
        public readonly int Material, Price;
        public readonly BlueprintFeature Feature;
        public string Title => Kind==ForgePurchaseKind.Material ? ForgeMaterialGuide.Find(Material).Heading : BlueprintAliases.Find(Feature).Symbol+"() · "+BlueprintAliases.Find(Feature).Description;
        public Offer(int material,int price) {Kind=ForgePurchaseKind.Material;Material=material;Price=price;}
        public Offer(BlueprintFeature feature,int price) {Kind=ForgePurchaseKind.Function;Feature=feature;Price=price;}
    }
    public static readonly ReadOnlyCollection<Offer> Offers=Array.AsReadOnly(new[]{
        new Offer(1,5),new Offer(2,3),new Offer(3,1),new Offer(4,8),new Offer(5,20),
        new Offer(6,30),new Offer(7,40),new Offer(8,25),new Offer(9,50),
        new Offer(BlueprintFeature.Heat,20),new Offer(BlueprintFeature.Abs,15),
        new Offer(BlueprintFeature.Len,15),new Offer(BlueprintFeature.Min,20),
        new Offer(BlueprintFeature.Max,20),new Offer(BlueprintFeature.Random,30),new Offer(BlueprintFeature.Set,25),
        new Offer(BlueprintFeature.IsDay,20),new Offer(BlueprintFeature.NightTime,20)
    }.Concat(ForgeExpansion.Definitions.Where(d=>d.Price>0).Select(d=>new Offer(d.Alias.Id,d.Price))).ToArray());
    readonly HashSet<BlueprintFeature> purchased=new HashSet<BlueprintFeature>();
    public BlueprintFeature[] Export()=>purchased.ToArray();
    public void Restore(BlueprintFeature[] features){purchased.Clear();foreach(var f in features??Array.Empty<BlueprintFeature>())if(RequiresPurchase(f))purchased.Add(f);}
    public bool Owns(BlueprintFeature feature) => purchased.Contains(feature);
    public static bool RequiresPurchase(BlueprintFeature feature)
    {foreach(var offer in Offers)if(offer.Kind==ForgePurchaseKind.Function&&offer.Feature==feature)return true;return false;}
    public long Stock(Offer offer,int iron,ForgeMaterialStock[] stocks)
    {
        if(offer.Material==1)return iron;
        var stock=Array.Find(stocks??Array.Empty<ForgeMaterialStock>(),s=>s!=null&&s.material==offer.Material);
        return stock==null?0:stock.count;
    }
    public string BlockReason(Offer offer,int gold,int iron,ForgeMaterialStock[] stocks,bool busy)
    {
        if(offer==null||!Offers.Contains(offer))return "판매하지 않는 항목입니다.";
        if(busy)return "제작 중에는 구매할 수 없습니다.";
        if(offer.Kind==ForgePurchaseKind.Function&&Owns(offer.Feature))return "이미 구매한 기능입니다.";
        if(gold<offer.Price)return "골드가 부족합니다.";
        if(offer.Kind==ForgePurchaseKind.Material&&Stock(offer,iron,stocks)>=(offer.Material==1?int.MaxValue:long.MaxValue))return "재료 보유 한도에 도달했습니다.";
        return null;
    }
    public bool TryPurchase(Offer offer,ref int gold,ref int iron,ref ForgeMaterialStock[] stocks,bool busy,out string message)
    {
        message=BlockReason(offer,gold,iron,stocks,busy);if(message!=null)return false;
        if(offer.Kind==ForgePurchaseKind.Function)purchased.Add(offer.Feature);
        else if(offer.Material==1)iron++;
        else {
            var stock=Array.Find(stocks??Array.Empty<ForgeMaterialStock>(),s=>s!=null&&s.material==offer.Material);
            if(stock==null){stock=new ForgeMaterialStock{material=offer.Material};var list=new List<ForgeMaterialStock>(stocks??Array.Empty<ForgeMaterialStock>());list.Add(stock);stocks=list.ToArray();}
            stock.unlocked=true;stock.count++;
        }
        gold-=offer.Price;
        message=offer.Kind==ForgePurchaseKind.Material?ForgeMaterialGuide.Find(offer.Material).Name+" 1개 구매 완료":BlueprintAliases.Find(offer.Feature).Symbol+"() 기능 해금 완료";
        return true;
    }
}
