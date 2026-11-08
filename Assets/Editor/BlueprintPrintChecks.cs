using System;
using System.Linq;
using System.Text;
public static class BlueprintPrintChecks
{
    static void Require(bool test,string message){if(!test)throw new Exception(message);}
    public static string Run()
    {
        int count=0;var log=new StringBuilder();Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("print-only supports literal, variable, empty and escaped strings",()=>{
            var r=ForgeMvp.Evaluate("print(\"안녕하세요\")\nmessage = '문자열'\nprint(message)\nprint(\"\")\nprint(\"첫줄\\n둘째줄\")");
            Require(r.error==null&&r.debugOnly&&r.product==null&&r.cost==0&&r.ironConsumed==0,r.error);
            Require(r.steps.Select(s=>s.text).SequenceEqual(new[]{"안녕하세요","문자열","","첫줄\n둘째줄"})&&r.steps.All(s=>s.debugOutput),"output order");
        });
        check("branches loops and user functions print only executed paths",()=>{
            var r=ForgeMvp.Evaluate("333(say, text):\n    print(text)\n1?(False):\n    print('hidden')\n0?:\n    say('shown')\n123(i in [1,2]):\n    print('repeat')");
            Require(r.error==null&&r.steps.Select(s=>s.text).SequenceEqual(new[]{"shown","repeat","repeat"}),r.error);
        });
        check("print receipts preserve craft order and economics",()=>{
            string source="print('start')\n"+ForgeEconomyChecks.Basic.Replace("3(Blade)","print('hot')\n3(Blade)").Replace("!Blade","print('ready')\n!Blade");
            var r=ForgeMvp.Evaluate(source,100,10);Require(r.error==null&&r.cost==12&&r.ironConsumed==1&&ForgePricing.Quote(r.product).Profit==16,r.error);
            Require(r.steps.Select(s=>s.debugOutput?s.text:s.process.ToString()).SequenceEqual(new[]{"start","1","2","hot","3","4","ready","0"}),"ordered playback");
            Require(ForgeMvp.Evaluate(source,100,10).steps.Select(s=>s.text).SequenceEqual(r.steps.Select(s=>s.text)),"preview parity");
        });
        check("invalid argument count and type fail before printing",()=>{
            foreach(string source in new[]{"print()","print(1)","print('a','b')"}){var r=ForgeMvp.Evaluate("print('earlier')\n"+source);Require(r.compileError&&r.steps.Count==0&&r.errorLine==2,r.error);}
        });
        check("dynamic type failure preserves only earlier messages",()=>{
            var r=ForgeMvp.Evaluate("def say(x):\n    print(x)\nprint('before')\nsay(1)\nprint('after')");Require(r.error!=null&&!r.compileError&&r.errorLine==2&&r.steps.Count==1&&r.steps[0].text=="before",r.error);
        });
        check("prints cannot bypass missing completion for crafting",()=>{
            var r=ForgeMvp.Evaluate("print('start')\nBlade = 1(1)");Require(r.compileError&&r.steps.Count==0&&r.errorMessage.Contains("!Blade"),r.error);
        });
        check("failure excludes later prints and keeps previous production cost",()=>{
            var r=ForgeMvp.Evaluate("print('start')\nBlade = 1(1)\n3(Blade)\nprint('after')\n!Blade");Require(r.error!=null&&!r.compileError&&r.cost==10&&r.product==null&&r.steps.Count(s=>s.debugOutput)==1,r.error);
        });
        check("print is yellow with tooltip; strings and comments stay opaque",()=>{
            const string source="print('print(<color=red>text</color>)') # print()";
            Require(ForgeSyntax.Scan(source).Count(t=>t.kind==ForgeSyntax.Kind.Function)==1,"function color");var tokens=BlueprintLexer.Scan(source);Require(tokens.Count(t=>t.Feature==BlueprintFeature.Print)==1,"opaque");
            Require(BlueprintHelp.Describe(source,tokens[0]).Contains("디버그 출력"),"tooltip");var r=ForgeMvp.Evaluate(source);Require(r.steps[0].text=="print(<color=red>text</color>)","raw output");
        });
        check("bounded output and buffer clearing",()=>{
            var r=ForgeMvp.Evaluate("888(True):\n    print('x')");Require(r.error!=null&&r.steps.Count==256&&r.cost==0,"run cap");
            var buffer=new ForgeDebugOutput();for(int i=0;i<300;i++)buffer.Append(i.ToString());Require(buffer.Count==256&&buffer.Text.StartsWith("44\n"),"rolling buffer");
            buffer.Clear();Require(buffer.Count==0&&buffer.Text=="","clear");buffer.Append("<b>한글</b>");Require(buffer.Text=="<b>한글</b>","literal text");
        });
        log.AppendLine("TOTAL print "+count+" passed");return log.ToString();
    }
    public static void Main(){Console.WriteLine(Run());BlueprintIndentationChecks.Main();}
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Print Output")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
