using System;
using System.Collections.Generic;
using System.Linq;
public static class BlueprintMaterialChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main(){
        foreach(var material in ForgeMaterialGuide.Entries){
            string code="Item = 1("+material.Number+")\nprint(Item)\n!Item";
            var stock=new Dictionary<int,long>{{material.Number,2}};
            var r=ForgeMvp.Evaluate(code,100,10,materialStocks:stock);
            Require(r.error==null&&r.steps.Count==3&&ForgePricing.Quote(r.product).SalePrice==0,"material "+material.Number+": "+r.error);
            Require(r.steps[0].material==material.Number&&r.cost==10&&r.ironConsumed==(material.Number==1?1:0),"receipt");
            Require(((ForgeMvp.Piece)r.values["Item"]).material==material.Number,"identity");
            Require(stock[material.Number]==2,"preview mutates inventory");
        }
        foreach(string value in new[]{"10","999","True","'wood'","None"}){
            var r=ForgeMvp.Evaluate("Item = 1("+value+")\n!Item");Require(r.compileError&&r.steps.Count==0,"compile rejection "+value);
        }
        var bad=ForgeMvp.Evaluate("m = 99\nItem = 1(m)\n!Item");Require(!bad.compileError&&bad.errorLine==2&&bad.steps.Count==0,"dynamic material validation");
        var empty=ForgeMvp.Evaluate("Item = 1(2)\n!Item",materialStocks:new Dictionary<int,long>());Require(!empty.compileError&&empty.steps.Count==0&&empty.cost==0,"stock validation");
        var repeated=ForgeMvp.Evaluate("A = 1(2)\nB = 1(2)\n!B",materialStocks:new Dictionary<int,long>{{2,1}});Require(repeated.steps.Count==1&&repeated.cost==10,"stock consumed in simulation");
        Console.WriteLine("PASS all 10 materials, static/dynamic validation, identity, inventory isolation and depletion");
        BlueprintGuideChecks.Main();
    }
}
