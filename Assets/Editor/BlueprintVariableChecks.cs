using System;
using System.Linq;
using System.Text;
public static class BlueprintVariableChecks
{
    public const string Original="a = 1\n888(True):\n    1? a == 1:\n        res = 1(a)\n    01? a == 2:\n        2(res, 7)\n    01? a == 3:\n        3(res)\n    01? a == 4:\n        4(res, 0)\n    01? a == 5:\n        !!\n    a = a + 1\n!res";
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static ForgeMvp.Evaluation Good(string text){var r=ForgeMvp.Evaluate(text,100,10);Require(r.error==null,r.error);return r;}
    public static string Run()
    {
        int count=0;var log=new StringBuilder();Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("original staged while blueprint completes unchanged",()=>{var r=Good(Original);var q=ForgePricing.Quote(r.product);Require(r.cost==12&&r.ironConsumed==1&&q.SalePrice==28&&q.Profit==16&&r.steps.Count==5,"original result");});
        check("English and numeric staged loops agree",()=>{var r=Good(Original.Replace("888","while").Replace("01?","elif").Replace("1?","if").Replace("!!","break"));Require(r.cost==12&&r.product!=null,"English result");});
        check("selected if branch binding survives outside block",()=>{var r=Good("if True:\n    message = 'ready'\nprint(message)");Require(r.steps.Single().text=="ready","binding");});
        check("for body and iteration variable survive outside loop",()=>{var r=Good("for i in [6,7]:\n    target = i\n"+ForgeEconomyChecks.Basic.Replace("2(Blade, 7)","2(Blade, target)"));Require(Equals(r.values["i"],7)&&Equals(r.values["target"],7)&&r.cost==12,"for scope");});
        check("unexecuted paths do not read undefined variables",()=>{var r=Good("if False:\n    print(Missing)\nprint('ok')");Require(r.steps.Count==1&&r.steps[0].text=="ok","dead branch");r=Good("if False and Missing:\n    print('no')\nprint('ok')");Require(r.steps.Count==1,"short circuit");});
        check("missing branch binding fails at actual read line",()=>{var r=ForgeMvp.Evaluate("if False:\n    message = 'ready'\nprint(message)");Require(!r.compileError&&r.errorLine==3&&r.errorMessage.Contains("message")&&r.steps.Count==0,"read-time error");});
        check("zero iteration loop creates no binding",()=>{var r=ForgeMvp.Evaluate("for i in []:\n    message = 'ready'\nprint(message)");Require(!r.compileError&&r.errorLine==3&&r.errorMessage.Contains("message"),r.error);});
        check("read before later assignment still fails",()=>{var r=ForgeMvp.Evaluate("print(message)\nmessage = 'later'");Require(!r.compileError&&r.errorLine==1&&r.steps.Count==0,r.error);});
        check("failed read retains only prior successful production cost",()=>{var r=ForgeMvp.Evaluate(ForgeEconomyChecks.Basic.Replace("3(Blade)","3(Missing)"),100,10);Require(!r.compileError&&r.errorLine==4&&r.cost==12&&r.ironConsumed==1&&r.product==null&&r.steps.Count==2,r.error);});
        check("runtime variable types follow reassignment inside loop",()=>{var r=Good("res = None\n"+Original);Require(r.cost==12&&r.product!=null,"reassignment");var bad=ForgeMvp.Evaluate("value = 1\nprint(value)");Require(!bad.compileError&&bad.errorLine==2&&bad.errorMessage.Contains("문자열"),bad.error);});
        check("function block bindings remain local and calls are isolated",()=>{
            var r=Good("def make_message(flag):\n    if flag:\n        message = 'local'\n    return message\nprint(make_message(True))");Require(r.steps.Single().text=="local","local branch");
            r=ForgeMvp.Evaluate("def make_message(flag):\n    if flag:\n        message = 'local'\n    return message\nprint(make_message(True))\nprint(make_message(False))");Require(!r.compileError&&r.errorLine==4&&r.steps.Count==1,"fresh locals");
        });
        check("syntax and undefined function checks remain compile errors",()=>{foreach(string text in new[]{"print('before')\nunknown()","print('before')\nif True\n    print('x')","print('before')\nprint()"}){var r=ForgeMvp.Evaluate(text);Require(r.compileError&&r.steps.Count==0,r.error);}});
        log.AppendLine("TOTAL variables "+count+" passed");return log.ToString();
    }
    public static void Main(){Console.WriteLine(Run());BlueprintPrintChecks.Main();}
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Runtime Variables")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
