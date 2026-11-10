using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ForgeBlacksmithChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception("Blacksmith: "+message);}
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object host,string name)=>host.GetType().GetField(name,Flags).GetValue(host);
    static void Set(object host,string name,object value)=>host.GetType().GetField(name,Flags).SetValue(host,value);
    static object Call(object host,string name,params object[] args)=>host.GetType().GetMethod(name,Flags).Invoke(host,args);
    public static string Run()
    {
        var state=new ForgeBlacksmiths();state.Normalize(8);int diamonds=10000;
        Require(state.smiths.Count==1&&ForgeBlacksmiths.X(0)==0&&ForgeBlacksmiths.Y(0)==0,"initial smith origin");
        state.smiths[0].required=2;Require(state.Gift(0,2,ref diamonds)&&state.smiths[0].favor==5&&diamonds==9998,"correct gift");
        state.smiths[0].required=2;Require(state.Gift(0,3,ref diamonds)&&state.smiths[0].favor==-5&&diamonds==9995,"incorrect gift");
        int previous=diamonds;Require(!state.Gift(0,10,ref diamonds)&&diamonds==previous,"invalid gift atomic");
        for(int i=0;i<100;i++){int demand=state.smiths[0].required;state.Gift(0,demand,ref diamonds);Require(state.smiths[0].required>=1&&state.smiths[0].required<=9&&state.smiths[0].required!=demand,"fresh demand every gift");}
        state.smiths[0].favor=0;state.Tick(1,9.99);Require(state.smiths[0].favor==0,"fractional decay");
        state.Tick(2,32);Require(state.smiths[0].favor==-12,"24 hour decay");state.Tick(2,32);Require(state.smiths[0].favor==-12,"no duplicate decay");
        int burn=state.burningSmith;double until=state.burningUntil;state.Tick(2,33);Require(state.burningSmith==burn&&state.burningUntil==until,"daily event not rerolled");
        foreach(int favor in new[]{-100,0,100}){
            state.smiths[0].favor=favor;state.Chances(0,1000,out double destruction,out double great);
            Require(Math.Abs(destruction-(favor<0?.1:.01))<1e-8&&Math.Abs(great-(favor>0?.1:.01))<1e-8,"probability endpoints "+favor);
        }
        state.Chances(0,33,out _,out double fire);Require(fire==.5,"burning probability");
        state.smiths[0].favor=99;state.smiths[0].required=1;state.Gift(0,1,ref diamonds);Require(state.smiths[0].favor==100,"upper clamp");
        state.smiths[0].favor=-99;state.smiths[0].required=1;state.Gift(0,2,ref diamonds);Require(state.smiths[0].favor==-100,"lower clamp");
        for(int tier=0;tier<5;tier++){int before=diamonds;Require(state.Hire(ref diamonds,33)&&state.smiths.Count==ForgeBlacksmiths.Counts[tier+1]&&before-diamonds==ForgeBlacksmiths.HireCosts[tier],"hire tier "+tier);}
        Require(!state.Hire(ref diamonds,33),"max hire cap");
        state.runes.Add(new ForgeBlacksmiths.GiantRune{rune=1,x=0,y=0});state.runes.Add(new ForgeBlacksmiths.GiantRune{rune=7,x=0,y=1});
        Require(Math.Abs(state.RuneBonus(0,1)-.03)<1e-8&&state.RuneBonus(15,1)==0,"rune range and strongest only");
        var restored=JsonUtility.FromJson<ForgeBlacksmiths>(JsonUtility.ToJson(state));restored.Normalize(33);
        Require(restored.smiths.Count==16&&restored.smiths[0].favor==-100&&restored.burningUntil==state.burningUntil&&restored.runes.Count==2,"save roundtrip");
        foreach(var alias in ForgeSmithLanguage.Aliases.Where(a=>ForgeSmithLanguage.IsSmith(a.Id)))foreach(string spelling in new[]{alias.Symbol,alias.English})
            Require(ForgeSyntax.Scan(spelling+"()")[0].kind==ForgeSyntax.Kind.Blacksmith,"light blue "+spelling);
        var data=new ForgeItemData{handle=2,smithSaleMultiplier=3.25};var p=new ForgeProduct("test",0,true,ForgeTraits.None,0,1,1,data);
        Require(ForgePricing.Quote(p).SalePrice==(int)Math.Round(50*3.25),"great sale multiplier");
        CheckParallelRuntime();
        return "PASS: gifts, clamps, game-clock decay, daily event, probabilities, five hire tiers, rune range, save roundtrip, all aliases, sale multiplier, isolated parallel runtime";
    }
    static void CheckParallelRuntime()
    {
        var children=new List<BlueprintRuntime.Session>();
        var context=new ForgeRunContext{StartSmith=(id,create)=>{children.Add(create(new ForgeRunContext()));return id;}};
        string source="333 sword(n):\n    p=1(n)\n    2(p,7)\n    3(p)\n    4(p,0)\n    5(p,2)\n    << p\na=90904(1,sword,1)\nb=90904(2,sword,1)";
        using(var main=BlueprintRuntime.StartSession(source,1000,10,materialStocks:new Dictionary<int,long>{{2,10}},context:context)){
            Require(!main.Evaluation.compileError,main.Evaluation.error);
            for(int i=0;i<100&&!main.Completed;i++)main.Advance().GetAwaiter().GetResult();
            Require(main.Evaluation.error==null&&children.Count==2,"dispatch two workers "+main.Evaluation.error);
            Require(!children[0].Completed&&!children[1].Completed,"independent suspended workers");
            foreach(var child in children){for(int i=0;i<100&&!child.Completed;i++)child.Advance().GetAwaiter().GetResult();Require(child.Evaluation.error==null&&child.Evaluation.steps.Count(s=>s.finished)==1&&child.Evaluation.product.Kind==ForgeProductKind.Sword,"worker submits returned sword "+child.Evaluation.error);child.Dispose();}
        }
    }
    public static string PlayResult="not started";
    static ForgeMvp previewHost;
    static Dictionary<string,object> previewFields;
    static bool previewPaused;
    public static void BeginPreview()
    {
        if(previewHost)throw new Exception("Preview already active");
        previewHost=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();if(!previewHost)throw new Exception("No workshop");
        previewFields=new Dictionary<string,object>();
        foreach(var name in new[]{"showTitle","showTutorial","showProgress","showGameMenu","campaignComplete","showTechTree","showSmithTree","activeSaveSlot","blacksmiths","diamonds"})previewFields[name]=Get(previewHost,name);
        previewPaused=previewHost.DayCycle.Paused;previewHost.DayCycle.Paused=true;
        foreach(var name in new[]{"showTitle","showTutorial","showProgress","showGameMenu","campaignComplete"})Set(previewHost,name,false);
        Set(previewHost,"activeSaveSlot",0);Set(previewHost,"showTechTree",true);Set(previewHost,"showSmithTree",true);Set(previewHost,"diamonds",500);
        var state=new ForgeBlacksmiths{hireTier=2};state.Normalize((previewHost.DayCycle.Day-1)*24+previewHost.DayCycle.CurrentHour);Set(previewHost,"blacksmiths",state);
    }
    public static void EndPreview()
    {
        if(!previewHost)return;
        foreach(var pair in previewFields)Set(previewHost,pair.Key,pair.Value);
        previewHost.DayCycle.Paused=previewPaused;previewHost=null;previewFields=null;
    }
    public static ForgeRunContext GuideContext()
    {
        var state=new ForgeBlacksmiths{hireTier=1};state.Normalize(8);int diamonds=100;
        return new ForgeRunContext{
            SmithCall=(feature,args)=>{
                int id=args.Count>0?(int)args[0]:0;
                switch(feature){
                    case BlueprintFeature.SmithDemand:return state.smiths[id].required;
                    case BlueprintFeature.SmithFavor:return state.smiths[id].favor;
                    case BlueprintFeature.SmithPosition:return new[]{ForgeBlacksmiths.X(id),ForgeBlacksmiths.Y(id)};
                    case BlueprintFeature.SmithCount:return 2;
                    case BlueprintFeature.SmithSelf:return 0;
                    case BlueprintFeature.SmithBusy:return 0;
                    case BlueprintFeature.SmithGift:Require(state.Gift(id,(int)args[1],ref diamonds),"guide gift");return null;
                    case BlueprintFeature.GiantRune:return null;
                }
                throw new Exception("unsupported guide feature");
            },
            StartSmith=(id,create)=>{using(var child=create(new ForgeRunContext())){while(!child.Completed)child.Advance().GetAwaiter().GetResult();Require(child.Evaluation.error==null,child.Evaluation.error);}return id;}
        };
    }
    public static void StartPlayCheck()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        var root=new GameObject("Isolated blacksmith checks");root.transform.position=Vector3.right*1000;var runner=root.AddComponent<ForgeBlacksmithCheckRunner>();runner.StartCoroutine(PlayCheck(root));
    }
    static IEnumerator PlayCheck(GameObject root)
    {
        PlayResult="running";
        var host=root.AddComponent<ForgeMvp>();host.enabled=false;
        var cycle=root.AddComponent<ForgeDayCycle>();cycle.Paused=true;cycle.enabled=false;
        cycle.Clock.Restore(3,15.5);cycle.OnBeforeSerialize();cycle.OnAfterDeserialize();
        Require(cycle.Day==3&&cycle.CurrentHour==15.5&&cycle.Paused,"clock reload snapshot");cycle.Clock.Restore(1,8);
        typeof(ForgeMvp).GetProperty("DayCycle").SetValue(host,cycle);
        var work=GameObject.CreatePrimitive(PrimitiveType.Cube);work.transform.SetParent(root.transform);
        var motion=root.AddComponent<ForgeCraftMotion>();motion.Initialize(work);
        Set(host,"craftMotion",motion);Set(host,"work",work);Set(host,"showTitle",false);Set(host,"craftSpeed",5f);
        Set(host,"materialStocks",new[]{new ForgeMaterialStock{material=2,count=20,unlocked=true},new ForgeMaterialStock{material=6,count=10,unlocked=true}});
        Set(host,"gold",1000);Set(host,"iron",20);Set(host,"diamonds",1000);
        var state=(ForgeBlacksmiths)Get(host,"blacksmiths");state.hireTier=2;state.Normalize(8);state.burningDay=cycle.Day;state.burningUntil=0;
        Call(host,"UpdateSmiths");
        var tech=(ForgeTechnology)Get(host,"technology");tech.Restore(new[]{BlueprintFeature.Def,BlueprintFeature.Rune});
        var context=(ForgeRunContext)Call(host,"SmithContext",0);
        string code="333 sword():\n    p=1(1)\n    2(p,7)\n    3(p)\n    4(p,0)\n    5(p,2)\n    << p\na=90904(1,sword)\nb=90904(2,sword)\nc=90904(1,sword)";
        var main=BlueprintRuntime.StartSession(code,1000,20,materialStocks:new Dictionary<int,long>{{2,20}},technology:tech,context:context);
        while(!main.Completed){var task=main.Advance();while(!task.IsCompleted)yield return null;}
        if(main.Evaluation.error!=null){PlayResult="FAIL: "+main.Evaluation.error;main.Dispose();UnityEngine.Object.Destroy(root);yield break;}
        bool duplicate=Equals(main.Evaluation.values["c"],-1);
        main.Dispose();
        float deadline=Time.realtimeSinceStartup+10;
        while(((IDictionary)Get(host,"smithJobs")).Count>0&&Time.realtimeSinceStartup<deadline)yield return null;
        try{
            Require(duplicate,"busy target rejected");Require(((IDictionary)Get(host,"smithJobs")).Count==0,"parallel jobs complete");
            var stock=((ForgeMaterialStock[])Get(host,"materialStocks"))[0];
            // Force normal rolls in a separate atomicity check; production rolls may refund materials.
            Require((int)Get(host,"iron")>=18&&stock.count>=18,"shared inventory nonnegative");
            Set(host,"iron",1);stock.count=0;
            var step=new ForgeMvp.Step{iron=1,consumed=new Dictionary<int,int>{{2,1}}};
            Require(!(bool)Call(host,"SpendSmithStep",step,new ForgeProductionLedger())&&(int)Get(host,"iron")==1,"failed multi-material transaction consumes nothing");
            Set(host,"iron",1);stock.count=1;
            Require((bool)Call(host,"SpendSmithStep",step,new ForgeProductionLedger())&&!(bool)Call(host,"SpendSmithStep",step,new ForgeProductionLedger())&&(int)Get(host,"iron")==0&&stock.count==0,"last material consumed once");
            context.SmithCall(BlueprintFeature.GiantRune,new List<object>{7,0,0});Require(state.runes.Count==1&&(int)Get(host,"diamonds")==925,"rune purchase");
            Require(((string)Call(host,"SmithInfo",0)).Contains("x: 0   y: 0"),"initial tooltip origin");
            // Search deterministic state for the great-success branch, then verify actual host refund and double output.
            uint seed=1;for(;seed<100000;seed++){var probe=new ForgeBlacksmiths{randomState=seed};double roll=probe.Next(1000000)/1000000d;if(roll>=.01&&roll<.5)break;}
            state.randomState=seed;state.burningSmith=0;state.burningUntil=1000;
            int before=((ForgeShop)Get(host,"shop")).Items.Count;int beforeIron=(int)Get(host,"iron");
            var data=new ForgeItemData{handle=2};data.materials[1]=1;data.materials[2]=1;
            var result=(ForgeProduct)Call(host,"FinishSmithProduct",0,new ForgeProduct("check",0,true,ForgeTraits.None,0,1,1,data));
            Require(((ForgeShop)Get(host,"shop")).Items.Count==before+2&&(int)Get(host,"iron")==beforeIron+1&&result.Data.smithSaleMultiplier>=1.25&&result.Data.smithSaleMultiplier<=3.25,"great success actual refund and double output");
            PlayResult="PASS: two concurrent animated workers, busy rejection, atomic shared inventory, rune cost, tooltip origin, great success refunds and two products";
        }catch(Exception error){PlayResult="FAIL: "+error;}
        finally{Call(host,"StopSmithJobs");UnityEngine.Object.Destroy(root);}
    }
}
public sealed class ForgeBlacksmithCheckRunner:MonoBehaviour {}
