using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
public static class BlueprintFreeForgeChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static ForgeMvp.Evaluation Good(string source,int iron=10)
    {var r=ForgeMvp.Evaluate(source,100,iron);Require(r.error==null,r.error);return r;}
    public static string Run()
    {
        int count=0;var log=new StringBuilder();
        Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("raw iron submission consumes one iron and pays zero",()=>{
            var r=Good("res = 1\n!res");Require(r.cost==0&&r.ironConsumed==1&&r.product.Material==1&&ForgePricing.Quote(r.product).SalePrice==0,"raw receipt");
            var shop=new ForgeShop();int gold=100,id=shop.Add(r.product);Require(shop.TrySell(id,ref gold,out var sold)&&gold==100&&!shop.TrySell(id,ref gold,out sold),"zero sale once");
        });
        check("raw submission cannot create inventory",()=>{var r=ForgeMvp.Evaluate("!1",100,0);Require(r.error!=null&&r.product==null&&r.steps.Count==0,"stock");});
        check("standalone load and complete process sequence need no submission",()=>{
            var load=Good("1(1)");Require(load.steps.Count==1&&load.cost==0&&load.product==null,"standalone");
            var r=Good("p = 1(1)\n2(p)\n3(p)\n4(p, 0)");Require(r.product==null&&r.cost==0&&r.steps.Select(x=>x.process).SequenceEqual(new[]{1,2,3,4}),"order");
        });
        check("calculation only and empty programs succeed",()=>{Require(Good("x = 2 + 3").values["x"].Equals(5)&&Good("").skipped,"compute");});
        check("cold strike acts without changing shape; later hot strike succeeds",()=>{
            var r=Good("p = 1(1)\n3(p)\n4(p, 0)\n2(p)\n3(p)\n4(p, 0)\n!p");Require(!r.steps[1].shaped&&r.steps[4].shaped&&ForgePricing.Quote(r.product).SalePrice==28,"cold and hot");
        });
        check("nested calls execute inner first and preserve snapshots",()=>{
            var r=Good("2(1(1))");Require(r.steps.Select(x=>x.process).SequenceEqual(new[]{1,2})&&r.steps[0].heat==0&&r.steps[1].heat==900&&r.steps[0].pieceId==r.steps[1].pieceId,"nested order");
        });
        check("effectful return and condition allowed; short circuit skips actions",()=>{
            var r=Good("def make():\n    return 1(1)\ndef warm(p):\n    2(p)\n    return True\np = make()\nif False and warm(p):\n    3(p)\nif warm(p):\n    3(p)\n4(p, 0)\n!p");Require(r.cost==0&&r.steps.Count(x=>x.process==2)==1&&r.product.Shaped,"effectful calls");
        });
        check("void calls return None without suppressing actions",()=>{var r=Good("p = 1(1)\nx = 2(p)\ny = 3(p)\nz = 4(p, 0)");Require(r.values["x"]==null&&r.values["y"]==null&&r.values["z"]==null&&r.steps.Count==4,"none");});
        check("multiple workpieces retain animation identity",()=>{var r=Good("a = 1(1)\nb = 1(1)\n2(a)\n3(a)\n4(b, 0)\n!b");Require(r.steps[0].pieceId!=r.steps[1].pieceId&&r.steps[2].pieceId==r.steps[0].pieceId&&r.steps[4].pieceId==r.steps[1].pieceId&&!r.product.Shaped,"identity");});
        check("failed calls leave only successful animation receipts",()=>{var r=ForgeMvp.Evaluate("p = 1(1)\n8(99)");Require(!r.compileError&&r.errorLine==2&&r.steps.Count==1&&r.cost==0,"failed receipt");});
        check("arity types cooling and invalid submissions remain errors",()=>{foreach(var code in new[]{"1()","2()","3()","4()","2(1, 7)","!True","!99","p = 1(1)\n4(p, 2)"})Require(ForgeMvp.Evaluate(code).error!=null,code);});
        check("preview receipts reproduce final material and pricing",()=>{
            foreach(string source in new[]{"!1","p = 1(2)\n!p",ForgeEconomyChecks.Basic}){
                var r=Good(source);int gold=100,iron=10;var ledger=new ForgeProductionLedger();ForgeProduct actual=null;
                foreach(var step in r.steps){Require(ledger.TrySpend(step.cost,step.iron,ref gold,ref iron),"spend");if(step.finished)actual=ledger.Complete("actual",step.heat,step.shaped,step.traits,step.material);}
                Require(actual.Material==r.product.Material&&actual.CashCost==r.cost&&ForgePricing.Quote(actual).SalePrice==ForgePricing.Quote(r.product).SalePrice,"parity");
            }
        });
        return log.AppendLine("TOTAL free forge "+count+" passed").ToString();
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Free Forge")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
