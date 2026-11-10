using System;
using System.Linq;
using System.Collections.Generic;
public static class BlueprintTimeChecks
{
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 public static string Run(){
  foreach(double hour in new[]{0d,5.999,6,12,17.999,18,23.999}){
   var e=ForgeMvp.Evaluate("d = 12()\nn = 24()\nm = 24(60)",clockHour:()=>hour);
   Require(e.error==null,e.error);int day=hour>=6&&hour<18?1:0;
   Require((int)e.values["d"]==day&&(int)e.values["n"]==1-day&&(int)e.values["m"]==(int)Math.Floor(hour*60),"boundaries");
   Require(e.cost==0&&e.ironConsumed==0&&e.steps.Count==0&&e.product==null,"no side effects");
  }
  foreach(var code in new[]{"12(1)","24(1)","24(True)","24(60,60)"})Require(ForgeMvp.Evaluate(code).compileError,"invalid literal/arity "+code);
  Require(ForgeMvp.Evaluate("x = 1\n24(x)").errorLine==2,"dynamic invalid argument");
  double now=17.5;
  using(var session=BlueprintRuntime.StartSession("a = 24(60)\nb = 24()\nc = 24(60)",0,0,clockHour:()=>now)){
   session.Advance().GetAwaiter().GetResult();now=18;
   session.Advance().GetAwaiter().GetResult();now=0;
   session.Advance().GetAwaiter().GetResult();
   Require((int)session.Evaluation.values["a"]==1050&&(int)session.Evaluation.values["b"]==1&&(int)session.Evaluation.values["c"]==0,"live stepped clock");
  }
  var tech=new ForgeTechnology();int gold=100,iron=0;ForgeMaterialStock[] stocks=null;
  tech.Restore(new[]{BlueprintFeature.If});
  Require(ForgeMvp.Evaluate("print(12(),24(),24(60))",technology:tech).compileError,"time bundle starts locked");
  var timeOffer=ForgeTechnology.Find("function-IsDay");
  Require(tech.TryPurchase(timeOffer,ref gold,ref iron,ref stocks,false,out var purchaseMessage),purchaseMessage);
  Require(gold==60,"time bundle charged once");
  foreach(var feature in new[]{BlueprintFeature.IsDay,BlueprintFeature.NightTime}){
   string source=feature==BlueprintFeature.IsDay?"print(12())":"print(24(),24(60))";
   Require(ForgeMvp.Evaluate(source,technology:tech,clockHour:()=>23).error==null,"unlocked");
  }
  var modules=new Dictionary<string,string>{{"clockwork","def read():\n    return 24(60)"}};
  var imported=ForgeMvp.Evaluate("import clockwork\nx = clockwork.read()",modules:modules,clockHour:()=>13.5);
  Require(imported.error==null&&(int)imported.values["x"]==810,"import shared clock");
  using(var session=BlueprintRuntime.StartSession("print(1)\n24(1)",0,0,deferErrors:true)){
   session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.steps.Count==1&&session.Evaluation.error==null,"valid prefix");
   session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.errorLine==2,"deferred invalid argument");
  }
  return "PASS time functions: boundaries, integer minutes, live stepping, no cost, invalid arguments, unlocks, import, deferred errors";
 }
#if UNITY_EDITOR
 [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Time Functions")]
 public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
