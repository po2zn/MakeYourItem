using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
public static class BlueprintImportChecks
{
    public const string Smith = "TARGET = 7\ndef forge(material):\n    blade = 1(material)\n    2(blade)\n    3(blade)\n    4(blade, 0)\n    return blade";
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static Dictionary<string,string> Sources()=>new Dictionary<string,string>{{"smith",Smith}};
    static ForgeMvp.Evaluation Good(string source,IReadOnlyDictionary<string,string> modules=null)
    {var r=ForgeMvp.Evaluate(source,100,10,modules:modules??Sources());Require(r.error==null,r.error);return r;}
    public static string Run()
    {
        var log=new StringBuilder();int count=0;
        Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("import module, qualified call and craft receipts",()=>{
            var r=Good("import smith\nblade = smith.forge(1)\n!blade");
            Require(r.cost==0&&r.ironConsumed==1&&ForgePricing.Quote(r.product).SalePrice==28,"economy");
            Require(r.steps.Take(4).All(s=>s.module=="smith")&&r.steps.Select(s=>s.process).SequenceEqual(new[]{1,2,3,4,0}),"motion order");
            Require(r.steps[0].line==3&&r.steps.Last().line==3&&r.steps.Last().module==null,"source lines");
        });
        check("module and function aliases match Python forms",()=>{
            foreach(var source in new[]{"import smith as s\np = s.forge(1)\n!p","from smith import forge\np = forge(1)\n!p","from smith import forge as make\np = make(1)\n!p"})Require(Good(source).cost==0,source);
        });
        check("comma imports and parenthesized from imports",()=>{
            var modules=Sources();modules["other"]="def value():\n    return 3";
            var r=Good("import smith as s, other as o\nfrom smith import (forge as make, TARGET,)\nprint(TARGET, o.value())\np = make(1)\n!p",modules);
            Require(r.steps[0].text=="7 3"&&r.cost==0,"multiple");
        });
        check("module globals stay with defining module and helpers",()=>{
            var modules=Sources();modules["helper"]="TARGET = 8\ndef get():\n    return TARGET\ndef outer():\n    return get()";
            var r=Good("TARGET = 1\nfrom helper import outer as answer\nprint(answer())",modules);Require(r.steps.Single().text=="8","global owner");
        });
        check("module initialization cached across repeated and diamond imports",()=>{
            var modules=new Dictionary<string,string>{{"base","print('init')\nx = 4"},{"left","import base"},{"right","from base import x"}};
            var r=Good("import left, right\nimport base as again\nfrom base import x\nprint(left.base.x, right.x, again.x, x)",modules);
            Require(r.steps.Count==2&&r.steps[0].text=="init"&&r.steps[1].text=="4 4 4 4","once");
        });
        check("fresh runs see updated sources without retaining module cache",()=>{
            var modules=new Dictionary<string,string>{{"m","x = 1"}};Require(Good("import m\nprint(m.x)",modules).steps.Single().text=="1","first");modules["m"]="x = 2";Require(Good("import m\nprint(m.x)",modules).steps.Single().text=="2","reload per run");
        });
        check("import initialization and function effects share budgets",()=>{
            var modules=Sources();modules["smith"]="print('init')\nprepared = 1(1)\n"+Smith;
            var r=Good("import smith\nimport smith as again\np = again.forge(1)\n!p",modules);Require(r.cost==0&&r.ironConsumed==2&&r.steps.Count(s=>s.process==1)==2,"cost");
            var shortStock=ForgeMvp.Evaluate("import smith\np = smith.forge(1)",0,1,modules:modules);Require(shortStock.error!=null&&shortStock.cost==0&&shortStock.steps.Count==2&&shortStock.errorModule=="smith","partial effects");
        });
        check("from import binds only requested names",()=>{
            var r=ForgeMvp.Evaluate("from smith import forge\nprint(smith)",modules:Sources());Require(r.error!=null&&!r.compileError&&r.errorLine==2,"namespace");
        });
        check("wildcard imports public names and excludes underscore",()=>{
            var modules=new Dictionary<string,string>{{"m","_secret = 5\nvalue = 9\ndef answer():\n    return value"}};
            var r=Good("from m import *\nprint(answer(), value)",modules);Require(r.steps.Single().text=="9 9"&&!r.values.ContainsKey("_secret"),"wildcard");
            Require(ForgeMvp.Evaluate("def f():\n    from m import *\nf()",modules:modules).compileError,"function wildcard");
        });
        check("conditional and function local imports execute lazily",()=>{
            var r=Good("if False:\n    import missing\ndef create():\n    from smith import forge as make\n    return make(1)\np = create()\n!p");Require(r.cost==0&&!r.values.ContainsKey("make"),"local import");
        });
        check("import module main guard and __name__",()=>{
            var modules=Sources();modules["smith"]=Smith+"\nif __name__ == '__main__':\n    !forge(1)";
            var imported=Good("import smith\nprint(smith.__name__, __name__)",modules);Require(imported.cost==0&&imported.steps.Single().text=="smith __main__","guard");
            Require(Good(modules["smith"],modules).cost==0,"direct main");
        });
        check("imported module submission gives source-aware error",()=>{
            var r=ForgeMvp.Evaluate("import m",modules:new Dictionary<string,string>{{"m","p = 1(1)\n!p"}});Require(r.errorModule=="m"&&r.errorLine==2&&r.cost==0&&r.product==null,"module submission");
        });
        check("function references and re-export keep defining globals",()=>{
            var modules=Sources();modules["exports"]="from smith import forge as make";
            var r=Good("import exports\nf = exports.make\np = f(1)\n!p",modules);Require(r.cost==0&&r.steps[1].module=="smith","function reference");
        });
        check("missing module member and wrong arity remain controlled errors",()=>{
            foreach(var source in new[]{"import missing","import smith\nsmith.unknown()","from smith import missing","import smith\nsmith.forge()","from smith import forge\nforge(1, 2)","import smith\nsmith()"}){var r=ForgeMvp.Evaluate(source,modules:Sources());Require(r.error!=null&&r.cost==0,source);}
        });
        check("imported syntax errors identify module and exact source line",()=>{
            var r=ForgeMvp.Evaluate("print('before')\nimport broken",modules:new Dictionary<string,string>{{"broken","x = 1\ndef nope(:"}});Require(r.errorModule=="broken"&&r.errorLine==2&&r.steps.Single().text=="before","diagnostic");
        });
        check("imported runtime and overflow errors keep source through callers",()=>{
            foreach(string expr in new[]{"missing","2147483647 + 1"}){
                var r=ForgeMvp.Evaluate("import m\nm.bad()",modules:new Dictionary<string,string>{{"m","def bad():\n    return "+expr}});Require(r.errorModule=="m"&&r.errorLine==2,"function error");
            }
            var caller=ForgeMvp.Evaluate("import smith\np = smith.forge(1)\nprint(Missing)",modules:Sources());Require(caller.errorModule==null&&caller.errorLine==3&&caller.cost==0,"caller restored");
        });
        check("cyclic imports and ambiguous names fail clearly",()=>{
            var r=ForgeMvp.Evaluate("import a",modules:new Dictionary<string,string>{{"a","import b"},{"b","import a"}});Require(r.error!=null&&r.errorMessage.Contains("순환")&&r.errorModule=="b","cycle");
            r=ForgeMvp.Evaluate("import m",modules:new Dictionary<string,string>{{"m",null}});Require(r.errorMessage.Contains("여러 개"),"duplicates");
        });
        check("Unicode titles normalize consistently",()=>{var r=Good("import 재료 as m\nprint(m.값)",new Dictionary<string,string>{{"재료","값 = 3"}});Require(r.steps.Single().text=="3","unicode");});
        check("module resource and command limits cannot reset via imports",()=>{
            var r=ForgeMvp.Evaluate("import m",100,0,modules:new Dictionary<string,string>{{"m","1(1)"}});Require(r.error!=null&&r.cost==0&&r.errorModule=="m","stock");
            r=ForgeMvp.Evaluate("import m",modules:new Dictionary<string,string>{{"m","while True:\n    x = 1"}});Require(r.errorMessage.Contains("10000")&&r.errorModule=="m","command limit");
        });
        check("technology ownership applies inside imported modules",()=>{
            var tech=new ForgeTechnology();tech.Restore(Array.Empty<BlueprintFeature>(),new[]{ForgeTechnology.AutomationRoot,ForgeTechnology.ImportResearch});var r=ForgeMvp.Evaluate("import m",technology:tech,modules:new Dictionary<string,string>{{"m","x = random(1, 2)"}});Require(r.error!=null&&r.errorModule=="m"&&r.errorMessage.Contains("구매"),"technology");
        });
        check("import syntax and qualified names highlight without changing source",()=>{
            var tokens=ForgeSyntax.Scan("from smith import forge as make\nimport smith\nsmith.forge(1)");Require(tokens.Count(t=>t.kind==ForgeSyntax.Kind.Reserved)==4&&tokens.Any(t=>t.kind==ForgeSyntax.Kind.Function),"highlight");
            foreach(var source in new[]{"import","from smith forge","import smith as","import 'smith'","from smith import","import smith,","from smith import forge,"})Require(ForgeMvp.Evaluate(source,modules:Sources()).compileError,source);
        });
        return log.AppendLine("TOTAL import "+count+" passed").ToString();
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Blueprint Imports")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
