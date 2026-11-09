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
            var r=ForgeMvp.Evaluate("333 say(text):\n    print(text)\n1?(False):\n    print('hidden')\n0?:\n    say('shown')\n123(i in [1,2]):\n    print('repeat')");
            Require(r.error==null&&r.steps.Select(s=>s.text).SequenceEqual(new[]{"shown","repeat","repeat"}),r.error);
        });
        check("print receipts preserve craft order and economics",()=>{
            string source="print('start')\n"+ForgeEconomyChecks.Basic.Replace("3(Blade)","print('hot')\n3(Blade)").Replace("!Blade","print('ready')\n!Blade");
            var r=ForgeMvp.Evaluate(source,100,10);Require(r.error==null&&r.cost==0&&r.ironConsumed==1&&ForgePricing.Quote(r.product).Profit==28,r.error);
            Require(r.steps.Select(s=>s.debugOutput?s.text:s.process.ToString()).SequenceEqual(new[]{"start","1","2","hot","3","4","ready","0"}),"ordered playback");
            Require(ForgeMvp.Evaluate(source,100,10).steps.Select(s=>s.text).SequenceEqual(r.steps.Select(s=>s.text)),"preview parity");
        });
        check("print accepts zero and multiple typed arguments",()=>{
            var r=ForgeMvp.Evaluate("value = 7\nprint()\nprint('value:', value, -3, True, False, None)\nprint('', 'x', '')");
            Require(r.error==null&&r.debugOnly&&r.cost==0,r.error);
            Require(r.steps.Select(x=>x.text).SequenceEqual(new[]{"","value: 7 -3 True False None"," x "}),"typed output");
        });
        check("collections use readable Python delimiters",()=>{
            var r=ForgeMvp.Evaluate("print([1, 2], (3,), (), {2, 1}, set(), {1: 2}, [], {})");
            Require(r.error==null,r.error);
            Require(r.steps[0].text=="[1, 2] (3,) () {1, 2} set() {1: 2} [] {}",r.steps[0].text);
        });
        check("function parameters and return values print dynamically",()=>{
            var r=ForgeMvp.Evaluate("def say(x):\n    print(x)\n    return x\nprint(say(1), say(True))");
            Require(r.error==null&&r.steps.Select(x=>x.text).SequenceEqual(new[]{"1","True","1 True"}),r.error);
        });
        check("failed argument preserves earlier messages without partial output",()=>{
            var r=ForgeMvp.Evaluate("print('before')\nprint('partial', 1 // 0)\nprint('after')");
            Require(r.error!=null&&!r.compileError&&r.errorLine==2&&r.steps.Count==1&&r.steps[0].text=="before",r.error);
        });
        check("workpiece output snapshots state without production effects",()=>{
            var r=ForgeMvp.Evaluate(ForgeEconomyChecks.Basic.Replace("2(Blade, 7)","print(Blade)\n2(Blade, 7)").Replace("!Blade","print(Blade)\n!Blade"));
            var messages=r.steps.Where(x=>x.debugOutput).Select(x=>x.text).ToArray();
            Require(r.error==null&&r.cost==0&&messages.Length==2,r.error);
            Require(messages[0].Contains("성형=False")&&messages[1].Contains("성형=True"),"piece snapshots");
        });
        check("combined arguments obey character cap atomically",()=>{
            string value=new string('x',33000);
            var r=ForgeMvp.Evaluate("value = '"+value+"'\nprint('before')\nprint(value, value)");
            Require(r.error!=null&&!r.compileError&&r.steps.Count==1&&r.errorMessage.Contains("출력 한도"),r.error);
        });
        check("prints and crafting run without submission",()=>{
            var r=ForgeMvp.Evaluate("print('start')\nBlade = 1(1)");Require(r.error==null&&r.steps.Count==2&&r.product==null&&r.cost==0,r.error);
        });
        check("cold strike permits later prints and zero-value submission",()=>{
            var r=ForgeMvp.Evaluate("print('start')\nBlade = 1(1)\n3(Blade)\nprint('after')\n!Blade");Require(r.error==null&&r.cost==0&&r.product!=null&&ForgePricing.Quote(r.product).SalePrice==0&&r.steps.Count(s=>s.debugOutput)==2,r.error);
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
