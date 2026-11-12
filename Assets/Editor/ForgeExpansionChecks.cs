using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
public static class ForgeExpansionChecks
{
 const string Blade="p = 1(1)\n2(p)\n3(p)\n";
 const string Cold=Blade+"4(p,0)\n";
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static ForgeMvp.Evaluation Good(string code,Dictionary<int,long> stocks=null,ForgeRunContext context=null){var e=ForgeMvp.Evaluate(code,0,10,materialStocks:stocks,context:context);Require(e.error==null,e.error);return e;}
 public static string Run(){int count=0;var log=new StringBuilder();Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
 check("seal guide examples quote their stated rewards and material costs",()=>{
  foreach(var feature in new[]{BlueprintFeature.Wrap,BlueprintFeature.Unwrap,BlueprintFeature.SealCount})foreach(bool english in new[]{false,true}){
   var guide=BlueprintGuide.Entries.Single(x=>x.Feature==feature);var e=Good(guide.ExampleFor(english));
   int layers=feature==BlueprintFeature.SealCount?2:3;int materialCost=0;
   foreach(var offer in ForgeTechnology.Offers.Where(x=>x.Kind==ForgePurchaseKind.Material))materialCost+=offer.Price*e.product.Data.materials[offer.Material];
   Require(e.product.Data.seals.Count==0&&e.product.Data.sealBonus==layers&&ForgePricing.Quote(e.product).SalePrice==28+layers*140,"guide reward");
   Require(materialCost==(layers==2?7:29),"guide material cost");
  }
  Require(ForgePricing.Quote(Good(Cold+"201(p,2)\n!p").product).SalePrice==0,"unreleased seals are unfinished and worth zero");
 });
 check("reported work speed follows each execution context and actual durations",()=>{
  var first=new ForgeRunContext{Speed=2};var second=new ForgeRunContext{Speed=5};
  using(var a=BlueprintRuntime.StartSession("222(3)\np=1(1)",0,1,context:first))
  using(var b=BlueprintRuntime.StartSession("p=1(1)",0,1,context:second)){
   Require(a.WorkSpeed==2&&b.WorkSpeed==5,"independent initial speeds");
   a.Advance().GetAwaiter().GetResult();Require(a.WorkSpeed==3&&b.WorkSpeed==5,"per-smith setting");
   a.Advance().GetAwaiter().GetResult();b.Advance().GetAwaiter().GetResult();
   Require(Math.Abs(a.Evaluation.steps.Last().duration-.8f/a.WorkSpeed)<.0001&&Math.Abs(b.Evaluation.steps.Last().duration-.8f/b.WorkSpeed)<.0001,"displayed speed agrees with action durations");
  }
 });
 check("hot and cold water, stock depletion and price",()=>{
  var inventory=new Dictionary<int,long>{{3,1},{2,1}};
  var e=Good(Blade+"4(p,3)\n5(p,2)\n!p",inventory);
  Require(e.product.Kind==ForgeProductKind.Sword&&e.product.Data.hardness==7&&ForgePricing.Quote(e.product).SalePrice==69,"quench handle quote");
  Require(inventory[3]==1&&e.steps.Single(s=>s.process==4).consumed[3]==1&&e.product.Data.materials[2]==1,"receipt isolation");
  var cold=Good(Cold+"4(p,3)\n!p");Require(cold.product.Data.hardness==0&&ForgePricing.Quote(cold.product).SalePrice==28,"cold water no bonus");
  var shortage=ForgeMvp.Evaluate(Blade+"4(p,3)",materialStocks:new Dictionary<int,long>());Require(shortage.error!=null&&shortage.steps.Count==3&&((ForgeMvp.Piece)shortage.values["p"]).heat==900,"failed cooling atomic");
 });
 check("handle material selection, atomic failures and stepping",()=>{
  foreach(int material in new[]{1,2,5,7}){
   var e=Good(Cold+"Material = "+material+"\nresult = 5(p,Material)\n!p",new Dictionary<int,long>{{2,1},{5,1},{7,1}});
   var receipt=e.steps.Single(step=>step.process==5);
   Require(e.product.Data.handle==material&&receipt.consumed[material]==1&&e.cost==0&&e.values["result"]==null,"selected material "+material);
   Require(e.ironConsumed==(material==1?2:1)&&e.product.Data.materials[material]==(material==1?2:1),"material accounting");
   Require(ForgePricing.Describe(e.product,false).Contains("손잡이 재료 · "+ForgeMaterialGuide.Find(material).Name),"receipt material name");
  }
  foreach(string arg in new[]{"0","3","4","6","8","9","99","True","'wood'"}){
   var e=ForgeMvp.Evaluate(Cold+"5(p,"+arg+")");Require(e.error!=null&&!e.steps.Any(step=>step.process==5)&&((ForgeMvp.Piece)e.values["p"]).data.handle==0,"invalid handle "+arg);
  }
  var missing=ForgeMvp.Evaluate(Cold+"5(p,5)",materialStocks:new Dictionary<int,long>());Require(missing.error!=null&&!missing.steps.Any(step=>step.process==5),"missing material");
  var ironMissing=ForgeMvp.Evaluate(Cold+"5(p,1)",availableIron:1);Require(ironMissing.error!=null&&ironMissing.ironConsumed==1&&!ironMissing.steps.Any(step=>step.process==5),"iron stock atomic");
  var twice=ForgeMvp.Evaluate(Cold+"5(p,2)\n5(p,5)",materialStocks:new Dictionary<int,long>{{2,1},{5,1}});Require(twice.error!=null&&twice.steps.Count(step=>step.process==5)==1&&((ForgeMvp.Piece)twice.values["p"]).data.handle==2,"no replacement consumption");
  using(var session=BlueprintRuntime.StartSession(Cold+"5(p,2)\nprint(102(p,2))",0,10,materialStocks:new Dictionary<int,long>{{2,1}})){
   while(session.NextLine!=5)session.Advance().GetAwaiter().GetResult();Require(((ForgeMvp.Piece)session.Evaluation.values["p"]).data.handle==0,"before handle step");
   session.Advance().GetAwaiter().GetResult();Require(session.NextLine==6&&((ForgeMvp.Piece)session.Evaluation.values["p"]).data.handle==2&&session.Evaluation.steps.Count(step=>step.process==5)==1,"single handle step");
  }
 });
 check("polish plating, star frost and dragon refinement",()=>{
  Require(Good(Cold+"6(p)\n7(p)\n!p").product.Data.plated,"plating");
  Require(!Good(Cold+"7(p)\n!p").product.Data.plated,"wrong order loses bonus");
  var star=Good(Blade.Replace("1(1)","1(7)")+"4(p,8)\n!p");Require(star.product.Data.star&&star.product.Data.frost,"combined traits");
  var bone=Good(Cold+"6(p)\nb = 1(9)\n10(b,p,100,13)\n5(p,b)\n9(p,1)\n9(p,1)\n9(p,1)\n!p");Require(bone.product.Data.handle==9&&ForgePricing.Bonuses(bone.product).ContainsKey("용의 맥동"),"dragon combo");
 });
 check("lucky daily identity, validation, duplicate application and midnight",()=>{
  Require(BlueprintAliases.Find(BlueprintFeature.LuckyCode).Symbol=="777?","canonical lucky query");
  Require(BlueprintLexer.Scan("777(777?(),p) # 777?()\ns=\"777?()\"").Count(t=>t.Feature==BlueprintFeature.LuckyCode)==1,"query is one token and comments/strings remain opaque");
  Require(ForgeCodeTranslation.Translate("lucky_code()",false)=="777?()","query translation");
  Require(ForgeMvp.Evaluate("x=778()").error==null,"old saved blueprint compatibility");
  int day=1;var context=new ForgeRunContext{Day=()=>day,Seed=31};int yesterday=context.LuckyCode;day++;Require(context.LuckyCode!=yesterday,"daily refresh");
  Require(!BlueprintAliases.All.Any(a=>a.Symbol==context.LuckyCode.ToString()),"reserved code exclusion");
  var e=Good(Cold+"5(p,2)\np=777(777?(),p)\np=777(777?(),p)\n!p",context:context);Require(e.product.DiamondReward==1&&e.steps.Count(s=>s.process==777)==1,"one reward");
  Require(ForgeMvp.Evaluate(Cold+"5(p,2)\n777("+yesterday+",p)",context:context).error!=null,"stale code rejected");
  using(var session=BlueprintRuntime.StartSession(Cold+"5(p,2)\np=777(777?(),p)\n!p",0,10,context:context)){
   while(session.NextLine!=7&&!session.Completed)session.Advance().GetAwaiter().GetResult();day++;session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.product.DiamondReward==1&&session.Evaluation.product.Data.luckyDay==2,"application survives midnight");
  }
 });
 check("stack set mutation and safe errors",()=>{
  var e=Good("a=[]\n301(a,2)\n301(a,3)\nx=302(a)\ns={1}\n303(s,x)\nprint(a,s)");Require((int)e.values["x"]==3&&((List<int>)e.values["a"]).Count==1&&((HashSet<int>)e.values["s"]).Contains(3),"collections");
  Require(ForgeMvp.Evaluate("a=[]\n302(a)").errorLine==2,"empty pop");
 });
 check("seals reverse order, damage and bounded bonus",()=>{
  var e=Good(Cold+"201(p,1)\n201(p,2)\n202(p,1)\n202(p,2)\n202(p,1)\n!p");Require(e.product.Data.sealBonus==1&&e.product.Data.seals.Count==0,"damaged layer no reward");
 });
 check("runes duplicate family does not consume material",()=>{
  var e=ForgeMvp.Evaluate(Cold+"211(p,1)\n211(p,4)",materialStocks:new Dictionary<int,long>{{6,2}});Require(e.error!=null&&e.steps.Count(s=>s.process==211)==1,"duplicate gate");
  var full=Good(Cold+"211(p,7)\n211(p,8)\n211(p,9)\n!p");Require(ForgePricing.Bonuses(full.product).ContainsKey("삼중 공명"),"three families");
 });
 check("fixed resonance target and successful attachment",()=>{
  var e=Good(Cold+"c=1(6)\nfor heatValue in [100,200,300,400,500,600,700,800,900]:\n    8(heatValue)\n    2(c)\n    if 501(c)==0:\n        break\n4(c,0)\n5(p,c)\n!p");Require(e.product.Data.activated,"resonance");
 });
 check("timed magic decay and acceleration",()=>{
  var hot=Good("p=1(1)\n401(p)\n2(p)\n3(p)\n4(p,0)\n!p");
  var late=Good("p=1(1)\n401(p)\n2(p)\n3(p)\n400(5)\n4(p,0)\n!p");Require(hot.product.Data.magicGrade>late.product.Data.magicGrade,"decay");
  var fast=Good("222(2)\n"+Cold+"!p");Require(fast.product.ProductionSeconds<hot.product.ProductionSeconds,"speed");
 });
 check("recovery and absorption prevent reuse",()=>{
  var e=ForgeMvp.Evaluate("p=1(1)\n404(p)\n404(p)");Require(e.errorLine==3&&e.steps.Count(s=>s.refund==1)==1,"recovery once");
  Require(ForgeMvp.Evaluate("p=1(1)\n404(p)\n!p").error!=null,"recovered item cannot sell");
 });
 check("purchase gates and all expansion examples",()=>{
  foreach(var d in ForgeExpansion.Definitions){
   var tech=new ForgeTechnology();int gold=100000000,iron=10;ForgeMaterialStock[] stocks=null;
   if(d.Price>0)Require(ForgeMvp.Evaluate(d.Example,technology:tech).compileError,"locked "+d.Alias.Symbol);
   ForgeTechnologyChecks.BuyAllNodes(tech,ref gold,ref iron,ref stocks);
   Require(ForgeMvp.Evaluate(d.Example,technology:tech).error==null,"example "+d.Alias.Symbol);
  }
 });
 check("translation preserves literals strings comments and execution",()=>{
  string original="x=100\n# 100(p) while\nprint('100(p)')\ny=abs(-3)";string numeric=ForgeCodeTranslation.Translate(original,false);
  Require(numeric.Contains("x=100")&&numeric.Contains("# 100(p) while")&&numeric.Contains("'100(p)'")&&numeric.Contains("101(-3)"),"tokens only");Require(ForgeCodeTranslation.Translate(numeric,true)==original,"roundtrip");
 });
 log.AppendLine("TOTAL expansion "+count+" passed");return log.ToString();
 }
#if UNITY_EDITOR
 [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Crafting Expansion")]
 public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
