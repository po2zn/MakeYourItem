using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum ForgePurchaseKind { Material, Function, MaterialUnlock, Commission }

// One graph controls purchases, the visible edges, and save migration.
public sealed class ForgeTechnology
{
    public const string MasterCommission="master-commission";
    public const string AutomationRoot="automation-root";
    public const string EnglishResearch="research-english-keywords";
    public const string RowSaleResearch="research-row-sale";
    public const string ImportResearch="research-import";
    public static readonly BlueprintFeature[] ControlFeatures={BlueprintFeature.If,BlueprintFeature.Elif,BlueprintFeature.Else,BlueprintFeature.For,BlueprintFeature.While,BlueprintFeature.Break,BlueprintFeature.Continue,BlueprintFeature.Def,BlueprintFeature.Return};
    public sealed class Offer
    {
        public readonly ForgePurchaseKind Kind;
        public readonly int Material,Price,Tier,Lane;
        public int SpeedLevel => Id.StartsWith("speed-")?int.Parse(Id.Substring(6)):0;
        public readonly BlueprintFeature Feature;
        public readonly ReadOnlyCollection<BlueprintFeature> Features;
        public readonly string Id,Name,Description;
        public readonly ReadOnlyCollection<string> Parents;
        public string Title => Name;
        public Offer(int material,int price):this("stock-"+material,ForgePurchaseKind.Material,price,ForgeMaterialGuide.Find(material).Heading,"보유 재고에 추가됩니다.",0,0,material,BlueprintFeature.None,material>3?new[]{"material-"+material}:Array.Empty<string>()){}
        public Offer(BlueprintFeature feature,int price):this("function-"+feature,ForgePurchaseKind.Function,price,BlueprintAliases.Find(feature).Symbol+"() · "+BlueprintAliases.Find(feature).English,BlueprintAliases.Find(feature).Description,1,0,0,feature,Array.Empty<string>()){}
        internal Offer(string id,ForgePurchaseKind kind,int price,string name,string description,int tier,int lane,int material,BlueprintFeature feature,params string[] parents)
        {Id=id;Kind=kind;Price=kind==ForgePurchaseKind.Material?price:ForgeBalance.ResearchPrice(id,price);Name=name;Description=description;Tier=tier+(kind!=ForgePurchaseKind.Material&&id!=AutomationRoot?1:0);Lane=lane;Material=material;Feature=feature;
            Parents=Array.AsReadOnly(parents.Length==0&&kind!=ForgePurchaseKind.Material&&id!=AutomationRoot?new[]{AutomationRoot}:parents.Select(CanonicalId).ToArray());
            Features=Array.AsReadOnly(id==AutomationRoot?(BlueprintFeature[])ControlFeatures.Clone():kind!=ForgePurchaseKind.Function||feature==BlueprintFeature.None?Array.Empty<BlueprintFeature>():feature==BlueprintFeature.Min?new[]{feature,BlueprintFeature.Max}:feature==BlueprintFeature.IsDay?new[]{feature,BlueprintFeature.NightTime}:feature==BlueprintFeature.Lucky?new[]{feature,BlueprintFeature.LuckyCode}:new[]{feature});}
    }
    static Offer F(BlueprintFeature f,int price,int tier,int lane,params string[] parents)
    {var a=BlueprintAliases.Find(f);return new Offer("function-"+f,ForgePurchaseKind.Function,price,a.Symbol+"() · "+a.English,a.Description,tier,lane,0,f,parents);}
    static Offer M(int material,int price,int tier,int lane,params string[] parents)
    {return new Offer("material-"+material,ForgePurchaseKind.MaterialUnlock,price,ForgeMaterialGuide.Find(material).Name+" 공급 해금","한 번 해금하면 재료 상점에서 필요한 수량을 계속 구매할 수 있습니다. 해금비에 재료는 포함되지 않습니다.",tier,lane,material,BlueprintFeature.None,parents);}
    static Offer MaterialBundle(int material,BlueprintFeature feature,int price,int tier,int lane,string name,params string[] parents)
    {return new Offer("function-"+feature,ForgePurchaseKind.Function,price,name,"재료 공급과 함수를 함께 해금합니다. 재료 재고는 상점에서 별도로 구매합니다.\n"+BlueprintAliases.Find(feature).Description,tier,lane,material,feature,parents);}
    static Offer Bundle(BlueprintFeature feature,BlueprintFeature companion,int price,int tier,int lane,string name,params string[] parents)
    {return new Offer("function-"+feature,ForgePurchaseKind.Function,price,name,"한 번 구매하면 두 함수의 숫자/영문 표기가 함께 해금됩니다.\n"+BlueprintAliases.Find(feature).Description+"\n"+BlueprintAliases.Find(companion).Description,tier,lane,0,feature,parents);}
    public static float SpeedMultiplier(int level)=>level<=0?1f:level==1?1.5f:level;
    public float RestoreSpeed(float saved)=>MaxSpeedLevel==0?1f:Math.Max(1.5f,Math.Min(float.IsNaN(saved)?1.5f:saved,SpeedMultiplier(MaxSpeedLevel)));
    static Offer Speed(int level,int price)=>new Offer("speed-"+level,ForgePurchaseKind.Function,price,"스피드 "+level+"단계 · "+SpeedMultiplier(level)+"배",
        (level==1?"기본 속도는 1배입니다. 1단계 구매 즉시 1.5배로 설정됩니다. 222(1)도 1.5배이며 속도 제어와 다음 단계 구매를 엽니다.":"구매 즉시 "+level+"배로 설정됩니다. 222(1)부터 222("+level+")까지 사용할 수 있습니다.")
        +(level==5?" 최대 속도 단계입니다. 자동판매는 다음 노드를 별도로 구매해야 사용할 수 있습니다.":" 다음 단계는 이 단계를 구매한 뒤 해금할 수 있습니다."),
        level+3,6,0,BlueprintFeature.Speed,level==1?"function-Wrap":"speed-"+(level-1));
    public static readonly ReadOnlyCollection<Offer> Offers=Array.AsReadOnly(new[]{
        new Offer(1,5),new Offer(2,3),new Offer(3,1),new Offer(4,20),new Offer(5,30),
        new Offer(6,30),new Offer(7,40),new Offer(8,25),new Offer(9,50),
        new Offer(AutomationRoot,ForgePurchaseKind.Function,500,"제어문 해금 · 자동화 시작","조건·반복·반복 제어·사용자 함수의 숫자/기호 표기를 해금합니다. 영어 예약어는 별도 연구가 필요합니다. 구매 후 모든 분야의 첫 노드를 구매할 수 있습니다.",1,4,0,BlueprintFeature.If),
        new Offer(EnglishResearch,ForgePurchaseKind.Function,500,"영어 예약어","제어문을 영어로도 작성합니다. if / elif / else / for / while / break / continue / def / return. 숫자·기호 표기도 계속 사용할 수 있습니다. in, and, or, not, True, False, None은 기본 문법이며 import는 별도 연구를 따릅니다.",4,10,0,BlueprintFeature.None,"function-Min"),
        F(BlueprintFeature.Heat,20,1,0),
        // General computation branches start from the workshop, independently of forging.
        F(BlueprintFeature.Abs,15,1,10),
        F(BlueprintFeature.Len,15,1,11),
        new Offer(ImportResearch,ForgePurchaseKind.Function,500,"import · 모듈 가져오기","다른 코드 창을 모듈로 가져옵니다. import, from ... import ..., as 별칭을 함께 해금합니다. 파일이나 외부 Python 패키지는 가져올 수 없습니다.",1,12,0,BlueprintFeature.None),
        Bundle(BlueprintFeature.Min,BlueprintFeature.Max,5000,2,10,"최솟값·최댓값 · min / max","function-Abs"),
        F(BlueprintFeature.Random,30,3,11,"function-Set"),
        F(BlueprintFeature.Set,25,2,11,"function-Len"),
        Bundle(BlueprintFeature.IsDay,BlueprintFeature.NightTime,40,1,8,"시간 조회 · 12() + 24()"),
        F(BlueprintFeature.Polish,25,2,0,"function-Heat"),
        M(5,120,3,0,"function-Polish"),
        F(BlueprintFeature.Plate,30,4,0,"material-5"),
        M(7,300,5,0,"function-Plate"),
        M(8,180,6,0,"material-7"),
        MaterialBundle(9,BlueprintFeature.Refine,10000,6,1,"용뼈 + 10()","material-7","function-Charge"),
        MaterialBundle(6,BlueprintFeature.Charge,15000,1,4,"마력 수정 + 9()"),
        F(BlueprintFeature.Inspect,15,2,3,"material-6"),
        F(BlueprintFeature.Rune,50,3,4,"function-Charge"),
        F(BlueprintFeature.Magic,50,3,3,"function-Inspect"),
        F(BlueprintFeature.Resonate,50,4,3,"function-Magic"),
        F(BlueprintFeature.Protect,50,3,5,"function-Charge"),
        F(BlueprintFeature.Curse,2000,8,5,"function-Protect","function-Refine"),
        new Offer("function-Fuel",ForgePurchaseKind.Function,2400,"석탄 + 8() 화로","석탄 공급과 8() 화로 온도 설정을 함께 해금합니다. 석탄은 재료 상점에서 개당 20G로 구매합니다. 해금비에 석탄 재고는 포함되지 않습니다.",1,6,4,BlueprintFeature.Fuel,AutomationRoot),
        F(BlueprintFeature.Recover,40,3,7,"function-Fuel"),
        F(BlueprintFeature.Wrap,40,3,6,"function-Fuel"),
        new Offer(RowSaleResearch,ForgePurchaseKind.Function,22000,"한 라인씩 팔기","선택한 완성품이 있는 행을 한 번에 판매합니다. 한 행은 6개이며 마지막 행은 남은 개수만 판매합니다.",7,7,0,BlueprintFeature.None,"speed-3"),
        Speed(1,100),Speed(2,150),Speed(3,200),Speed(4,300),Speed(5,450),
        new Offer("function-AutoSell",ForgePurchaseKind.Function,300000,"자동판매 · 225()","스피드 5단계 이후 별도 구매합니다. 구매하면 자동판매가 켜집니다. 225(0)으로 끄고 225(1)로 다시 켤 수 있습니다.",9,6,0,BlueprintFeature.AutoSell,"speed-5"),
        Bundle(BlueprintFeature.Lucky,BlueprintFeature.LuckyCode,500,1,2,"오늘의 행운 · 777() + 777?()"),
        new Offer(MasterCommission,ForgePurchaseKind.Commission,800,"명장의 마지막 의뢰","별무늬 별철 검 + 정제 용뼈 손잡이 + 충전 3회 + 서로 다른 계열 룬 3개. 균열 없이 완성해 제출하면 엔딩입니다.",8,1,0,BlueprintFeature.None,"function-Refine","function-Rune")
    });
    readonly HashSet<string> purchased=new HashSet<string>(StringComparer.Ordinal);
    static string CanonicalId(string id)
    {
        foreach(var f in ControlFeatures)if(id=="function-"+f)return AutomationRoot;
        return id=="function-Max"?"function-Min":id=="material-6"?"function-Charge":id=="material-9"?"function-Refine":id=="material-4"?"function-Fuel":id=="function-NightTime"?"function-IsDay":id=="function-LuckyCode"?"function-Lucky":id=="function-Speed"?"speed-1":id;
    }
    public static Offer Find(string id)=>Offers.FirstOrDefault(o=>o.Id==CanonicalId(id));
    public static bool RequiresPurchase(BlueprintFeature feature)=>Offers.Any(o=>o.Features.Contains(feature));
    public bool Owns(BlueprintFeature feature)=>Owns("function-"+feature);
    public bool Owns(string id)=>purchased.Contains(CanonicalId(id));
    public bool Owns(Offer offer)=>offer.Kind!=ForgePurchaseKind.Material&&purchased.Contains(offer.Id);
    public bool MaterialAvailable(int material)=>material>=0&&material<=3||Owns("material-"+material);
    public int MaxSpeedLevel=>Offers.Where(o=>o.SpeedLevel>0&&Owns(o)).Select(o=>o.SpeedLevel).DefaultIfEmpty(0).Max();
    public BlueprintFeature[] Export()=>Offers.Where(o=>o.Kind==ForgePurchaseKind.Function&&Owns(o)).SelectMany(o=>o.Features).Distinct().ToArray();
    public string[] ExportNodes()=>purchased.OrderBy(id=>id,StringComparer.Ordinal).ToArray();
    // Retain old purchases and grant their newly introduced ancestors without charging again.
    public void Restore(BlueprintFeature[] features,string[] nodes=null,ForgeMaterialStock[] legacyStocks=null)
    {
        purchased.Clear();
        foreach(var id in nodes??Array.Empty<string>())GrantWithParents(id=="function-Speed"?"function-AutoSell":id);
        foreach(var f in features??Array.Empty<BlueprintFeature>()){
            // New node saves are authoritative for levels; the legacy feature flag had all five speeds.
            if(f==BlueprintFeature.Speed){if(nodes==null)GrantWithParents("function-AutoSell");}
            else GrantWithParents("function-"+f);
        }
        if(nodes==null)foreach(var stock in legacyStocks??Array.Empty<ForgeMaterialStock>())
            if(stock!=null&&(stock.unlocked||stock.count>0))GrantWithParents("material-"+stock.material);
    }
    void GrantWithParents(string id)
    {
        var offer=Find(id);
        if(offer==null||offer.Kind==ForgePurchaseKind.Material||!purchased.Add(offer.Id))return;
        foreach(var parent in offer.Parents)GrantWithParents(parent);
    }
    public string MissingPrerequisites(Offer offer)
    {
        var missing=offer.Parents.Where(id=>!Owns(id)).Select(id=>Find(id).Title).ToArray();
        return missing.Length==0?null:"선행 구매 필요: "+string.Join(" + ",missing);
    }
    public string PrerequisiteText(Offer offer)=>offer.Parents.Count==0?"선행: 기본 공방 (제공)":"선행 구매 필요: "+string.Join(" + ",offer.Parents.Select(id=>Find(id).Title));
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
        // Purchases share the live ledger with crafting; being busy is not a purchase gate.
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
        if((offer.Kind==ForgePurchaseKind.Material&&offer.Material>1)||offer.Kind==ForgePurchaseKind.MaterialUnlock||(offer.Kind==ForgePurchaseKind.Function&&offer.Material>0)){
            var stock=Array.Find(stocks??Array.Empty<ForgeMaterialStock>(),s=>s!=null&&s.material==offer.Material);
            if(stock==null){stock=new ForgeMaterialStock{material=offer.Material};var list=new List<ForgeMaterialStock>(stocks??Array.Empty<ForgeMaterialStock>());list.Add(stock);stocks=list.ToArray();}
            stock.unlocked=true;
            if(offer.Kind==ForgePurchaseKind.Material)stock.count+=quantity;
        }
        gold-=(int)TotalPrice(offer,quantity);
        message=offer.Kind==ForgePurchaseKind.Material?ForgeMaterialGuide.Find(offer.Material).Name+" "+quantity+"개 구매 완료 · "+ForgeMaterialHud.FormatCount(TotalPrice(offer,quantity))+"G 사용":offer.Title+" 구매 완료 · "+ForgeMaterialHud.FormatCount(offer.Price)+"G";
        return true;
    }
}
