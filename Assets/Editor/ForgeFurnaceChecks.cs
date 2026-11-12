using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class ForgeFurnaceChecks
{
    const string Measured="p=1(1)\n2(p)\nt=100(p,1)\n";
    const string Perfect=Measured+"8(t)\n2(p)\n3(p)\n4(p,3)\n!p";
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static ForgeMvp.Evaluation Good(string code,uint seed=1,Dictionary<int,long> stock=null)
    {
        var result=BlueprintRuntime.Evaluate(code,0,20,"furnace test",seed,stock);
        Require(result.error==null,result.error);return result;
    }
    public static string Run()
    {
        var log=new StringBuilder();int count=0;
        Action<string,Action> test=(name,action)=>{action();count++;log.AppendLine("PASS "+name);};
        test("furnace setting does not heat a piece; persists within one execution",()=>{
            var e=Good("p=1(1)\n8(1000)\ncold=100(p)\n2(p)\nhot=100(p)\nq=1(1)\n2(q)\nother=100(q)");
            Require((int)e.values["cold"]==0&&(int)e.values["hot"]==1000&&(int)e.values["other"]==1000,"furnace transfer");
            Require(e.steps.Single(s=>s.process==8).pieceId==0&&e.steps.Single(s=>s.process==8).consumed[4]==1,"furnace receipt without phantom sword");
            Require((int)Good("p=1(1)\n2(p)\nh=100(p)").values["h"]==900,"fresh execution furnace");
        });
        test("ideal is hidden before heating, fixed per piece and varied across seeds",()=>{
            Require(BlueprintRuntime.Evaluate("p=1(1)\nt=100(p,1)",0,1,"",1).errorLine==2,"cold measurement gate");
            var targets=new HashSet<int>();
            for(uint seed=1;seed<=300;seed++){
                var e=Good(Measured+"8(1200)\n2(p)\nt2=100(p,1)\n4(p,0)\nt3=100(p,1)",seed);
                int t=(int)e.values["t"];targets.Add(t);
                Require(t>=850&&t<=1100&&t==(int)e.values["t2"]&&t==(int)e.values["t3"],"stable inclusive ideal range");
            }
            Require(targets.Contains(850)&&targets.Contains(1100)&&targets.Count>200,"random range coverage");
            var pair=Good(Measured+"q=1(1)\n2(q)\nu=100(q,1)");Require((int)pair.values["t"]!=(int)pair.values["u"],"piece identity");
        });
        test("exact temperature yields 128G; 1 degree miss yields 47G",()=>{
            var exact=Good(Perfect,1,new Dictionary<int,long>{{3,1},{4,1}});
            Require(exact.product.Data.perfectQuench&&ForgePricing.Quote(exact.product).SalePrice==128,"perfect price");
            Require(exact.product.Data.materials[1]==1&&exact.product.Data.materials[3]==1&&exact.product.Data.materials[4]==1,"stock accounting");
            var near=Good(Perfect.Replace("8(t)","8(t+1)"));Require(!near.product.Data.perfectQuench&&ForgePricing.Quote(near.product).SalePrice==47,"no exact reward for near miss");
            Require(exact.product.Data.Clone().idealTemperature==(int)exact.values["t"]&&exact.product.Data.Clone().perfectQuench,"completion clone");
        });
        test("requires hammer after latest heating and water; reheating clears reward",()=>{
            foreach(string code in new[]{Perfect.Replace("3(p)\n",""),Perfect.Replace("3(p)\n4","3(p)\n2(p)\n4"),Perfect.Replace("4(p,3)","4(p,0)")}){
                var e=Good(code);Require(!e.product.Data.perfectQuench&&e.product.Data.hardness==0,"quench order");
            }
            var e2=Good(Perfect.Replace("!p","2(p)\n4(p,3)\n!p"));Require(e2.product.Data.hardness==0&&!e2.product.Data.perfectQuench,"reheat reset");
            var repeated=Good(Perfect.Replace("!p","2(p)\n3(p)\n4(p,3)\n!p"));Require(ForgePricing.Quote(repeated.product).SalePrice==128,"no stacking");
            var low=Good("8(849)\np=1(1)\n2(p)\n3(p)\n4(p,3)\n!p");Require(!low.product.Shaped&&ForgePricing.Quote(low.product).SalePrice==0,"minimum forge temperature");
        });
        test("failed furnace changes consume nothing and retain previous state",()=>{
            var missing=BlueprintRuntime.Evaluate("p=1(1)\n2(p)\n8(1100)",0,1,"",1,new Dictionary<int,long>());
            Require(missing.errorLine==3&&missing.steps.Count==2&&((ForgeMvp.Piece)missing.values["p"]).heat==900,"missing coal atomic");
            foreach(string value in new[]{"99","1201","True","'hot'"}){
                var e=BlueprintRuntime.Evaluate("8("+value+")",0,1,"",1);
                Require(e.error!=null&&e.steps.Count==0,"invalid furnace input "+value);
            }
            foreach(string call in new[]{"2(p,7)","8(p,7)","100()","100(p,0)","100(p,1,2)"})Require(BlueprintRuntime.Evaluate("p=1(1)\n"+call,0,1,"",1).error!=null,"invalid signature "+call);
            Require(BlueprintRuntime.Evaluate("p=1(2)\n2(p)\nt=100(p,1)",0,1,"",1).error!=null,"nonblade measurement");
        });
        test("single stepping matches continuous results and receipts",()=>{
            var all=Good(Perfect);
            using(var session=BlueprintRuntime.StartSession(Perfect,0,20,seed:1)){
                while(!session.Completed)session.Advance().GetAwaiter().GetResult();
                Require(session.Evaluation.error==null,session.Evaluation.error);
                Require(session.Evaluation.product.Data.idealTemperature==all.product.Data.idealTemperature&&ForgePricing.Quote(session.Evaluation.product).SalePrice==128,"stepped price");
                Require(session.Evaluation.steps.Select(x=>x.process).SequenceEqual(all.steps.Select(x=>x.process)),"stepped receipts");
            }
        });
        test("numeric and English temperature commands agree; guide hides research",()=>{
            var english=Good(Perfect.Replace("100(","heat(").Replace("8(t)","fuel(t)"));Require(ForgePricing.Quote(english.product).SalePrice==128,"English aliases");
            var entry=BlueprintGuide.Entries.Single(e=>e.Feature==BlueprintFeature.Fuel);
            Require(!entry.ShowResearch,"research removed");
            foreach(bool mode in new[]{false,true})Require(ForgePricing.Quote(Good(entry.ExampleFor(mode)).product).SalePrice==128,"guide example");
        });
        test("parallel smith furnaces and parent furnace are isolated",()=>{
            var children=new List<BlueprintRuntime.Session>();
            var context=new ForgeRunContext{StartSmith=(id,create)=>{children.Add(create(new ForgeRunContext()));return id;}};
            const string code="333 sword(temperature):\n    8(temperature)\n    p=1(1)\n    2(p)\n    << p\n8(1200)\na=90904(1,sword,850)\nb=90904(2,sword,1100)\np=1(1)\n2(p)\nh=100(p)";
            using(var parent=BlueprintRuntime.StartSession(code,0,10,context:context)){
                while(!parent.Completed)parent.Advance().GetAwaiter().GetResult();
                Require(parent.Evaluation.error==null&&children.Count==2,"dispatch");
                while(children.Any(c=>!c.Completed))foreach(var child in children)if(!child.Completed)child.Advance().GetAwaiter().GetResult();
                Require((int)parent.Evaluation.values["h"]==1200&&children[0].Evaluation.product.FinalHeat==850&&children[1].Evaluation.product.FinalHeat==1100,"independent furnace temperatures");
                foreach(var child in children)child.Dispose();
            }
        });
        test("furnace animation duration follows production speed",()=>{
            foreach(float speed in new[]{1f,2f,5f}){
                var e=BlueprintRuntime.Evaluate("8(1000)\np=1(1)\n!p",0,1,"",1,context:new ForgeRunContext{Speed=speed});
                Require(e.error==null,e.error);
                var receipt=e.steps.Single(step=>step.process==8);
                Require(Math.Abs(receipt.duration-2.4f/speed)<.0001f,"scaled furnace duration");
                Require(Math.Abs(e.product.ProductionSeconds-3.2f/speed)<.0001f,"animation and production time agree");
            }
        });
        return log+"TOTAL furnace checks "+count;
    }
}
