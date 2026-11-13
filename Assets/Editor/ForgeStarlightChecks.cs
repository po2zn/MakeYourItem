using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
public static class ForgeStarlightChecks {
 static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Near(double a,double b,string message){Require(Math.Abs(a-b)<.00001,message+": "+a+" != "+b);}
 static ForgeMvp.Piece Piece(params int[] initial)=>new ForgeMvp.Piece{id=1,material=7,data=new ForgeItemData{starlight=new ForgeStarlight(initial)}};
 static ForgeMvp.Evaluation Eval(ForgeMvp.Piece p,string code,float speed=1)=>BlueprintRuntime.Evaluate(code,0,0,"test",12,context:new ForgeRunContext{PracticePiece=p,Speed=speed});
 static void Ok(ForgeMvp.Evaluation e)=>Require(e.error==null,e.error);
 static void Drain(BlueprintRuntime.Session s){int n=0;while(!s.Completed){Require(++n<10000,"test step bound");s.Advance().GetAwaiter().GetResult();}}
 public const string RefineCode="i = 0\n888(i < 70(StarIron)):\n    1?(71(StarIron, i) == 0):\n        73(StarIron, i)\n    0?:\n        i = i + 1\ni = 0\n888(i < 70(StarIron)):\n    j = i + 1\n    888(j < 70(StarIron)):\n        1?(71(StarIron, i) > 71(StarIron, j)):\n            72(StarIron, i, j)\n        j = j + 1\n    i = i + 1\ni = 1\n888(i < 70(StarIron)):\n    1?(71(StarIron, i) == 71(StarIron, i - 1)):\n        73(StarIron, i)\n    0?:\n        i = i + 1";
 public static string Run(){int count=0;var log=new StringBuilder();Action<string,Action> test=(name,body)=>{body();count++;log.AppendLine("PASS "+name);};
 test("all four numeric functions execute and mutate original reference",()=>{var p=Piece(5,0,2,5,2);var e=Eval(p,"n = 70(StarIron)\nv = 71(StarIron, 0)\n72(StarIron, 0, 2)\n73(StarIron, 1)");Ok(e);Require((int)e.values["n"]==5&&(int)e.values["v"]==5,"read returns");Require(p.data.starlight.Values.SequenceEqual(new[]{2,5,5,2}),"mutated original");Require(p.data.starlight.Initial.SequenceEqual(new[]{5,0,2,5,2}),"initial immutable");});
 test("read preserves values and reveal follows item through swap/removal",()=>{var p=Piece(5,0,2,5,2);var e=Eval(p,"71(StarIron, 0)\n72(StarIron, 0, 2)\n73(StarIron, 1)");Ok(e);var d=p.data.starlight;Require(!d.Revealed(0)&&d.Revealed(1)&&!d.Revealed(2),"item reveal moves");Require(e.steps[0].starlightBefore.Values.SequenceEqual(e.steps[0].data.starlight.Values),"read non-mutating");Require(e.steps[1].starA==0&&e.steps[1].starB==2&&e.steps[2].starA==1,"zero based highlights");Require(e.steps[0].data.starlight.Count==5&&e.steps[0].data.starlight.Revealed(0),"old receipts independent");});
 test("invalid argument types counts ranges are atomic and report original line",()=>{
 foreach(string call in new[]{"70()","70(StarIron, 0)","71(StarIron)","72(StarIron, 0)","73(StarIron, 0, 1)","71(StarIron, -1)","71(StarIron, 5)","72(StarIron, 0, 99)","73(StarIron, 5)","72(StarIron, 0, True)","71(StarIron, [0])","70(42)"}){
 var p=Piece(5,0,2,5,2);var e=Eval(p,"x = 1\n"+call);Require(e.error!=null&&e.errorLine==2,call+" line");Require(e.steps.Count==0&&p.data.starlight.Values.SequenceEqual(p.data.starlight.Initial),call+" atomic");Near(p.data.starlight.Seconds,0,"invalid free");}
 var other=Piece(5,0,2,5,2);other.material=1;Require(Eval(other,"70(StarIron)").error!=null,"wrong material");
 });
 test("perfect refinement only exact initial nonzero set in ascending order",()=>{
 var p=Piece(5,0,2,5,2);var e=Eval(p,RefineCode);Ok(e);Require(p.data.starlight.Values.SequenceEqual(new[]{2,5})&&p.data.starlight.Perfect,"example exact");p.data.starlight.Remove(1,.25);Require(!p.data.starlight.Perfect,"lost type");
 var d=new ForgeStarlight(new[]{5,0,2,5,2});Require(d.Bonus==0,"impurity");d.Remove(1,0);Require(d.Bonus==0,"duplicates and unsorted");d.Remove(2,0);d.Remove(2,0);Require(d.Bonus==0,"unsorted unique");d.Swap(0,1,0);Require(d.Perfect,"sorted unique");d.Remove(0,0);d.Remove(0,0);Require(!d.Perfect,"empty");var zero=new ForgeStarlight(new int[5]);while(zero.Count>0)zero.Remove(0,0);Require(!zero.Perfect,"all zeros");
 });
 test("exhaustive 100000 initial arrays: reference result and missing type",()=>{
 for(int n=0;n<100000;n++){int x=n;var values=new int[5];for(int i=0;i<5;i++){values[i]=x%10;x/=10;}var d=new ForgeStarlight(values);for(int i=0;i<d.Count;)if(d.Values[i]==0)d.Remove(i,0);else i++;
 for(int i=0;i<d.Count;i++)for(int j=i+1;j<d.Count;j++)if(d.Values[i]>d.Values[j])d.Swap(i,j,0);
 for(int i=1;i<d.Count;)if(d.Values[i]==d.Values[i-1])d.Remove(i,0);else i++;
 Require(d.Values.SequenceEqual(values.Where(v=>v!=0).Distinct().OrderBy(v=>v)),"reference set "+n);Require(d.Perfect==(n!=0),"perfect "+n);if(d.Count>0){d.Remove(0,0);Require(!d.Perfect,"lost type "+n);}}
 });
 test("timing reads no-op and equal-valued distinct swap",()=>{var p=Piece(5,0,2,5,2);var e=Eval(p,"70(StarIron)\n71(StarIron, 0)\n72(StarIron, 0, 0)\n72(StarIron, 0, 3)\n73(StarIron, 1)");Ok(e);Require(e.steps.Take(3).All(s=>s.duration==0),"reads/noop zero");Require(p.data.starlight.Swaps==1&&p.data.starlight.Removals==1,"counts");Near(p.data.starlight.Seconds,.65,"cost");Near(e.steps.Last().productionSeconds,.65,"receipt total");var fast=Piece(5,0,2,5,2);var ef=Eval(fast,"72(StarIron, 0, 1)\n73(StarIron, 0)",5);Ok(ef);Near(fast.data.starlight.Seconds,.13,"speed cost");});
 test("same quality fewer swaps finishes sooner with one cost in product",()=>{
 var p=Piece(5,0,2,5,2);p.shaped=true;var q=Piece(5,0,2,5,2);q.shaped=true;
 var a=Eval(p,RefineCode+"\n!StarIron");var b=Eval(q,"72(StarIron, 0, 1)\n72(StarIron, 0, 1)\n"+RefineCode+"\n!StarIron");Ok(a);Ok(b);Near(b.product.ProductionSeconds-a.product.ProductionSeconds,.8,"extra two swaps");Near(a.product.ProductionSeconds,a.steps.Sum(s=>(double)s.duration),"exactly once");Require(ForgePricing.Bonuses(a.product)["별빛 정제"]==ForgePricing.Bonuses(b.product)["별빛 정제"],"same bonus");Require(a.steps.Last().duration==0,"no completion wait");
 });
 test("old star effect base value and repeat pricing preserved",()=>{var p=Piece(5,0,2,5,2);p.shaped=true;p.data.star=true;Ok(Eval(p,RefineCode));var product=new ForgeProduct("test",0,true,ForgeTraits.None,0,0,7,p.data);var a=ForgePricing.Bonuses(product);Require(a["별무늬"]==ForgeBalance.StarBonus&&a["별빛 정제"]==250&&ForgeBalance.StarlightBonus==250,"separate bonuses");for(int i=0;i<10;i++)Require(ForgePricing.Bonuses(product).SequenceEqual(a),"not stacking");p.data.starlight.Remove(0,0);Require(product.Data.starlight.Perfect,"snapshot isolated");});
 test("practice reset cancels session generation and preserves source",()=>{var source=new ForgeStarlight(new[]{5,0,2,5,2});using(var practice=new ForgeStarlightPractice(source)){
 var s=practice.Run("71(StarIron, 0)\n72(StarIron, 0, 2)\n73(StarIron, 1)\n73(StarIron, 0)",.4f,.25f);s.Advance().GetAwaiter().GetResult();s.Advance().GetAwaiter().GetResult();var old=practice.Piece;uint generation=practice.Generation;Require(old.data.starlight.Swaps==1,"operation in flight receipt");practice.Reset();Require(s.Cancelled&&generation!=practice.Generation,"cancel/invalidate");s.Advance().GetAwaiter().GetResult();Require(practice.Piece!=old&&practice.Piece.data.starlight.Values.SequenceEqual(source.Initial),"new clone same initial");var d=practice.Piece.data.starlight;Require(d.Swaps==0&&d.Removals==0&&d.Seconds==0&&!Enumerable.Range(0,5).Any(d.Revealed),"stats and reveals reset");old.data.starlight.Remove(0,1);Require(d.Count==5&&source.Count==5&&source.Swaps==0,"late old reference and real source isolated");
 }});
 test("practice reexecution retains current and initial, successful run has no stock costs",()=>{using(var p=new ForgeStarlightPractice(new ForgeStarlight(new[]{5,0,2,5,2}))){var a=p.Run("73(StarIron, 1)",.4f,.25f);Drain(a);var b=p.Run("n = 70(StarIron)",.4f,.25f);Drain(b);Ok(b.Evaluation);Require((int)b.Evaluation.values["n"]==4&&p.Piece.data.starlight.Initial.Count==5,"no regeneration");Require(a.Evaluation.cost==0&&a.Evaluation.ironConsumed==0&&a.Evaluation.product==null,"no sale or payment");}});
 test("creation has five hidden bounded values, reads never reroll",()=>{var e=BlueprintRuntime.Evaluate("p = 1(7)\na = 70(p)\nb = 71(p, 0)\nc = 71(p, 0)",0,0,"test",99);Ok(e);var p=(ForgeMvp.Piece)e.values["p"];Require(p.data.starlight.Initial.Count==5&&p.data.starlight.Initial.All(v=>v>=0&&v<=9),"initial generation");Require((int)e.values["b"]==(int)e.values["c"],"repeat read");Require(e.steps[0].data.starlight.Count==5&&!Enumerable.Range(0,5).Any(e.steps[0].data.starlight.Revealed),"hidden load snapshot");});
 test("guide is concise and keeps essential refinement rules",()=>{string guide=ForgeMaterialGuide.Find(7).Description;foreach(string term in new[]{"칼날","0~9","오름차순","250G","종류","가공 시간"})Require(guide.Contains(term),term);Require(guide.Length<500&&!guide.Contains("연습")&&!guide.Contains("예시"),"concise material page");});
 return log.AppendLine("Starlight checks passed: "+count).ToString();
 }
}
