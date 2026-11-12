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
        CheckLivePurchases();
        CheckCoalBundle();
        CheckBulkPurchase();
        CheckBundles();
        CheckSpeedLevels();
        CheckResearch();
        CheckControlRoot();
        var tech=new ForgeTechnology();int gold=100000000,iron=0;ForgeMaterialStock[] stocks=null;string message;
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
        Require(tech.BlockReason(heat,gold,iron,stocks,true)==null,"research available while crafting");
        gold=heat.Price-1;
        Require(!tech.TryPurchase(heat,ref gold,ref iron,ref stocks,false,out message)&&gold==heat.Price-1,"insufficient funds");
        gold=heat.Price;
        Require(tech.TryPurchase(heat,ref gold,ref iron,ref stocks,true,out message)&&gold==0,"exact funds");
        gold=100000000;
        Require(!tech.TryPurchase(heat,ref gold,ref iron,ref stocks,false,out message)&&gold==100000000,"duplicate");
        Require(ForgeMvp.Evaluate("p=1(1)\nprint(heat(p),100(p))",technology:tech).error==null,"both spellings");
        var fuel=ForgeTechnology.Find("function-Fuel");
        Require(tech.TryPurchase(fuel,ref gold,ref iron,ref stocks,false,out message)&&tech.MaterialAvailable(4)&&tech.Owns(BlueprintFeature.Fuel),"coal and furnace unlock together");
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
    static void CheckLivePurchases()
    {
        var tech=new ForgeTechnology();tech.Restore(Array.Empty<BlueprintFeature>(),new[]{ForgeTechnology.AutomationRoot});
        int gold=100000,iron=0;ForgeMaterialStock[] stocks=null;
        var context=new ForgeRunContext{RefreshResources=session=>session.SetResources(gold,iron,new System.Collections.Generic.Dictionary<int,long>())};
        using(var session=BlueprintRuntime.StartSession("print(0)\np=1(1)\nprint(1)",gold,iron,technology:tech,deferErrors:true,context:context)){
            session.Advance().GetAwaiter().GetResult();
            Require(tech.TryPurchase(ForgeTechnology.Find("stock-1"),ref gold,ref iron,ref stocks,true,out var message,2),message);
            int afterBuy=gold;session.Advance().GetAwaiter().GetResult();
            Require(session.Evaluation.error==null&&session.Evaluation.steps.Last().iron==1,"live purchased iron usable on next statement");iron--;
            Require(tech.TryPurchase(ForgeTechnology.Find("function-Heat"),ref gold,ref iron,ref stocks,true,out message),message);
            session.Advance().GetAwaiter().GetResult();
            Require(session.Completed&&session.Evaluation.error==null&&tech.Owns(BlueprintFeature.Heat)&&iron==1&&gold==afterBuy-ForgeTechnology.Find("function-Heat").Price,"research purchase preserves running work and balances: "+session.Evaluation.error+" completed="+session.Completed+" iron="+iron+" gold="+gold);
            int before=gold;Require(!tech.TryPurchase(ForgeTechnology.Find("function-Heat"),ref gold,ref iron,ref stocks,true,out message)&&gold==before,"duplicate remains atomic during production");
        }
        Console.WriteLine("PASS live purchases: material refill, research during execution, balances, duplicate prevention");
    }
    static void CheckCoalBundle()
    {
        var bundle=ForgeTechnology.Find("function-Fuel");
        Require(bundle==ForgeTechnology.Find("material-4")&&bundle.Price==2400,"single coal bundle identity and price");
        Require(!ForgeTechnology.Offers.Any(o=>o.Id=="material-4"),"no separate coal research sale");
        var tech=new ForgeTechnology();int gold=2400,iron=0;ForgeMaterialStock[] stocks=null;
        Require(!tech.TryPurchase(bundle,ref gold,ref iron,ref stocks,false,out _)&&gold==2400&&stocks==null,"locked bundle atomic");
        tech.Restore(Array.Empty<BlueprintFeature>(),new[]{ForgeTechnology.AutomationRoot});gold=2399;
        Require(!tech.TryPurchase(bundle,ref gold,ref iron,ref stocks,false,out _)&&!tech.MaterialAvailable(4)&&!tech.Owns(BlueprintFeature.Fuel)&&stocks==null,"insufficient bundle funds atomic");
        gold=2400;Require(tech.TryPurchase(bundle,ref gold,ref iron,ref stocks,false,out _)&&gold==0,"single bundle charge");
        Require(tech.MaterialAvailable(4)&&tech.Owns(BlueprintFeature.Fuel)&&stocks.Single(s=>s.material==4).unlocked&&stocks.Single(s=>s.material==4).count==0,"both unlock without granting stock");
        gold=40;Require(tech.TryPurchase(ForgeTechnology.Find("stock-4"),ref gold,ref iron,ref stocks,false,out _,2)&&gold==0&&stocks.Single(s=>s.material==4).count==2,"coal remains repeatable stock purchase");
        gold=5000;Require(!tech.TryPurchase(bundle,ref gold,ref iron,ref stocks,false,out _)&&gold==5000,"no double purchase");
        foreach(var ids in new[]{new[]{"material-4"},new[]{"function-Fuel"},new[]{"material-4","function-Fuel"}}){
            var old=new ForgeTechnology();old.Restore(Array.Empty<BlueprintFeature>(),ids);
            Require(old.MaterialAvailable(4)&&old.Owns(BlueprintFeature.Fuel)&&old.ExportNodes().Count(id=>id=="function-Fuel")==1&&!old.ExportNodes().Contains("material-4"),"legacy bundle migration");
            var restored=new ForgeTechnology();restored.Restore(old.Export(),old.ExportNodes());Require(restored.MaterialAvailable(4)&&restored.Owns(BlueprintFeature.Fuel),"bundle save roundtrip");
        }
        var fuelGuide=BlueprintGuide.Entries.Single(e=>e.Feature==BlueprintFeature.Fuel);
        Require(!fuelGuide.ShowResearch&&fuelGuide.ResearchFor(false)==""&&fuelGuide.ResearchFor(true)=="","fuel research prose removed in both variants");
        Console.WriteLine("PASS coal bundle: one purchase, both unlocks, atomicity, stock purchase, migration, guide suppression");
    }
    static void CheckControlRoot()
    {
        var tech=new ForgeTechnology();int gold=100,iron=10;ForgeMaterialStock[] stocks=null;
        var root=ForgeTechnology.Find(ForgeTechnology.AutomationRoot);
        Require(!tech.TryPurchase(root,ref gold,ref iron,ref stocks,false,out var message)&&gold==100,"initial funds require manual crafting first");
        Require(ForgeMvp.Evaluate("p=1(1)\n2(p)\n3(p)\n4(p,0)\n5(p,2)\n!p",technology:tech).error==null,"basic five processes remain free");
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
                if(session.Completed){Require(session.Evaluation.compileError&&session.Evaluation.steps.Count==0,"English notation blocked before execution");continue;}
                session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.error==null,"valid prefix allowed");
                session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.error!=null&&session.Evaluation.steps.Count==1,"stepped control blocked at execution");
            }
        }
        var modules=new System.Collections.Generic.Dictionary<string,string>{{"logic","def f():\n    return 1"}};
        Require(ForgeMvp.Evaluate("import logic\nprint(logic.f())",technology:tech,modules:modules).error!=null,"imports cannot bypass control lock");
        Require(ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material&&o.Id!=root.Id).All(o=>o.Parents.Count>0),"all branches descend from common root");
        gold=root.Price;Require(tech.TryPurchase(root,ref gold,ref iron,ref stocks,false,out message)&&gold==0,message);
        Require(ForgeTechnology.ControlFeatures.All(tech.Owns),"all nine control forms unlocked");
        Require(!tech.Owns(ForgeTechnology.EnglishResearch),"Control root must not grant English");
        gold=1000000;
        Require(!tech.TryPurchase(ForgeTechnology.Find(ForgeTechnology.EnglishResearch),ref gold,ref iron,ref stocks,true,out message),"English requires Max");
        foreach(var id in new[]{"function-Abs","function-Min"})Require(tech.TryPurchase(ForgeTechnology.Find(id),ref gold,ref iron,ref stocks,true,out message),message);
        Require(ForgeTechnology.Find(ForgeTechnology.EnglishResearch).Parents.SequenceEqual(new[]{"function-Min"}),"English follows Max");
        Require(tech.TryPurchase(ForgeTechnology.Find(ForgeTechnology.EnglishResearch),ref gold,ref iron,ref stocks,false,out message),message);
        Require(tech.TryPurchase(ForgeTechnology.Find(ForgeTechnology.ImportResearch),ref gold,ref iron,ref stocks,false,out message),message);
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
        var tech=new ForgeTechnology();int gold=100000000,iron=0;ForgeMaterialStock[] stocks=null;
        Require(tech.TryPurchase(ForgeTechnology.Find(ForgeTechnology.AutomationRoot),ref gold,ref iron,ref stocks,false,out var rootMessage),rootMessage);
        foreach(var path in new[]{new[]{BlueprintFeature.Abs,BlueprintFeature.Min,BlueprintFeature.Max},new[]{BlueprintFeature.Len,BlueprintFeature.Set,BlueprintFeature.Random}}){
            var last=ForgeTechnology.Find("function-"+path[2]);int before=gold;
            Require(!tech.TryPurchase(last,ref gold,ref iron,ref stocks,false,out var reason)&&gold==before,"research cannot skip prerequisites");
            foreach(var feature in path){
                if(tech.Owns(feature))continue;
                var offer=ForgeTechnology.Find("function-"+feature);
                Require(offer.Lane>=10&&offer.Parents.All(id=>id==ForgeTechnology.AutomationRoot||ForgeTechnology.Find(id).Lane>=10),"research prerequisites stay in research after common root");
                Require(tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out var message),message);
            }
        }
        Require(gold==100000000-ForgeTechnology.Find(ForgeTechnology.AutomationRoot).Price-new[]{BlueprintFeature.Abs,BlueprintFeature.Min,BlueprintFeature.Max,BlueprintFeature.Len,BlueprintFeature.Set,BlueprintFeature.Random}.Select(f=>ForgeTechnology.Find("function-"+f)).Distinct().Sum(o=>o.Price)&&!tech.Owns(BlueprintFeature.Heat)&&stocks==null,"research independent of crafting, cost accounting");
        Require(ForgeMvp.Evaluate("print(101(-3),000([1,2]),999([1,2]),1111([1]),1010(),66(1,2))",technology:tech).error==null,"research functions execute");
        var restored=new ForgeTechnology();restored.Restore(tech.Export(),tech.ExportNodes());
        Require(restored.ExportNodes().SequenceEqual(tech.ExportNodes()),"research save compatibility");
        foreach(var feature in new[]{BlueprintFeature.Heat,BlueprintFeature.Polish,BlueprintFeature.Charge,BlueprintFeature.Speed,BlueprintFeature.IsDay,BlueprintFeature.Lucky})
            Require(ForgeTechnology.Find("function-"+feature).Lane<10,"crafting and operations remain outside research");
        Console.WriteLine("PASS research: independent branches, prerequisites, existing prices, execution and save roundtrip");
    }
    static void CheckSpeedLevels()
    {
        var tech=new ForgeTechnology();int gold=100000000,iron=0;ForgeMaterialStock[] stocks=null;
        Require(tech.MaxSpeedLevel==0&&!tech.Owns(BlueprintFeature.AutoSell),"new game has no speed purchases");
        tech.Restore(new[]{BlueprintFeature.Wrap});
        for(int level=1;level<=5;level++){
            var offer=ForgeTechnology.Find("speed-"+level);
            if(level<5){int before=gold;Require(!tech.TryPurchase(ForgeTechnology.Find("speed-"+(level+1)),ref gold,ref iron,ref stocks,false,out var blocked)&&gold==before,"cannot skip speed level");}
            int balance=gold;gold=offer.Price-1;
            Require(!tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out var error)&&tech.MaxSpeedLevel==level-1,"unaffordable speed atomic");gold=balance;
            Require(tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out var message)&&gold==balance-offer.Price,message);
            Require(tech.MaxSpeedLevel==level&&!tech.Owns(BlueprintFeature.AutoSell),"speed does not grant auto sell");
            float multiplier=level==1?1.5f:level;
            var timed=ForgeMvp.Evaluate("222("+level+")\np=1(1)",technology:tech);
            Require(timed.error==null&&Math.Abs(timed.steps.Last().duration-.8f/multiplier)<.0001f,"stage duration matches actual multiplier");
            Require(tech.RestoreSpeed(1)==1.5f&&tech.RestoreSpeed(multiplier)==multiplier&&tech.RestoreSpeed(5)==multiplier,"saved speed migration and cap");
            using(var speedSession=BlueprintRuntime.StartSession("222(1)\np=1(1)",0,1,technology:tech)){
                speedSession.Advance().GetAwaiter().GetResult();Require(speedSession.WorkSpeed==1.5f,"first stage hover reports 1.5x");
            }
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
            Require(restored.MaxSpeedLevel==level&&!restored.Owns(BlueprintFeature.AutoSell),"partial speed save must not unlock higher levels");
            Require(ForgeMvp.Evaluate("225(1)",technology:tech).error!=null,"auto sell requires separate purchase");
            balance=gold;Require(!tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out message)&&gold==balance,"speed duplicate atomic");
        }
        var auto=ForgeTechnology.Find("function-AutoSell");
        Require(!BlueprintResearchGuide.FeatureVisible(BlueprintFeature.AutoSell,tech,new ForgeBlacksmiths()),"guide stays locked at speed five");
        int beforeAuto=gold;Require(tech.TryPurchase(auto,ref gold,ref iron,ref stocks,false,out var autoMessage)&&gold==beforeAuto-auto.Price,"separate auto sell charge");
        Require(tech.Owns(BlueprintFeature.AutoSell)&&ForgeMvp.Evaluate("225(1)",technology:tech).error==null,"purchased auto sell works");
        Require(BlueprintResearchGuide.FeatureVisible(BlueprintFeature.AutoSell,tech,new ForgeBlacksmiths()),"guide unlocked after purchase");
        var oldReward=new ForgeTechnology();oldReward.Restore(new[]{BlueprintFeature.Speed,BlueprintFeature.AutoSell},new[]{"speed-5"});Require(oldReward.Owns(BlueprintFeature.AutoSell),"old bundled reward preserved");
        foreach(string id in new[]{"function-Speed","function-AutoSell"}){
            var old=new ForgeTechnology();old.Restore(Array.Empty<BlueprintFeature>(),new[]{id});
            Require(old.MaxSpeedLevel==5&&old.Owns(BlueprintFeature.AutoSell),"legacy speed access preserved");
        }
        var legacy=new ForgeTechnology();legacy.Restore(new[]{BlueprintFeature.Speed});
        Require(legacy.MaxSpeedLevel==5,"feature-only legacy speed preserved");
        Console.WriteLine("PASS speed levels: sequential/atomic purchases, cap, imports, stepping, separate auto sell purchase, guide gating, save roundtrips and migration");
    }
    static void CheckBundles()
    {
        var tech=new ForgeTechnology();int gold=100000000,iron=0;ForgeMaterialStock[] stocks=null;
        var time=ForgeTechnology.Find("function-IsDay");var luck=ForgeTechnology.Find("function-Lucky");
        Require(time==ForgeTechnology.Find("function-NightTime")&&luck==ForgeTechnology.Find("function-LuckyCode"),"one node per bundle");
        Require(!tech.TryPurchase(luck,ref gold,ref iron,ref stocks,false,out var message)&&gold==100000000,"luck prerequisites atomic");
        Require(tech.TryPurchase(ForgeTechnology.Find(ForgeTechnology.AutomationRoot),ref gold,ref iron,ref stocks,false,out message),message);
        int before=gold;
        Require(tech.TryPurchase(luck,ref gold,ref iron,ref stocks,false,out message)&&gold==before-luck.Price,"luck purchasable immediately after basic control bundle");
        Require(!tech.Owns(BlueprintFeature.IsDay)&&!tech.Owns(BlueprintFeature.Charge)&&!tech.MaterialAvailable(6),"luck independent of time and magic purchases");
        before=gold;
        Require(tech.TryPurchase(time,ref gold,ref iron,ref stocks,false,out message)&&gold==before-time.Price,"one time bundle payment");
        Require(tech.Owns(BlueprintFeature.IsDay)&&tech.Owns(BlueprintFeature.NightTime),"both time functions owned");
        Require(ForgeMvp.Evaluate("print(12(),24(),24(60),is_day(),night_time())",technology:tech).error==null,"all time spellings");
        Require(tech.Owns(BlueprintFeature.Lucky)&&tech.Owns(BlueprintFeature.LuckyCode),"both luck functions owned");
        string blade="p=1(1)\n2(p)\n3(p)\n4(p,0)\n5(p,2)\n";
        foreach(string call in new[]{"777(777?(),p)","lucky(lucky_code(),p)"})Require(ForgeMvp.Evaluate(blade+"p="+call+"\n!p",technology:tech).error==null,"luck bundle execution");
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
