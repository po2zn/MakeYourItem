using System;
using System.Linq;

public static class ForgeTechnologyChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Main()
    {
        CheckBulkPurchase();
        var technology=new ForgeTechnology();int gold=1000,iron=0;ForgeMaterialStock[] stocks=null;string message;
        foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind==ForgePurchaseKind.Material)){
            int before=gold;
            Require(technology.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out message),message);
            Require(technology.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out message),message);
            Require(gold==before-2*offer.Price&&technology.Stock(offer,iron,stocks)==2,"repeat material purchase");
        }
        Require(stocks.Length==8&&stocks.All(s=>s.unlocked),"material unlocks");
        var function=ForgeTechnology.Offers.First(o=>o.Feature==BlueprintFeature.Abs);
        var locked=ForgeMvp.Evaluate("print(101(-3))",technology:technology);
        Require(locked.compileError&&locked.steps.Count==0&&locked.cost==0,"locked numeric call");
        Require(ForgeMvp.Evaluate("def f():\n    return abs(-3)\nprint(1)",technology:technology).compileError,"locked call inside function");
        Require(ForgeMvp.Evaluate("if False:\n    print(abs(-3))\nprint(1)",technology:technology).compileError,"locked unreachable call");
        Require(ForgeMvp.Evaluate(ForgeEconomyChecks.Basic,technology:technology).error==null,"basic crafting stays free");
        int balance=gold;
        Require(!technology.TryPurchase(function,ref gold,ref iron,ref stocks,true,out message)&&gold==balance&&!technology.Owns(function.Feature),"busy purchase atomicity");
        gold=function.Price-1;
        Require(!technology.TryPurchase(function,ref gold,ref iron,ref stocks,false,out message)&&gold==function.Price-1&&!technology.Owns(function.Feature),"insufficient funds");
        gold=function.Price;
        Require(technology.TryPurchase(function,ref gold,ref iron,ref stocks,false,out message)&&gold==0,"exact funds");
        gold=100;
        Require(!technology.TryPurchase(function,ref gold,ref iron,ref stocks,false,out message)&&gold==100,"duplicate purchase");
        foreach(string name in new[]{"101","abs"})Require(ForgeMvp.Evaluate("print("+name+"(-3))",technology:technology).error==null,"unlocked spelling "+name);
        var ironOffer=ForgeTechnology.Offers[0];iron=int.MaxValue;
        Require(!technology.TryPurchase(ironOffer,ref gold,ref iron,ref stocks,false,out message)&&gold==100&&iron==int.MaxValue,"iron overflow");
        stocks[0].count=long.MaxValue;
        Require(!technology.TryPurchase(ForgeTechnology.Offers[1],ref gold,ref iron,ref stocks,false,out message)&&gold==100,"material overflow");
        Require(!technology.TryPurchase(new ForgeTechnology.Offer(1,0),ref gold,ref iron,ref stocks,false,out message)&&gold==100,"unknown offer");
        var fresh=new ForgeTechnology();gold=1000;
        string[] expressions={"100(Blade)","101(-3)","1111([1])","000([1])","999([1])","66(1,2)","1010()","12()","24(60)"};
        int index=0;
        foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind==ForgePurchaseKind.Function&&o.Feature<BlueprintFeature.Attach)){
            string code="Blade = 1(1)\nprint("+expressions[index++]+")\n2(Blade,7)\n3(Blade)\n4(Blade,0)\n!Blade";
            Require(ForgeMvp.Evaluate(code,technology:fresh).compileError,"every feature starts locked");
            Require(fresh.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out message),message);
            Require(ForgeMvp.Evaluate(code,technology:fresh).error==null,"every purchased feature executes");
        }
        Console.WriteLine("PASS technology: repeated materials, stock unlocks, funds, duplicate/busy/overflow guards, all function gates and both spellings");
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
