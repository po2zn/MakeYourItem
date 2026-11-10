using System;
using System.Linq;

public static class ForgeTechnologyChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void BuyAllNodes(ForgeTechnology tech,ref int gold,ref int iron,ref ForgeMaterialStock[] stocks)
    {
        var pending=ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material&&!tech.Owns(o)).ToList();
        while(pending.Count>0){
            var ready=pending.FirstOrDefault(o=>tech.MissingPrerequisites(o)==null);
            Require(ready!=null,"graph cycle / unreachable node");
            Require(tech.TryPurchase(ready,ref gold,ref iron,ref stocks,false,out var message),message);
            pending.Remove(ready);
        }
    }
    public static void Main()
    {
        CheckBulkPurchase();
        var tech=new ForgeTechnology();int gold=10000,iron=0;ForgeMaterialStock[] stocks=null;string message;
        Require(ForgeTechnology.Offers.Select(o=>o.Id).Distinct().Count()==ForgeTechnology.Offers.Count,"unique IDs");
        foreach(var definition in ForgeExpansion.Definitions)
            Require(ForgeTechnology.RequiresPurchase(definition.Alias.Id)==(definition.Price>0),"expansion purchase coverage "+definition.Alias.Id);
        foreach(var offer in ForgeTechnology.Offers)
            foreach(var parent in offer.Parents){
                var ancestor=ForgeTechnology.Find(parent);
                Require(ancestor!=null&&ancestor.Kind!=ForgePurchaseKind.Material&&ancestor.Tier<offer.Tier || offer.Kind==ForgePurchaseKind.Material&&ancestor!=null,"acyclic graph");
            }
        foreach(var o in ForgeTechnology.Offers.Where(o=>o.Parents.Count>0)){
            int before=gold;
            Require(!tech.TryPurchase(o,ref gold,ref iron,ref stocks,false,out message)&&message.StartsWith("선행")&&gold==before&&!tech.Owns(o)&&stocks==null,"locked purchase atomic "+o.Id);
        }
        var heat=ForgeTechnology.Find("function-Heat");
        Require(ForgeMvp.Evaluate("p=1(1)\nprint(100(p))",technology:tech).compileError,"locked numeric function");
        Require(!tech.TryPurchase(heat,ref gold,ref iron,ref stocks,true,out message),"busy");
        gold=heat.Price-1;
        Require(!tech.TryPurchase(heat,ref gold,ref iron,ref stocks,false,out message)&&gold==heat.Price-1,"insufficient funds");
        gold=heat.Price;
        Require(tech.TryPurchase(heat,ref gold,ref iron,ref stocks,false,out message)&&gold==0,"exact funds");
        gold=10000;
        Require(!tech.TryPurchase(heat,ref gold,ref iron,ref stocks,false,out message)&&gold==10000,"duplicate");
        Require(ForgeMvp.Evaluate("p=1(1)\nprint(heat(p),100(p))",technology:tech).error==null,"both spellings");
        var fuel=ForgeTechnology.Find("function-Fuel");
        Require(!tech.TryPurchase(fuel,ref gold,ref iron,ref stocks,false,out message),"all parents required not any");
        Require(!tech.TryPurchase(new ForgeTechnology.Offer(1,0),ref gold,ref iron,ref stocks,false,out message),"unknown offer");
        BuyAllNodes(tech,ref gold,ref iron,ref stocks);
        foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind==ForgePurchaseKind.Material)){
            long before=tech.Stock(offer,iron,stocks);int balance=gold;
            Require(tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out message,2),message);
            Require(gold==balance-offer.Price*2&&tech.Stock(offer,iron,stocks)==before+2,"repeat stock "+offer.Material);
        }
        var restored=new ForgeTechnology();
        restored.Restore(tech.Export(),tech.ExportNodes(),stocks);
        Require(restored.ExportNodes().SequenceEqual(tech.ExportNodes()),"node save roundtrip");
        var legacy=new ForgeTechnology();
        var oldStocks=new[]{new ForgeMaterialStock{material=9,count=2,unlocked=true}};
        legacy.Restore(new[]{BlueprintFeature.Curse},null,oldStocks);
        Require(legacy.Owns(BlueprintFeature.Curse)&&legacy.Owns(BlueprintFeature.Protect)&&legacy.Owns(BlueprintFeature.Refine)&&legacy.MaterialAvailable(9)&&legacy.MaterialAvailable(7)&&legacy.MaterialAvailable(6),"legacy ancestor migration");
        Require(oldStocks[0].count==2,"migration preserves stock");
        Require(!legacy.Owns(ForgeTechnology.MasterCommission),"no accidental ending commission");
        legacy.Restore(Array.Empty<BlueprintFeature>(),Array.Empty<string>(),oldStocks);
        Require(!legacy.MaterialAvailable(9),"new save trusts node IDs rather than debug stock");
        Console.WriteLine("PASS tree: unique DAG, all-parent gates, atomicity, stock/permits, both spellings, legacy migration and roundtrip");
        BlueprintMaterialChecks.Main();
    }
    static void CheckBulkPurchase()
    {
        var technology=new ForgeTechnology();int gold=50,iron=0;ForgeMaterialStock[] stocks=null;string message;
        var ironOffer=ForgeTechnology.Offers.First(o=>o.Material==1&&o.Kind==ForgePurchaseKind.Material);
        var water=ForgeTechnology.Offers.First(o=>o.Material==3&&o.Kind==ForgePurchaseKind.Material);
        Require(technology.TryPurchase(ironOffer,ref gold,ref iron,ref stocks,false,out message,10)&&gold==0&&iron==10,"bulk exact balance");
        Require(message.Contains("10개")&&message.Contains("50G"),"bulk receipt");
        Require(!technology.TryPurchase(water,ref gold,ref iron,ref stocks,false,out message,3)&&gold==0&&stocks==null,"insufficient bulk funds atomic");
        gold=10;
        Require(technology.TryPurchase(water,ref gold,ref iron,ref stocks,false,out message,7)&&gold==3&&stocks[0].count==7&&stocks[0].unlocked,"water bulk stock and price");
        foreach(int quantity in new[]{0,-1,int.MaxValue})Require(!technology.TryPurchase(water,ref gold,ref iron,ref stocks,false,out message,quantity)&&gold==3&&stocks[0].count==7,"invalid/unaffordable quantity atomic");
        gold=100;iron=int.MaxValue-1;
        Require(!technology.TryPurchase(ironOffer,ref gold,ref iron,ref stocks,false,out message,2)&&gold==100&&iron==int.MaxValue-1,"bulk iron overflow");
        stocks[0].count=long.MaxValue-1;
        Require(!technology.TryPurchase(water,ref gold,ref iron,ref stocks,false,out message,2)&&gold==100&&stocks[0].count==long.MaxValue-1,"bulk long stock overflow");
        Require(ForgeTechnology.TotalPrice(ironOffer,int.MaxValue)==(long)int.MaxValue*5,"wide price calculation");
        var feature=ForgeTechnology.Offers.First(o=>o.Kind==ForgePurchaseKind.Function);
        Require(!technology.TryPurchase(feature,ref gold,ref iron,ref stocks,false,out message,2)&&gold==100&&!technology.Owns(feature.Feature),"function quantity remains one");
        Console.WriteLine("PASS bulk purchase: quantities, totals, exact/insufficient gold, atomic rejection and overflow guards");
    }
}
