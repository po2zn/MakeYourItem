using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
public static class BlueprintDeferredChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static BlueprintRuntime.Session Start(string source,IReadOnlyDictionary<string,string> modules=null,ForgeTechnology technology=null)=>BlueprintRuntime.StartSession(source,100,10,modules:modules,technology:technology,deferErrors:true);
    static void Step(BlueprintRuntime.Session session){session.Advance().GetAwaiter().GetResult();}
    static void Drain(BlueprintRuntime.Session session){int count=0;while(!session.Completed){Require(++count<11000,"step limit");Step(session);}}
    public static string Run()
    {
        int count=0;var log=new StringBuilder();Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("future arity error preserves earlier real receipts",()=>{
            using(var s=Start("p = 1(1)\n2()\nprint('after')")){
                Require(!s.Completed&&s.NextLine==1&&s.Evaluation.error==null,"initial");Step(s);
                Require(s.NextLine==2&&s.Evaluation.error==null&&s.Evaluation.cost==0&&s.Evaluation.steps.Count==1,"load");Step(s);
                Require(s.Completed&&!s.Evaluation.compileError&&s.Evaluation.errorLine==2&&s.Evaluation.cost==0&&s.Evaluation.steps.Count==1,"reached arity");
            }
        });
        check("missing functions and wrong literal types fail only when reached",()=>{
            foreach(string bad in new[]{"unknown()","2(1, 7)","x = abs(True)","4(p, 2)"})using(var s=Start("p = 1(1)\n"+bad)){Step(s);Require(s.Evaluation.error==null&&s.NextLine==2,bad);Step(s);Require(s.Completed&&s.Evaluation.errorLine==2&&s.Evaluation.cost==0,bad);}
        });
        check("bad parentheses and argument assignment do not execute partial expressions",()=>{
            foreach(string bad in new[]{"2(p, 7","!4(3(2(r = 1(1), 7)), 0)","x = (1 + )"})using(var s=Start("p = 1(1)\n"+bad+"\nprint('after')")){Step(s);Require(s.NextLine==2&&s.Evaluation.error==null,bad);Step(s);Require(s.Completed&&s.Evaluation.errorLine==2&&s.Evaluation.steps.Count==1&&s.Evaluation.cost==0,bad);}
        });
        check("unterminated string preserves valid lexical prefix",()=>{
            using(var s=Start("p = 1(1)\nprint('broken\nprint('after')")){Step(s);Require(s.NextLine==2&&s.Evaluation.error==null,"prefix");Step(s);Require(s.Completed&&s.Evaluation.errorLine==2&&s.Evaluation.cost==0,"quote error");}
        });
        check("invalid indentation stops at unreadable boundary after prior lines",()=>{
            using(var s=Start("p = 1(1)\nif True:\n  2(p)")){Step(s);Require(s.Evaluation.cost==0&&s.NextLine==2,"before header");Step(s);Require(s.NextLine==3&&s.Evaluation.error==null,"header entered");Step(s);Require(s.Completed&&s.Evaluation.errorLine==3&&s.Evaluation.cost==0,"indent error");}
        });
        check("valid statements inside a block run before its malformed line",()=>{
            using(var s=Start("if True:\n    p = 1(1)\n    2(p, 7\nprint('after')")){Step(s);Step(s);Require(s.NextLine==3&&s.Evaluation.cost==0&&s.Evaluation.error==null,"body prefix");Step(s);Require(s.Completed&&s.Evaluation.errorLine==3,"body error");}
        });
        check("unselected branches and unused function bodies do not fail",()=>{
            using(var s=Start("def unused():\n    2()\nif False:\n    missing()\nprint('ok')")){Drain(s);Require(s.Evaluation.error==null&&s.Evaluation.steps.Single().text=="ok","unreached");}
        });
        check("function call pauses before its bad body statement",()=>{
            using(var s=Start("def f():\n    p = 1(1)\n    2()\nf()")){Step(s);Step(s);Require(s.NextLine==2,"entered");Step(s);Require(s.NextLine==3&&s.Evaluation.cost==0&&s.Evaluation.error==null,"valid body");Step(s);Require(s.Evaluation.errorLine==3,"function error");}
        });
        check("imports preserve module prefix and report original module line",()=>{
            using(var s=Start("import m",new Dictionary<string,string>{{"m","p = 1(1)\n2()"}})){
                Step(s);Require(s.NextModule=="m"&&s.NextLine==1,"module pause");Step(s);Require(s.NextLine==2&&s.Evaluation.cost==0&&s.Evaluation.error==null,"module prefix");Step(s);Require(s.Evaluation.errorModule=="m"&&s.Evaluation.errorLine==2,"module error");
            }
        });
        check("block context errors remain enforced at reached statement",()=>{
            foreach(var bad in new[]{"break","continue","return 1"})using(var s=Start("p = 1(1)\n"+bad)){Step(s);Step(s);Require(s.Completed&&s.Evaluation.errorLine==2&&s.Evaluation.cost==0,bad);}
        });
        check("first-line errors pause before reporting",()=>{
            foreach(var code in new[]{"2()","x = (","print('broken"})using(var s=Start(code)){Require(!s.Completed&&s.Evaluation.error==null&&s.NextLine==1,"pause");Step(s);Require(s.Completed&&s.Evaluation.errorLine==1&&s.Evaluation.cost==0,code);}
        });
        check("full execution still rejects whole-source compile errors",()=>{
            string code="p = 1(1)\n2()";using(var s=BlueprintRuntime.StartSession(code,100,10)){Require(s.Completed&&s.Evaluation.compileError&&s.Evaluation.cost==0,"strict session");}Require(ForgeMvp.Evaluate(code).compileError,"strict evaluator");
        });
        check("technology checks are delayed but cannot be bypassed",()=>{
            using(var s=Start("p = 1(1)\nx = random(1, 2)",technology:new ForgeTechnology())){Step(s);Require(s.Evaluation.cost==0&&s.Evaluation.error==null,"prefix");Step(s);Require(s.Evaluation.errorLine==2&&s.Evaluation.errorMessage.Contains("구매"),"gate");}
        });
        check("valid prefix and malformed elif retain branch control flow",()=>{
            using(var s=Start("p = 1(1)\nif False:\n    print('no')\nelif True\n    print('no')")){Step(s);Step(s);Require(s.NextLine==4&&s.Evaluation.error==null&&s.Evaluation.cost==0,"elif position");Step(s);Require(s.Evaluation.errorLine==4,"elif fault");}
        });
        check("malformed unselected elif does not leak its body into outer scope",()=>{
            using(var s=Start("if True:\n    print('selected')\nelif True\n    1(1)\nprint('after')")){Drain(s);Require(s.Evaluation.error==null&&s.Evaluation.cost==0&&s.Evaluation.steps.Select(x=>x.text).SequenceEqual(new[]{"selected","after"}),"branch recovery");}
        });
        return log.AppendLine("TOTAL deferred errors "+count+" passed").ToString();
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Deferred Step Errors")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
