using System;
using System.Linq;
using System.Collections.Generic;
public static class BlueprintMatchChecks
{
 static int checks;
 static void Require(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
 static ForgeMvp.Evaluation Good(string text,ForgeTechnology tech=null){var r=BlueprintRuntime.Evaluate(text,1000,10,"match-test",1,technology:tech);Require(r.error==null,r.error+"\n"+text);return r;}
 static void Result(string text,object expected){var r=Good(text);Require(r.values.TryGetValue("answer",out var value)&&Equals(value,expected),"Expected "+expected);}
 static void Bad(string text){var r=BlueprintRuntime.Evaluate(text,1000,10,"test",1);Require(r.compileError,"Expected compile error: "+text+" / "+r.error);}
 public static string Run(){checks=0;
  Result("match 2:\n    case 1:\n        answer = 10\n    case 2:\n        answer = 20\n    case _:\n        answer = 30",20);
  Result("696 2:\n    6? 1:\n        answer = 10\n    6? 2:\n        answer = 20\n    9?:\n        answer = 30",20);
  Result("answer = 0\nmatch 4:\n    case 1:\n        answer = 1",0);
  Result("match -2:\n    case -1 | -2:\n        answer = 9\n    case _:\n        answer = 0",9);
  Result("match 'iron':\n    case 'wood':\n        answer = 0\n    case 'iron':\n        answer = 1",1);
  Result("match 1:\n    case True:\n        answer = 0\n    case 1:\n        answer = 1",1);
  Result("match True:\n    case 1:\n        answer = 1",1);
  Result("match None:\n    case None:\n        answer = 1",1);
  Result("match 7:\n    case n if n < 5:\n        answer = 0\n    case n if n > 5:\n        answer = n",7);
  Result("696 7:\n    6? n 1? n > 5:\n        answer = n\n    9?:\n        answer = 0",7);
  Result("match 2:\n    case (1 | 2) as n:\n        answer = n",2);
  Result("match [1,2,3]:\n    case [head,*rest]:\n        answer = head + len(rest)",3);
  Result("match (1,2,3):\n    case [head,*_,tail]:\n        answer = head + tail",4);
  Result("match 1,2:\n    case a,b:\n        answer = a+b",3);
  Result("match {1:2,3:4}:\n    case {1: x, **rest}:\n        answer = x + rest[3]",6);
  Result("match 3:\n    case [x] | (x,):\n        answer = 0\n    case _:\n        answer = 1",1);
  Result("answer = 0\nmatch 1:\n    case 1:\n        answer = 1\n    case 1 if 1 // 0 > 0:\n        answer = 2",1);
  Result("def choose(x):\n    match x:\n        case 1:\n            return 10\n        case _:\n            return 20\nanswer = choose(1)",10);
  Result("answer = 0\nfor x in [1,2,3]:\n    match x:\n        case 1:\n            continue\n        case 2:\n            answer = x\n        case _:\n            break",2);
  Result("match 1:\n    case 1:\n        match 2:\n            case 2:\n                answer = 3",3);
  Result("match = 696\ncase = 6\nanswer = match + case",702);
  foreach(var text in new[]{"case 1:\n    print(1)","9?:\n    print(1)","match 1:\n    print(1)","match 1:\n    case _:\n        print(1)\n    case 1:\n        print(2)","match 1:\n    case x | 1:\n        print(x)","match [1,2]:\n    case [x,x]:\n        print(x)","match [1,2]:\n    case [*x,*y]:\n        print(1)","match 1:\n    case 1+2:\n        print(1)","match 1:\n    case 1:\n        break"})Bad(text);
  Result("match '#':\n    case '#':\n        answer = 1",1);
  Result("match [1,2,3]:\n    case *rest, last:\n        answer = len(rest)+last",5);
  Result("n = 99\nmatch 2:\n    case n:\n        answer = n",2);
  Bad("match [1]:\n    case (*rest):\n        print(1)");
  var modules=new Dictionary<string,string>{{"constants","VALUE = 3\nVALUES = [1,2]"}};
  var imported=BlueprintRuntime.Evaluate("import constants\nmatch 3:\n    case constants.VALUE:\n        answer = 3",0,0,"main",1,modules:modules);Require(imported.error==null&&Equals(imported.values["answer"],3),"module value pattern");
  imported=BlueprintRuntime.Evaluate("import constants\nmatch [1,2]:\n    case constants.VALUES:\n        answer = 3",0,0,"main",1,modules:modules);Require(imported.error==null&&Equals(imported.values["answer"],3),"collection value equality");
  var once=Good("def subject():\n    print('once')\n    return 2\nmatch subject():\n    case 1:\n        answer = 1\n    case 2:\n        answer = 2");Require(once.steps.Count(step=>step.debugOutput)==1,"subject evaluated once");
  Result("match 1:\n    case _ if set():\n        answer = 0\n    case _:\n        answer = 1",1);
  var tech=new ForgeTechnology();var offer=ForgeTechnology.Find("function-Match");Require(offer.Price==5000&&offer.Parents.SequenceEqual(new[]{"function-Random"}),"research price/parent");
  Require(tech.MissingPrerequisites(offer)!=null,"locked without random");tech.Restore(new BlueprintFeature[0],new[]{"function-Random"});Require(!tech.Owns(BlueprintFeature.Match)&&tech.MissingPrerequisites(offer)==null,"random unlocks purchase only");
  var locked=BlueprintRuntime.Evaluate("696 1:\n    6? 1:\n        print(1)",0,0,"",1,technology:tech);Require(locked.compileError,"research gate");
  int gold=5000,iron=0;ForgeMaterialStock[] stock=new ForgeMaterialStock[0];Require(tech.TryPurchase(offer,ref gold,ref iron,ref stock,false,out var message)&&gold==0,"purchase costs 5K");Require(tech.Owns(BlueprintFeature.Case)&&tech.Owns(BlueprintFeature.Default),"bundle owned");
  Good("696 1:\n    6? 1:\n        print(1)",tech);
  var english=BlueprintRuntime.Evaluate("match 1:\n    case 1:\n        print(1)",0,0,"",1,technology:tech);Require(english.compileError,"English research gate");
  tech.Restore(tech.Export(),tech.ExportNodes().Concat(new[]{ForgeTechnology.EnglishResearch}).ToArray());Good("match 1:\n    case 1:\n        print(1)",tech);
  var restored=new ForgeTechnology();restored.Restore(tech.Export(),tech.ExportNodes());Require(restored.Owns(BlueprintFeature.Match)&&restored.Owns(BlueprintFeature.Default),"save restore");
  var run=BlueprintRuntime.StartSession("696 2:\n    6? 1:\n        print(1)\n    6? 2:\n        print(2)\n    9?:\n        print(3)",0,0,deferErrors:true);var lines=new List<int>();while(!run.Completed){lines.Add(run.NextLine);run.Advance().GetAwaiter().GetResult();}Require(run.Evaluation.error==null&&run.Evaluation.steps.Single().text=="2","stepped execution");Require(lines.SequenceEqual(new[]{1,2,4,5}),"stepped line positions: "+string.Join(",",lines));
  foreach(string header in new[]{"696 x:","    6? 1:","    9?:","match x:","    case 1:"}){var e=BlueprintIndentation.Enter(header,header.Length,header.Length);Require(e.Text.EndsWith(new string(' ',header.StartsWith(" ")?8:4)),"indent "+header);}
  var guide=BlueprintGuide.Entries.Single(e=>e.Id=="match");Require(BlueprintResearchGuide.EntryVisible(guide,restored,new ForgeBlacksmiths()),"guide unlock after purchase");Require(!BlueprintResearchGuide.EntryVisible(guide,new ForgeTechnology(),new ForgeBlacksmiths()),"guide hidden before purchase");
  return "PASS match/case: "+checks+" assertions";
 }
}
