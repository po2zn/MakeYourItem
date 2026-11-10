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
        CheckBundles();
        CheckSpeedLevels();
        CheckResearch();
        CheckControlRoot();
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
        Require(tech.TryPurchase(ForgeTechnology.Find(ForgeTechnology.AutomationRoot),ref gold,ref iron,ref stocks,false,out message),message);
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
    static void CheckControlRoot()
    {
        var tech=new ForgeTechnology();int gold=100,iron=10;ForgeMaterialStock[] stocks=null;
        var root=ForgeTechnology.Find(ForgeTechnology.AutomationRoot);
        Require(!tech.TryPurchase(root,ref gold,ref iron,ref stocks,false,out var message)&&gold==100,"initial funds require manual crafting first");
        Require(ForgeMvp.Evaluate("p=1(1)\n2(p,7)\n3(p)\n4(p,0)\n5(p,2)\n!p",technology:tech).error==null,"basic five processes remain free");
        string[] samples={
            "if True:\n    print(1)\nelif False:\n    print(2)\nelse:\n    print(3)",
            "1?(True):\n    print(1)\n01?(False):\n    print(2)\n0?:\n    print(3)",
            "for n in [1,2]:\n    continue", "123(n in [1,2]):\n    ~~",
            "while True:\n    break", "888(True):\n    !!",
            "def f():\n    return 1\nprint(f())", "333 f():\n    << 1\nprint(f())"
        };
        foreach(var source in samples){
            var locked=ForgeMvp.Evaluate(source,technology:tech);
            Require(locked.compileError&&locked.steps.Count==0,"locked control without side effects");
            using(var session=BlueprintRuntime.StartSession("print(0)\n"+source,0,0,technology:tech,deferErrors:true)){
                session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.error==null,"valid prefix allowed");
                session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.error!=null&&session.Evaluation.steps.Count==1,"stepped control blocked at execution");
            }
        }
        var modules=new System.Collections.Generic.Dictionary<string,string>{{"logic","def f():\n    return 1"}};
        Require(ForgeMvp.Evaluate("import logic\nprint(logic.f())",technology:tech,modules:modules).error!=null,"imports cannot bypass control lock");
        Require(ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material&&o.Id!=root.Id).All(o=>o.Parents.Count>0),"all branches descend from common root");
        gold=150;Require(tech.TryPurchase(root,ref gold,ref iron,ref stocks,false,out message)&&gold==0,message);
        Require(ForgeTechnology.ControlFeatures.All(tech.Owns),"all nine control forms unlocked");
        foreach(var source in samples)Require(ForgeMvp.Evaluate(source,technology:tech).error==null,"unlocked controls execute");
        Require(ForgeMvp.Evaluate("import logic\nprint(logic.f())",technology:tech,modules:modules).error==null,"imported control works after unlock");
        var old=new ForgeTechnology();old.Restore(new[]{BlueprintFeature.Heat});
        Require(old.Owns(root.Id),"previous purchases retain access through root migration");
        var again=new ForgeTechnology();again.Restore(tech.Export(),tech.ExportNodes());
        Require(again.ExportNodes().SequenceEqual(tech.ExportNodes()),"root save roundtrip");
        Console.WriteLine("PASS control root: basic processes, locked English/symbol controls, stepping/import gates, common prerequisite and save compatibility");
    }
    static void CheckResearch()
    {
        var tech=new ForgeTechnology();int gold=350,iron=0;ForgeMaterialStock[] stocks=null;
        Require(tech.TryPurchase(ForgeTechnology.Find(ForgeTechnology.AutomationRoot),ref gold,ref iron,ref stocks,false,out var rootMessage),rootMessage);
        foreach(var path in new[]{new[]{BlueprintFeature.Abs,BlueprintFeature.Min,BlueprintFeature.Max},new[]{BlueprintFeature.Len,BlueprintFeature.Set,BlueprintFeature.Random}}){
            var last=ForgeTechnology.Find("function-"+path[2]);int before=gold;
            Require(!tech.TryPurchase(last,ref gold,ref iron,ref stocks,false,out var reason)&&gold==before,"research cannot skip prerequisites");
            foreach(var feature in path){
                var offer=ForgeTechnology.Find("function-"+feature);
                Require(offer.Lane>=10&&offer.Parents.All(id=>id==ForgeTechnology.AutomationRoot||ForgeTechnology.Find(id).Lane>=10),"research prerequisites stay in research after common root");
                Require(tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out var message),message);
            }
        }
        Require(gold==75&&!tech.Owns(BlueprintFeature.Heat)&&stocks==null,"research independent of crafting, prices unchanged");
        Require(ForgeMvp.Evaluate("print(101(-3),000([1,2]),999([1,2]),1111([1]),1010(),66(1,2))",technology:tech).error==null,"research functions execute");
        var restored=new ForgeTechnology();restored.Restore(tech.Export(),tech.ExportNodes());
        Require(restored.ExportNodes().SequenceEqual(tech.ExportNodes()),"research save compatibility");
        foreach(var feature in new[]{BlueprintFeature.Heat,BlueprintFeature.Polish,BlueprintFeature.Charge,BlueprintFeature.Speed,BlueprintFeature.IsDay,BlueprintFeature.Lucky})
            Require(ForgeTechnology.Find("function-"+feature).Lane<10,"crafting and operations remain outside research");
        Console.WriteLine("PASS research: independent branches, prerequisites, existing prices, execution and save roundtrip");
    }
    static void CheckSpeedLevels()
    {
        var tech=new ForgeTechnology();int gold=10000,iron=0;ForgeMaterialStock[] stocks=null;
        Require(tech.MaxSpeedLevel==0&&!tech.Owns(BlueprintFeature.AutoSell),"new game has no speed purchases");
        tech.Restore(new[]{BlueprintFeature.Wrap});
        for(int level=1;level<=5;level++){
            var offer=ForgeTechnology.Find("speed-"+level);
            if(level<5){int before=gold;Require(!tech.TryPurchase(ForgeTechnology.Find("speed-"+(level+1)),ref gold,ref iron,ref stocks,false,out var blocked)&&gold==before,"cannot skip speed level");}
            int balance=gold;gold=offer.Price-1;
            Require(!tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out var error)&&tech.MaxSpeedLevel==level-1,"unaffordable speed atomic");gold=balance;
            Require(tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out var message)&&gold==balance-offer.Price,message);
            Require(tech.MaxSpeedLevel==level&&tech.Owns(BlueprintFeature.AutoSell)==(level==5),"speed cap and free auto sell reward");
            Require(ForgeMvp.Evaluate("222("+level+")\nspeed(1)",technology:tech).error==null,"owned speed works");
            if(level<5){
                var failed=ForgeMvp.Evaluate("n="+(level+1)+"\n222(n)\nprint(1)",technology:tech);
                Require(failed.error!=null&&failed.steps.Count==0,"dynamic higher speed blocked without setting receipt");
                var modules=new System.Collections.Generic.Dictionary<string,string>{{"fast","def run():\n    speed("+(level+1)+")"}};
                Require(ForgeMvp.Evaluate("import fast\nfast.run()",technology:tech,modules:modules).error!=null,"import cannot bypass cap");
                using(var session=BlueprintRuntime.StartSession("222("+(level+1)+")",0,0,technology:tech,deferErrors:true)){
                    session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.error!=null&&session.Evaluation.steps.Count==0,"stepped execution cannot bypass cap");
                }
            }
            var restored=new ForgeTechnology();restored.Restore(tech.Export(),tech.ExportNodes());
            Require(restored.MaxSpeedLevel==level&&restored.Owns(BlueprintFeature.AutoSell)==(level==5),"partial speed save must not unlock higher levels");
            Require((ForgeMvp.Evaluate("225(1)",technology:tech).error==null)==(level==5),"auto sell locked until all five levels");
            balance=gold;Require(!tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out message)&&gold==balance,"speed duplicate atomic");
        }
        foreach(string id in new[]{"function-Speed","function-AutoSell"}){
            var old=new ForgeTechnology();old.Restore(Array.Empty<BlueprintFeature>(),new[]{id});
            Require(old.MaxSpeedLevel==5&&old.Owns(BlueprintFeature.AutoSell),"legacy speed access preserved");
        }
        var legacy=new ForgeTechnology();legacy.Restore(new[]{BlueprintFeature.Speed});
        Require(legacy.MaxSpeedLevel==5,"feature-only legacy speed preserved");
        Console.WriteLine("PASS speed levels: sequential/atomic purchases, cap, imports, stepping, automatic reward, save roundtrips and migration");
    }
    static void CheckBundles()
    {
        var tech=new ForgeTechnology();int gold=1000,iron=0;ForgeMaterialStock[] stocks=null;
        tech.Restore(new[]{BlueprintFeature.If});
        var time=ForgeTechnology.Find("function-IsDay");var luck=ForgeTechnology.Find("function-Lucky");
        Require(time==ForgeTechnology.Find("function-NightTime")&&luck==ForgeTechnology.Find("function-LuckyCode"),"one node per bundle");
        Require(!tech.TryPurchase(luck,ref gold,ref iron,ref stocks,false,out var message)&&gold==1000,"luck prerequisites atomic");
        Require(tech.TryPurchase(time,ref gold,ref iron,ref stocks,false,out message)&&gold==960,"one time bundle payment");
        Require(tech.Owns(BlueprintFeature.IsDay)&&tech.Owns(BlueprintFeature.NightTime),"both time functions owned");
        Require(ForgeMvp.Evaluate("print(12(),24(),24(60),is_day(),night_time())",technology:tech).error==null,"all time spellings");
        Require(!tech.TryPurchase(luck,ref gold,ref iron,ref stocks,false,out message),"time alone cannot unlock luck");
        Require(tech.TryPurchase(ForgeTechnology.Find("material-6"),ref gold,ref iron,ref stocks,false,out message),message);
        Require(tech.TryPurchase(ForgeTechnology.Find("function-Charge"),ref gold,ref iron,ref stocks,false,out message),message);
        int before=gold;
        Require(tech.TryPurchase(luck,ref gold,ref iron,ref stocks,false,out message)&&gold==before-100,"one luck bundle payment");
        Require(tech.Owns(BlueprintFeature.Lucky)&&tech.Owns(BlueprintFeature.LuckyCode),"both luck functions owned");
        string blade="p=1(1)\n2(p,7)\n3(p)\n4(p,0)\n5(p,2)\n";
        foreach(string call in new[]{"777(778(),p)","lucky(lucky_code(),p)"})Require(ForgeMvp.Evaluate(blade+"p="+call+"\n!p",technology:tech).error==null,"luck bundle execution");
        before=gold;
        Require(!tech.TryPurchase(ForgeTechnology.Find("function-LuckyCode"),ref gold,ref iron,ref stocks,false,out message)&&gold==before,"companion cannot be purchased twice");
        foreach(var feature in new[]{BlueprintFeature.IsDay,BlueprintFeature.NightTime,BlueprintFeature.Lucky,BlueprintFeature.LuckyCode}){
            foreach(bool oldNodes in new[]{false,true}){
                var restored=new ForgeTechnology();
                restored.Restore(oldNodes?Array.Empty<BlueprintFeature>():new[]{feature},oldNodes?new[]{"function-"+feature}:null);
                var offer=ForgeTechnology.Find("function-"+feature);
                Require(offer.Features.All(restored.Owns),"partial legacy purchase grants full bundle");
                Require(restored.ExportNodes().Distinct().Count()==restored.ExportNodes().Length,"canonical save IDs");
                var again=new ForgeTechnology();again.Restore(restored.Export(),restored.ExportNodes());
                Require(again.ExportNodes().SequenceEqual(restored.ExportNodes()),"bundle roundtrip");
            }
        }
        Console.WriteLine("PASS bundles: shared purchase, both spellings, parent gates, duplicate rejection, partial-save migration and roundtrip");
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
