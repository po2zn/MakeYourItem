using System;
using System.Linq;
using System.Collections.Generic;

public static class ForgeCampaignChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    const string Base="p=1(1)\n2(p)\n3(p)\n4(p,3)\n5(p,2)\n!p";
    const string Plated="p=1(1)\n2(p)\n3(p)\n4(p,3)\n6(p)\n7(p)\n5(p,2)\n!p";
    const string Star="p=1(7)\n2(p)\n3(p)\n4(p,3)\n6(p)\n7(p)\n5(p,2)\n!p";
    const string Charged="p=1(7)\n2(p)\n3(p)\n4(p,3)\n6(p)\n7(p)\n5(p,2)\n9(p,1)\n9(p,1)\n9(p,1)\n!p";
    const string Dragon="p=1(7)\n2(p)\n3(p)\n4(p,3)\n6(p)\n7(p)\nb=1(9)\n10(b,p,100,13)\n5(p,b)\n9(p,1)\n9(p,1)\n9(p,1)\n!p";
    public const string Final="p=1(7)\n2(p)\n3(p)\n4(p,3)\n6(p)\n7(p)\nb=1(9)\n10(b,p,100,13)\n5(p,b)\n9(p,1)\n9(p,1)\n9(p,1)\n123(r in [7,8,9]):\n    211(p,r)\n!p";
    sealed class Run
    {
        public readonly ForgeTechnology Tech=new ForgeTechnology();
        public int Gold=100,Iron=10,Sales;public long Revenue;
        public ForgeMaterialStock[] Stocks=Array.Empty<ForgeMaterialStock>();
        public string Recipe=Base;
        public void Buy(string id)
        {
            var offer=ForgeTechnology.Find(id);
            while(Gold<offer.Price+250){Craft(Recipe);Require(Sales<200000,"progression stalled");}
            Require(Tech.TryPurchase(offer,ref Gold,ref Iron,ref Stocks,false,out var message),message);
        }
        public ForgeProduct Craft(string source)
        {
            // Discover exact required stock from an isolated evaluation, then buy only shortages.
            var preview=ForgeMvp.Evaluate(source,technology:Tech);
            Require(preview.error==null,preview.error);
            var needed=preview.product.Data.materials;
            foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind==ForgePurchaseKind.Material)){
                int missing=(int)Math.Max(0,needed[offer.Material]-Tech.Stock(offer,Iron,Stocks));
                if(missing>0)Require(Tech.TryPurchase(offer,ref Gold,ref Iron,ref Stocks,false,out var message,missing),message);
            }
            var available=Stocks.Where(s=>s.unlocked).ToDictionary(s=>s.material,s=>s.count);
            var actual=ForgeMvp.Evaluate(source,Gold,Iron,materialStocks:available,technology:Tech);
            Require(actual.error==null&&actual.product!=null,actual.error);
            int cost=0;
            foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind==ForgePurchaseKind.Material)){
                int consumed=actual.product.Data.materials[offer.Material];cost+=consumed*offer.Price;
                if(offer.Material==1)Iron-=consumed;else if(consumed>0)Stocks.Single(s=>s.material==offer.Material).count-=consumed;
            }
            var shop=new ForgeShop();int id=shop.Add(actual.product);
            Require(shop.TrySell(id,ref Gold,out var sold),"sale");
            int price=ForgePricing.Quote(sold).SalePrice;
            Require(price>cost,"repeat recipe must be profitable after replacement materials");
            Sales++;Revenue+=price;
            return sold;
        }
    }
    public static string RunChecks()
    {
        var run=new Run();
        run.Craft(Base);
        run.Buy(ForgeTechnology.AutomationRoot);
        foreach(var id in new[]{"function-Heat","function-Polish","material-5","function-Plate"})run.Buy(id);
        run.Recipe=Plated;
        run.Buy("material-7");run.Recipe=Star;
        run.Buy("function-Charge");run.Recipe=Charged;
        run.Buy("function-Refine");run.Recipe=Dragon;
        run.Buy("function-Rune");run.Recipe=Final;
        var before=run.Craft(Final);
        Require(ForgeCampaign.IsMasterwork(before)&&!ForgeCampaign.CanComplete(run.Tech,before),"commission must be accepted");
        run.Buy(ForgeTechnology.MasterCommission);
        var master=run.Craft(Final);
        Require(ForgeCampaign.CanComplete(run.Tech,master),"actual route reaches ending");
        Require(run.Gold>=0&&run.Iron>=0&&run.Stocks.All(s=>s.count>=0),"resources non-negative");
        foreach(string name in new[]{"crack","no star","handle","charge","runes","duplicate family"}){
            var data=master.Data;
            if(name=="crack")data.cracked=true;
            if(name=="no star")data.star=false;
            if(name=="handle")data.handle=2;
            if(name=="charge")data.charge=2;
            if(name=="runes")data.runes.RemoveAt(0);
            if(name=="duplicate family")data.runes=new List<int>{1,4,7};
            var invalid=new ForgeProduct("test",0,true,master.Traits,0,0,7,data);
            Require(!ForgeCampaign.CanComplete(run.Tech,invalid),"reject false ending "+name);
        }
        var restored=new ForgeTechnology();restored.Restore(run.Tech.Export(),run.Tech.ExportNodes());
        Require(ForgeCampaign.CanComplete(restored,master),"saved commission persists");
        string result="PASS campaign route: "+run.Sales+" sales, revenue "+run.Revenue+"G, balance "+run.Gold+"G; final value "+ForgePricing.Quote(master).SalePrice+"G";
        foreach(var pair in new[]{Tuple.Create("basic",Base),Tuple.Create("silver",Plated),Tuple.Create("star",Star),Tuple.Create("charged",Charged),Tuple.Create("dragon",Dragon),Tuple.Create("final",Final)}){
            var e=ForgeMvp.Evaluate(pair.Item2);int cost=ForgeTechnology.Offers.Where(o=>o.Kind==ForgePurchaseKind.Material).Sum(o=>o.Price*e.product.Data.materials[o.Material]);
            result+="\n"+pair.Item1+": sale "+ForgePricing.Quote(e.product).SalePrice+"G / materials "+cost+"G / net "+(ForgePricing.Quote(e.product).SalePrice-cost)+"G";
        }
        return result;
    }
    public static void Main()
    {
        ForgeTechnologyChecks.Main();
        Console.WriteLine(RunChecks());
        Console.WriteLine(ForgeEconomyChecks.Run());
        Console.WriteLine(ForgeExpansionChecks.Run());
        Console.WriteLine(BlueprintTimeChecks.Run());
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Campaign Route")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(RunChecks());}
#endif
}
