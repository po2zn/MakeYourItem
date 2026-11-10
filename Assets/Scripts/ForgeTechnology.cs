using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum ForgePurchaseKind { Material, Function, MaterialUnlock, Commission }

// One graph controls purchases, the visible edges, and save migration.
public sealed class ForgeTechnology
{
    public const string MasterCommission="master-commission";
    public sealed class Offer
    {
        public readonly ForgePurchaseKind Kind;
        public readonly int Material,Price,Tier,Lane;
        public readonly BlueprintFeature Feature;
        public readonly string Id,Name,Description;
        public readonly ReadOnlyCollection<string> Parents;
        public string Title => Name;
        public Offer(int material,int price):this("stock-"+material,ForgePurchaseKind.Material,price,ForgeMaterialGuide.Find(material).Heading,"보유 재고에 추가됩니다.",0,0,material,BlueprintFeature.None,material>3?new[]{"material-"+material}:Array.Empty<string>()){}
        public Offer(BlueprintFeature feature,int price):this("function-"+feature,ForgePurchaseKind.Function,price,BlueprintAliases.Find(feature).Symbol+"() · "+BlueprintAliases.Find(feature).English,BlueprintAliases.Find(feature).Description,1,0,0,feature,Array.Empty<string>()){}
        internal Offer(string id,ForgePurchaseKind kind,int price,string name,string description,int tier,int lane,int material,BlueprintFeature feature,params string[] parents)
        {Id=id;Kind=kind;Price=price;Name=name;Description=description;Tier=tier;Lane=lane;Material=material;Feature=feature;Parents=Array.AsReadOnly(parents);}
    }
    static Offer F(BlueprintFeature f,int price,int tier,int lane,params string[] parents)
    {var a=BlueprintAliases.Find(f);return new Offer("function-"+f,ForgePurchaseKind.Function,price,a.Symbol+"() · "+a.English,a.Description,tier,lane,0,f,parents);}
    static Offer M(int material,int price,int tier,int lane,params string[] parents)
    {return new Offer("material-"+material,ForgePurchaseKind.MaterialUnlock,price,ForgeMaterialGuide.Find(material).Name+" 공급 해금","한 번 해금하면 재료 상점에서 필요한 수량을 계속 구매할 수 있습니다. 해금비에 재료는 포함되지 않습니다.",tier,lane,material,BlueprintFeature.None,parents);}
    public static readonly ReadOnlyCollection<Offer> Offers=Array.AsReadOnly(new[]{
        new Offer(1,5),new Offer(2,3),new Offer(3,1),new Offer(4,8),new Offer(5,20),
        new Offer(6,30),new Offer(7,40),new Offer(8,25),new Offer(9,50),
        F(BlueprintFeature.Heat,20,1,0),
        F(BlueprintFeature.Abs,15,2,1,"function-Heat"),
        F(BlueprintFeature.Len,15,2,5,"function-Heat"),
        F(BlueprintFeature.Min,20,3,1,"function-Abs"),
        F(BlueprintFeature.Max,20,4,1,"function-Min"),
        F(BlueprintFeature.Random,30,4,5,"function-Set"),
        F(BlueprintFeature.Set,25,3,5,"function-Len"),
        F(BlueprintFeature.IsDay,20,1,8),
        F(BlueprintFeature.NightTime,20,2,8,"function-IsDay"),
        F(BlueprintFeature.Polish,25,2,0,"function-Heat"),
        M(5,120,3,0,"function-Polish"),
        F(BlueprintFeature.Plate,30,4,0,"material-5"),
        M(7,300,5,0,"function-Plate"),
        M(8,180,6,0,"material-7"),
        M(9,500,6,1,"material-7","function-Charge"),
        F(BlueprintFeature.Refine,50,7,1,"material-9"),
        M(6,150,1,3),
        F(BlueprintFeature.Inspect,15,2,2,"material-6"),
        F(BlueprintFeature.Charge,40,2,3,"material-6"),
        F(BlueprintFeature.Rune,50,3,3,"function-Charge"),
        F(BlueprintFeature.Magic,50,3,2,"function-Inspect"),
        F(BlueprintFeature.Resonate,50,4,2,"function-Magic"),
        F(BlueprintFeature.Protect,50,3,4,"function-Charge"),
        F(BlueprintFeature.Curse,2000,8,4,"function-Protect","function-Refine"),
        M(4,60,1,6),
        F(BlueprintFeature.Fuel,25,2,6,"material-4","function-Heat"),
        F(BlueprintFeature.Recover,40,3,7,"function-Fuel"),
        F(BlueprintFeature.Wrap,40,3,6,"function-Fuel"),
        F(BlueprintFeature.Speed,100,4,6,"function-Wrap"),
        F(BlueprintFeature.AutoSell,100,5,6,"function-Speed"),
        F(BlueprintFeature.LuckyCode,50,3,8,"function-NightTime"),
        F(BlueprintFeature.Lucky,50,4,8,"function-LuckyCode","function-Charge"),
        new Offer(MasterCommission,ForgePurchaseKind.Commission,800,"명장의 마지막 의뢰","별무늬 별철 검 + 정제 용뼈 손잡이 + 충전 3회 + 서로 다른 계열 룬 3개. 균열 없이 완성해 제출하면 엔딩입니다.",8,1,0,BlueprintFeature.None,"function-Refine","function-Rune")
    });
    readonly HashSet<string> purchased=new HashSet<string>(StringComparer.Ordinal);
    public static Offer Find(string id)=>Offers.FirstOrDefault(o=>o.Id==id);
    public static bool RequiresPurchase(BlueprintFeature feature)=>Offers.Any(o=>o.Kind==ForgePurchaseKind.Function&&o.Feature==feature);
    public bool Owns(BlueprintFeature feature)=>Owns("function-"+feature);
    public bool Owns(string id)=>purchased.Contains(id);
    public bool Owns(Offer offer)=>offer.Kind!=ForgePurchaseKind.Material&&Owns(offer.Id);
    public bool MaterialAvailable(int material)=>material>=0&&material<=3||Owns("material-"+material);
    public BlueprintFeature[] Export()=>Offers.Where(o=>o.Kind==ForgePurchaseKind.Function&&Owns(o)).Select(o=>o.Feature).ToArray();
    public string[] ExportNodes()=>purchased.OrderBy(id=>id,StringComparer.Ordinal).ToArray();
    // Retain old purchases and grant their newly introduced ancestors without charging again.
    public void Restore(BlueprintFeature[] features,string[] nodes=null,ForgeMaterialStock[] legacyStocks=null)
    {
        purchased.Clear();
        foreach(var id in nodes??Array.Empty<string>())GrantWithParents(id);
        foreach(var f in features??Array.Empty<BlueprintFeature>())GrantWithParents("function-"+f);
        if(nodes==null)foreach(var stock in legacyStocks??Array.Empty<ForgeMaterialStock>())
            if(stock!=null&&(stock.unlocked||stock.count>0))GrantWithParents("material-"+stock.material);
    }
    void GrantWithParents(string id)
    {
        var offer=Find(id);
        if(offer==null||offer.Kind==ForgePurchaseKind.Material||!purchased.Add(id))return;
        foreach(var parent in offer.Parents)GrantWithParents(parent);
    }
    public string MissingPrerequisites(Offer offer)
    {
        var missing=offer.Parents.Where(id=>!Owns(id)).Select(id=>Find(id).Title).ToArray();
        return missing.Length==0?null:"선행 구매 필요: "+string.Join(" + ",missing);
    }
    public string PrerequisiteText(Offer offer)=>offer.Parents.Count==0?"선행: 기본 공방 (제공)":"선행 모두 필요: "+string.Join(" + ",offer.Parents.Select(id=>Find(id).Title));
    public long Stock(Offer offer,int iron,ForgeMaterialStock[] stocks)
    {
        if(offer.Material==1)return iron;
        var stock=Array.Find(stocks??Array.Empty<ForgeMaterialStock>(),s=>s!=null&&s.material==offer.Material);
        return stock==null?0:stock.count;
    }
    public static long TotalPrice(Offer offer,int quantity)=>(long)offer.Price*quantity;
    public string BlockReason(Offer offer,int gold,int iron,ForgeMaterialStock[] stocks,bool busy,int quantity=1)
    {
        if(offer==null||!Offers.Contains(offer))return "판매하지 않는 항목입니다.";
        if(quantity<1)return "수량은 1 이상의 정수로 입력하세요.";
        if(offer.Kind!=ForgePurchaseKind.Material&&quantity!=1)return "해금은 한 번에 하나만 구매할 수 있습니다.";
        if(busy)return "제작 중에는 구매할 수 없습니다.";
        if(Owns(offer))return "이미 구매한 항목입니다.";
        string missing=MissingPrerequisites(offer);if(missing!=null)return missing;
        if(gold<TotalPrice(offer,quantity))return "골드가 부족합니다.";
        if(offer.Kind==ForgePurchaseKind.Material&&Stock(offer,iron,stocks)>(offer.Material==1?int.MaxValue:long.MaxValue)-quantity)return "재료 보유 한도에 도달했습니다.";
        return null;
    }
    public bool TryPurchase(Offer offer,ref int gold,ref int iron,ref ForgeMaterialStock[] stocks,bool busy,out string message,int quantity=1)
    {
        message=BlockReason(offer,gold,iron,stocks,busy,quantity);if(message!=null)return false;
        if(offer.Kind!=ForgePurchaseKind.Material)purchased.Add(offer.Id);
        else if(offer.Material==1)iron+=quantity;
        if((offer.Kind==ForgePurchaseKind.Material&&offer.Material>1)||offer.Kind==ForgePurchaseKind.MaterialUnlock){
            var stock=Array.Find(stocks??Array.Empty<ForgeMaterialStock>(),s=>s!=null&&s.material==offer.Material);
            if(stock==null){stock=new ForgeMaterialStock{material=offer.Material};var list=new List<ForgeMaterialStock>(stocks??Array.Empty<ForgeMaterialStock>());list.Add(stock);stocks=list.ToArray();}
            stock.unlocked=true;
            if(offer.Kind==ForgePurchaseKind.Material)stock.count+=quantity;
        }
        gold-=(int)TotalPrice(offer,quantity);
        message=offer.Kind==ForgePurchaseKind.Material?ForgeMaterialGuide.Find(offer.Material).Name+" "+quantity+"개 구매 완료 · "+TotalPrice(offer,quantity)+"G 사용":offer.Title+" 구매 완료 · "+offer.Price+"G";
        return true;
    }
}
