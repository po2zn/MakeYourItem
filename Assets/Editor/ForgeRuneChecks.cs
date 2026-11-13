using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class ForgeRuneChecks
{
 static void Require(bool ok,string text){if(!ok)throw new Exception("Rune: "+text);}
 static void Near(double a,double b,string text)=>Require(Math.Abs(a-b)<1e-7,text+" "+a+" != "+b);
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,Flags).SetValue(o,value);
 static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Flags).Invoke(o,args);
 const string Blade="p=1(1)\n2(p)\n3(p)\n4(p,0)\n";
 public static string RunPure(){
  for(int rune=1;rune<=9;rune++){
   var stocks=Enumerable.Range(2,8).ToDictionary(i=>i,i=>10L);
   var result=ForgeMvp.Evaluate(Blade+"211(p,"+rune+")\n",1000,10,materialStocks:stocks);
   Require(result.error==null,result.error);var step=result.steps.Last();Require(step.process==211&&step.consumed.Count==1&&step.consumed[rune]==1,"matching material "+rune);
   Require(stocks.Values.All(v=>v==10),"caller inventory isolated");
   var bad=ForgeMvp.Evaluate(Blade+"211(p,"+rune+")",1000,rune==1?1:10,materialStocks:new Dictionary<int,long>());
   Require(bad.errorLine==5&&((ForgeMvp.Piece)bad.values["p"]).data.runes.Count==0,"short stock atomic "+rune);
  }
  foreach(var source in new[]{"x=.25\ny=1.0-0.75\nprint(x,y,x==y,1/4)","x=-0.25+0.5\nprint(x*2, x<1, 1.0==1)"}){var result=ForgeMvp.Evaluate(source,0,0);Require(result.error==null,result.error);}
  foreach(var source in new[]{"p=1(1.0)","x=1.0/0", "x=[0.25]"})Require(ForgeMvp.Evaluate(source,0,10).error!=null,"invalid scalar use");
  var state=new ForgeBlacksmiths();state.Normalize(8);state.burningUntil=0;
  for(int grade=1;grade<=3;grade++){
   state.runes.Clear();state.runes.Add(new ForgeBlacksmiths.GiantRune{rune=(grade-1)*3+1,x=.25,y=.25,timed=true,expiresAt=56});
   Near(state.RuneBonus(0,1,8),new[]{.02,.10,.15}[grade-1],"great grade");Near(state.RuneBonus(0,2,8),grade*.01,"destruction grade");Near(state.RuneBonus(0,3,8),new[]{.1,.2,.25}[grade-1],"sale grade");
   Near(state.RuneBonus(1,1,8),0,"outside footprint");state.runes[0].x=.25001;Near(state.RuneBonus(0,1,8),0,"boundary outside");state.runes[0].x=.25;
   state.runes.Add(new ForgeBlacksmiths.GiantRune{rune=1,timed=true,expiresAt=56});Near(state.RuneBonus(0,1,8),new[]{.02,.10,.15}[grade-1],"strongest only");
   state.smiths[0].favor=-100;state.Chances(0,8,out double destruction,out double great);Near(destruction,.1-grade*.01,"actual destruction");Near(great,.01+new[]{.02,.10,.15}[grade-1],"actual great");
   state.burningSmith=0;state.burningUntil=55;state.Chances(0,8,out _,out great);Near(great,.5+new[]{.02,.10,.15}[grade-1],"burning plus rune");state.burningUntil=0;
   Near(state.Remaining((grade-1)*3+1,.25,.25,32.5),23.5,"fractional lifetime");Near(state.RuneBonus(0,1,56),0,"expired effect");state.Normalize(56);Require(state.runes.Count==0,"expiry removes state");
  }
  var tech=new ForgeTechnology();Require(!tech.Owns(BlueprintFeature.GiantRuneTime),"locked time");tech.Restore(new[]{BlueprintFeature.Rune});Require(tech.Owns(BlueprintFeature.GiantRune)&&tech.Owns(BlueprintFeature.GiantRuneTime),"old research grants both");
  var restored=new ForgeTechnology();restored.Restore(tech.Export(),tech.ExportNodes());Require(restored.Owns(BlueprintFeature.GiantRuneTime),"saved unlock");
  Require(BlueprintAliases.All.Count(a=>a.Symbol=="90911")==1,"unique function number");
  var runeGuide=BlueprintGuide.Entries.Single(e=>e.Feature==BlueprintFeature.Rune);var example=ForgeMvp.Evaluate(runeGuide.ExampleFor(false),0,2,materialStocks:new Dictionary<int,long>{{2,2},{3,1}});Require(example.error==null&&example.product!=null&&example.product.Data.runes.SequenceEqual(new[]{1,2,3}),"beginner guide runs with matching basic materials");
  var entry=BlueprintGuide.Entries.Single(e=>e.Feature==BlueprintFeature.GiantRuneTime);Require(BlueprintResearchGuide.EntryVisible(entry,tech,null),"time guide unlocked");Require(BlueprintResearchGuide.VisibleProse(entry.DescriptionFor(false),tech,null).Contains("남은"),"남은 is not silver");
  return "PASS rune material 1-9, float expressions, grade probabilities/prices, square boundaries, strongest-only, 48h expiry, query unlock and guide";
 }
 [MenuItem("Tools/Validation/Runes and Giant Runes")]
 public static void RunUnity(){
  Debug.Log(RunPure());Debug.Log(ForgeBlacksmithChecks.Run());
  var root=new GameObject("Rune checks");root.SetActive(false);var game=root.AddComponent<ForgeMvp>();var cycle=root.AddComponent<ForgeDayCycle>();typeof(ForgeMvp).GetProperty("DayCycle").SetValue(game,cycle);cycle.Clock.Restore(1,8);
  var tech=(ForgeTechnology)Get(game,"technology");var state=(ForgeBlacksmiths)Get(game,"blacksmiths");Set(game,"diamonds",1000);Set(game,"iron",10);Set(game,"materialStocks",Enumerable.Range(2,8).Select(i=>new ForgeMaterialStock{material=i,count=10,unlocked=true}).ToArray());
  var context=(ForgeRunContext)Call(game,"SmithContext",0);
  try{
   var locked=ForgeMvp.Evaluate("90910(1,.25,.25)",0,10,technology:tech,context:context);Require(locked.error!=null&&state.runes.Count==0,"locked actual execution");tech.Restore(new[]{BlueprintFeature.Rune});
   var run=ForgeMvp.Evaluate("x=.25\n90910(1,x,x)\nleft=90911(1,x,x)\nprint(left)",0,10,technology:tech,context:context);Require(run.error==null,run.error);Near((double)run.values["left"],48,"runtime return");Require((int)Get(game,"iron")==9&&(int)Get(game,"diamonds")==975,"actual cost");
   Call(game,"UpdateGiantRuneViews");var views=(List<GameObject>)Get(game,"giantRuneViews");Require(views.Count==1&&views[0].GetComponent<ForgeGiantRuneArrival>().Playing,"actual installation starts arrival");views[0].GetComponent<ForgeGiantRuneArrival>().Sample(1.8f);Near(views[0].transform.localPosition.y,.02,"actual landing floor");
   var stocks=(ForgeMaterialStock[])Get(game,"materialStocks");var extra=ForgeMvp.Evaluate("90910(9,2.5,2.5)",0,10,technology:tech,context:context);Require(extra.error==null&&stocks.Single(v=>v.material==9).count==9&&(int)Get(game,"diamonds")==900,"non-iron giant material and grade cost");
   stocks.Single(v=>v.material==2).count=0;var missing=ForgeMvp.Evaluate("90910(2,1,1)",0,10,technology:tech,context:context);Require(missing.errorLine==1&&(int)Get(game,"diamonds")==900&&state.runes.Count==2,"missing material does not spend diamonds");stocks.Single(v=>v.material==2).count=10;
   Set(game,"diamonds",0);missing=ForgeMvp.Evaluate("90910(2,1,1)",0,10,technology:tech,context:context);Require(missing.errorLine==1&&stocks.Single(v=>v.material==2).count==10&&state.runes.Count==2,"missing diamonds does not spend material");Set(game,"diamonds",900);
   Require(state.RuneAreaOccupied(.7,.7,8)&&!state.RuneAreaOccupied(.7501,.25,8)&&!state.RuneAreaOccupied(.25,.25,56),"overlap, edge clearance and expiry");
   var ground=root.GetComponentInChildren<ForgeRuneGround>(true);Require(ground!=null,"rune creates backing floor");var initial=ground.Extents;ground.Advance(ForgeRuneGround.ExpandSeconds*.5f);Require(ground.Extents!=initial,"floor expands smoothly");Call(game,"UpdateGiantRuneViews");ground.Advance(ForgeRuneGround.ExpandSeconds);Require(ground.Extents.z>=13.75f&&ground.Extents.w>=11.5f,"distant rune has continuous floor");
   var snapshot=JsonUtility.ToJson(state);var copy=JsonUtility.FromJson<ForgeBlacksmiths>(snapshot);copy.Normalize(32);Near(copy.Remaining(1,.25,.25,32),24,"save keeps deadline");copy.Normalize(40);Near(copy.Remaining(1,.25,.25,40),16,"normalization never renews");
   foreach(string source in new[]{"90910(1,.25,.25)","90910(9,.25,.25)","90910(2,.7,.7)","90910(2,.75,.25)","90910(2,-.1,0)","90910(2,3.1,0)","90910(True,0,0)","90911(1,True,0)","90910(1,0)"}){
    var bad=ForgeMvp.Evaluate(source,0,10,technology:tech,context:context);Require(bad.errorLine==1&&JsonUtility.ToJson(state)==snapshot&&(int)Get(game,"diamonds")==900&&(int)Get(game,"iron")==9,"failed call atomic: "+source);
   }
   cycle.Clock.Restore(3,7.5);var remaining=ForgeMvp.Evaluate("left=90911(1,.25,.25)",0,0,technology:tech,context:context);Near((double)remaining.values["left"],.5,"game day boundary");cycle.Clock.Restore(3,8);remaining=ForgeMvp.Evaluate("left=90911(1,.25,.25)",0,0,technology:tech,context:context);Near((double)remaining.values["left"],0,"expired query");Require(state.runes.Count==0,"runtime removes expiry");Call(game,"UpdateGiantRuneViews");Require(((List<GameObject>)Get(game,"giantRuneViews")).Count==0,"expired visual removed");Require(!root.GetComponentInChildren<ForgeRuneGround>(true),"last expiry removes extension");
   // All tiers apply exactly one multiplier to a normal completed sword.
   for(int grade=1;grade<=3;grade++){state.runes.Clear();state.runes.Add(new ForgeBlacksmiths.GiantRune{rune=(grade-1)*3+1,x=0,y=0,timed=true,expiresAt=104});state.burningDay=3;state.burningUntil=0;state.smiths[0].favor=0;
    uint seed=1;for(;seed<100000;seed++){var probe=new ForgeBlacksmiths{randomState=seed};if(probe.Next(1000000)>800000)break;}state.randomState=seed;
    var product=new ForgeProduct("rune",0,true,ForgeTraits.None,0,1,1,new ForgeItemData{handle=2});var result=(ForgeProduct)Call(game,"FinishSmithProduct",0,product);Near(result.Data.smithSaleMultiplier,1+new[]{.1,.2,.25}[grade-1],"actual sale multiplier");int expected=(int)Math.Round(ForgePricing.Quote(product).SalePrice*(1+new[]{.1,.2,.25}[grade-1]));Require(ForgePricing.Quote(result).SalePrice==expected&&ForgePricing.Quote(result).SalePrice==expected,"actual sale price and repeat quote");
   }
   // Legacy permanent runes receive one 48-hour lifetime, retained on later loads.
   var legacy=JsonUtility.FromJson<ForgeBlacksmiths>("{\"runes\":[{\"rune\":4,\"x\":1,\"y\":2}]}");legacy.Normalize(100);legacy.Normalize(101);Near(legacy.Remaining(4,1,2,101),47,"legacy migration once");
   File.WriteAllText("Artifacts/Runes/runtime-results.txt","PASS actual numeric calls, resource costs, atomic errors, game-clock expiry, save migration, unlocks\n");
  }finally{state.runes.Clear();Call(game,"UpdateGiantRuneViews");UnityEngine.Object.DestroyImmediate(root);}
 }
 public static void RunBatch(){try{Directory.CreateDirectory("Artifacts/Runes");RunUnity();ForgeRuneUIChecks.Run();Debug.Log("RUNES_NATIVE_PASS "+DateTime.UtcNow.ToString("O"));EditorApplication.Exit(0);}catch(Exception error){File.WriteAllText("Artifacts/Runes/failure.txt",error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}}
}
