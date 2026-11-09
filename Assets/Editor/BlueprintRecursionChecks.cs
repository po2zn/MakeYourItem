using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
public static class BlueprintRecursionChecks
{
    const string Factorial="def factorial(n):\n    if n <= 1:\n        return 1\n    return n * factorial(n - 1)";
    const string Countdown="def down(n):\n    if n == 0:\n        return 0\n    return down(n - 1)";
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static ForgeMvp.Evaluation Good(string source,IReadOnlyDictionary<string,string> modules=null)
    {var r=ForgeMvp.Evaluate(source,100,10,modules:modules);Require(r.error==null,r.error);return r;}
    public static string Run()
    {
        int count=0;var log=new StringBuilder();Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("direct recursion and numeric aliases",()=>{
            foreach(var source in new[]{Factorial,Factorial.Replace("def ","333 ").Replace("if ","1? ").Replace("return ","<< ")})Require(Good(source+"\nprint(factorial(5), factorial(0))").steps.Single().text=="120 1","factorial");
        });
        check("mutual recursion",()=>{
            var r=Good("def even(n):\n    if n == 0:\n        return True\n    return odd(n - 1)\ndef odd(n):\n    if n == 0:\n        return False\n    return even(n - 1)\nprint(even(10), odd(9), even(9))");Require(r.steps.Single().text=="True True False","mutual");
        });
        check("locals and caller source restored while unwinding",()=>{
            var r=Good("def visit(n):\n    saved = n\n    if n > 0:\n        visit(n - 1)\n    print(saved)\nvisit(3)\nprint('done')");Require(r.steps.Select(s=>s.text).SequenceEqual(new[]{"0","1","2","3","done"})&&r.steps.Take(4).All(s=>s.line==5)&&r.steps.Last().line==7,"frames");
        });
        check("unused recursive and long call graphs compile",()=>{
            Require(Good("def forever():\n    return forever()\nprint('ok')").steps.Single().text=="ok","unused recursion");
            var source=new StringBuilder();for(int i=0;i<40;i++)source.Append("def f"+i+"():\n    return "+(i==39?"1":"f"+(i+1)+"()")+"\n");source.Append("print('ok')");Require(Good(source.ToString()).steps.Single().text=="ok","unused chain");
        });
        check("32 active frames succeed; 33 fail at runtime",()=>{
            Require(Good(Countdown+"\nprint(down(31), down(31))").steps.Single().text=="0 0","limit and unwind");
            var r=ForgeMvp.Evaluate(Countdown+"\nprint(down(32))");Require(!r.compileError&&r.errorLine==4&&r.errorMessage.Contains("32")&&r.steps.Count==0,"depth error");
        });
        check("unbounded recursion preserves prior costs and stops",()=>{
            var r=ForgeMvp.Evaluate("def forever():\n    return forever()\np = 1(1)\nforever()\n!p",100,10);Require(!r.compileError&&r.errorLine==2&&r.cost==0&&r.ironConsumed==1&&r.product==null&&r.steps.Count==1,"bounded failure");
        });
        check("recursive forging emits every successful action once",()=>{
            var r=Good("def hammer(p, count):\n    if count == 0:\n        return p\n    3(p)\n    return hammer(p, count - 1)\np = 1(1)\n2(p, 7)\nres = hammer(p, 3)\n4(res, 0)\n!res");Require(r.cost==0&&r.ironConsumed==1&&ForgePricing.Quote(r.product).SalePrice==28&&r.steps.Select(s=>s.process).SequenceEqual(new[]{1,2,3,3,3,4,0})&&r.steps.Select(s=>s.pieceId).Distinct().Count()==1,"craft receipts");
        });
        check("imported recursive functions retain globals and aliases",()=>{
            var modules=new Dictionary<string,string>{{"maths","BASE = 1\ndef fact(n):\n    if n <= 1:\n        return BASE\n    return n * fact(n - 1)"}};
            var r=Good("BASE = 9\nfrom maths import fact as calculate\nprint(calculate(5))\nimport maths\nf = maths.fact\nprint(f(4))",modules);Require(r.steps.Select(s=>s.text).SequenceEqual(new[]{"120","24"}),"import recursion");
        });
        check("cross module mutual recursion uses lazy imports",()=>{
            var modules=new Dictionary<string,string>{{"a","def even(n):\n    if n == 0:\n        return True\n    from b import odd\n    return odd(n - 1)"},{"b","def odd(n):\n    if n == 0:\n        return False\n    from a import even\n    return even(n - 1)"}};
            Require(Good("import a\nprint(a.even(10), a.even(9))",modules).steps.Single().text=="True False","module recursion");
        });
        check("recursive module errors preserve module and original line",()=>{
            var r=ForgeMvp.Evaluate("import m\nm.forever()",modules:new Dictionary<string,string>{{"m","def forever():\n    return forever()"}});Require(!r.compileError&&r.errorModule=="m"&&r.errorLine==2&&r.errorMessage.Contains("32"),"module error");
        });
        check("branching recursion shares command budget",()=>{
            var r=ForgeMvp.Evaluate("def fib(n):\n    if n <= 1:\n        return n\n    return fib(n - 1) + fib(n - 2)\nfib(20)");Require(!r.compileError&&r.errorMessage.Contains("10000"),"budget");
        });
        check("arithmetic overflow remains a controlled recursive error",()=>{
            var r=ForgeMvp.Evaluate(Factorial+"\nfactorial(13)");Require(!r.compileError&&r.errorLine==4&&r.errorMessage.Contains("정수 범위"),"overflow");
        });
        return log.AppendLine("TOTAL recursion "+count+" passed").ToString();
    }
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Blueprint Recursion")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
