using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
public static class BlueprintStepChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static BlueprintRuntime.Session Start(string code,IReadOnlyDictionary<string,string> modules=null)=>BlueprintRuntime.StartSession(code,0,10,modules:modules);
    static void Step(BlueprintRuntime.Session session){session.Advance().GetAwaiter().GetResult();Require(session.Evaluation.error==null,session.Evaluation.error);}
    static void Drain(BlueprintRuntime.Session session){int n=0;while(!session.Completed){Require(++n<11000,"step budget");session.Advance().GetAwaiter().GetResult();}}
    public static string Run()
    {
        int count=0;var log=new StringBuilder();Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("first click executes one line with no future effects",()=>{
            using(var s=Start("x = 1\np = 1(1)\n2(p, 7)\n!p")){
                Require(s.NextLine==1&&s.Evaluation.cost==0&&!s.Evaluation.values.ContainsKey("x"),"initial pause");
                Step(s);Require(s.NextLine==2&&Equals(s.Evaluation.values["x"],1)&&s.Evaluation.steps.Count==0,"assignment");
                Step(s);var piece=(ForgeMvp.Piece)s.Evaluation.values["p"];Require(s.NextLine==3&&s.Evaluation.cost==0&&s.Evaluation.ironConsumed==1&&piece.heat==0,"load only");
                Step(s);Require(s.NextLine==4&&piece.heat==7&&s.Evaluation.cost==0&&s.Evaluation.product==null,"heat only");
                Step(s);Require(s.Completed&&s.Evaluation.product!=null,"finish only");int receipts=s.Evaluation.steps.Count;Step(s);Require(receipts==s.Evaluation.steps.Count,"no duplicate finish");
            }
        });
        check("future runtime errors are not evaluated early",()=>{
            using(var s=Start("x = 1\nprint(missing)")){Step(s);Require(s.NextLine==2&&s.Evaluation.error==null,"future error");s.Advance().GetAwaiter().GetResult();Require(s.Completed&&s.Evaluation.errorLine==2&&!s.Evaluation.compileError,"error at step");}
        });
        check("blank comments and untaken branches do not consume steps",()=>{
            using(var s=Start("# comment\n\nx = 0\nif False:\n    1(1)\nelse:\n    x = 2\nprint(x)")){Require(s.NextLine==3,"skip blanks");Step(s);Require(s.NextLine==4,"condition");Step(s);Require(s.NextLine==7&&s.Evaluation.cost==0,"selected branch");Step(s);Step(s);Require(s.Completed&&s.Evaluation.steps.Single().text=="2","branch output");}
        });
        check("while header reappears on each iteration",()=>{
            using(var s=Start("n = 0\nwhile n < 2:\n    n = n + 1\nprint(n)")){var visited=new List<int>();while(!s.Completed){visited.Add(s.NextLine);Step(s);}Require(visited.SequenceEqual(new[]{1,2,3,2,3,2,4}),"while order");}
        });
        check("for continue and break preserve iteration state",()=>{
            using(var s=Start("for i in [1, 2, 3]:\n    if i == 1:\n        continue\n    print(i)\n    break\nprint('end')")){Drain(s);Require(s.Evaluation.error==null&&s.Evaluation.steps.Select(x=>x.text).SequenceEqual(new[]{"2","end"}),"flow");}
        });
        check("step into functions and resume assignment on return",()=>{
            using(var s=Start("def f(n):\n    local = n + 1\n    return local\nx = f(4)\nprint(x)")){Step(s);Require(s.NextLine==4,"definition");Step(s);Require(s.NextLine==2&&!s.Evaluation.values.ContainsKey("x"),"enter");Step(s);Require(s.NextLine==3,"local assignment");Step(s);Require(s.NextLine==5&&Equals(s.Evaluation.values["x"],5),"return resumes caller");Step(s);Require(s.Completed,"done");}
        });
        check("recursive calls retain independent paused frames",()=>{
            using(var s=Start("def fact(n):\n    if n <= 1:\n        return 1\n    return n * fact(n - 1)\nprint(fact(5))")){Drain(s);Require(s.Evaluation.error==null&&s.Evaluation.steps.Single().text=="120","recursion");}
        });
        check("import enters original module lines and initializes once",()=>{
            var modules=new Dictionary<string,string>{{"m","x = 7\ndef value():\n    return x"}};
            using(var s=Start("import m\nprint(m.value())\nimport m",modules)){
                Step(s);Require(s.NextModule=="m"&&s.NextLine==1,"module entry");Step(s);Require(s.NextModule=="m"&&s.NextLine==2,"module definition");Step(s);Require(s.NextModule==null&&s.NextLine==2,"caller restored");Step(s);Require(s.NextModule=="m"&&s.NextLine==3,"function entry");Step(s);Require(s.NextModule==null&&s.NextLine==3&&s.Evaluation.steps.Single().text=="7","return print");Step(s);Require(s.Completed,"cached import");
            }
        });
        check("stepped execution matches full costs output and random state",()=>{
            string code="print(random(1, 100))\n"+ForgeEconomyChecks.Basic;
            var full=ForgeMvp.Evaluate(code,100,10);using(var s=Start(code)){Drain(s);var e=s.Evaluation;Require(e.error==null&&e.cost==full.cost&&e.ironConsumed==full.ironConsumed&&e.steps.Select(x=>x.text).SequenceEqual(full.steps.Select(x=>x.text))&&ForgePricing.Quote(e.product).SalePrice==28,"equivalence");}
        });
        check("cancel preserves completed receipts and executes nothing later",()=>{
            var s=Start("p = 1(1)\n2(p, 7)\n!p");Step(s);s.Dispose();s.Advance().GetAwaiter().GetResult();Require(s.Cancelled&&s.Completed&&s.Evaluation.cost==0&&s.Evaluation.steps.Count==1&&s.Evaluation.product==null,"cancel");
        });
        check("compile failure creates completed zero-cost session",()=>{
            using(var s=Start("p = 1(1)\n2()")){Require(s.Completed&&s.Evaluation.compileError&&s.Evaluation.cost==0,"compile");}
        });
        check("error retains only prior successful process receipts",()=>{
            using(var s=Start("p = 1(1)\n2(p, 10)")){Step(s);s.Advance().GetAwaiter().GetResult();Require(s.Completed&&s.Evaluation.errorLine==2&&s.Evaluation.cost==0&&s.Evaluation.steps.Count==1,"partial error");}
        });
        check("recursion and command limits apply across separate clicks",()=>{
            using(var s=Start("def forever():\n    forever()\nforever()")){Drain(s);Require(s.Evaluation.errorMessage.Contains("32"),"depth");}
            using(var s=Start("while True:\n    x = 1")){Drain(s);Require(s.Evaluation.errorMessage.Contains("10000"),"commands");}
        });
        return log.AppendLine("TOTAL stepping "+count+" passed").ToString();
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Step Execution")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
