using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
public static class BlueprintLanguageChecks
{
    static void Require(bool yes,string message){if(!yes)throw new Exception(message);}
    static ForgeMvp.Evaluation Good(string prefix){var r=ForgeMvp.Evaluate(prefix+"\n"+ForgeEconomyChecks.Basic,100,10,"test",123);Require(r.error==null,r.error);return r;}
    public static string Run()
    {
        var log=new StringBuilder();int count=0;
        Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("16 aliases have unique stable IDs and complete metadata",()=>{Require(BlueprintAliases.All.Count==16,"count");Require(BlueprintAliases.All.Select(a=>a.Id).Distinct().Count()==16,"IDs");foreach(var a in BlueprintAliases.All)Require(a.Tooltip.Contains(a.English)&&a.Tooltip.Contains(a.Symbol),"tooltip");});
        check("all builtin aliases share result and source remains intact",()=>{
            string english="v = [9, 7, 5]\na = abs(-3)\nb = len(v)\nc = min(v)\nd = max(v)\ne = random(1, 7)\nf = set()";
            string numeric="v = [9, 7, 5]\na = 101(-3)\nb = 1111(v)\nc = 000(v)\nd = 999(v)\ne = 66(1, 7)\nf = 1010()";
            var a=Good(english);var b=Good(numeric);foreach(string key in new[]{"a","b","c","d","e"})Require(Equals(a.values[key],b.values[key]),key);
            Require((int)b.values["a"]==3&&(int)b.values["c"]==5&&((HashSet<int>)b.values["f"]).Count==0,"builtins");
            var h=ForgeMvp.Evaluate(ForgeEconomyChecks.Basic.Replace("3(Blade)","H = heat(Blade)\nN = 100(Blade)\n3(Blade)"));Require(h.error==null&&Equals(h.values["H"],7)&&Equals(h.values["N"],7),h.error);
        });
        const string choose="def choose(values, limit):\n    selected = None\n    for candidate in values:\n        if candidate <= limit:\n            selected = candidate\n            break\n    return selected\nTarget = choose([9, 7, 5], 7)\nif Target != None:\n    Blade = 1(1)\n    2(Blade, Target)\n    3(Blade)\n    4(Blade, 0)\n    !Blade\nelse:\n    skipped = True";
        check("function/for/if/else/break/return aliases produce identical craft",()=>{
            var a=ForgeMvp.Evaluate(choose);var b=ForgeMvp.Evaluate(choose.Replace("def ","333 ").Replace("for candidate in values:","123(candidate in values):").Replace("if candidate <= limit:","1?(candidate <= limit):").Replace("if Target != None:","1?(Target != None):").Replace("break","!!").Replace("return ","<< ").Replace("else:","0?:"));
            Require(a.error==null&&b.error==null,a.error+" / "+b.error);Require(a.cost==12&&b.cost==12&&b.steps.Count(x=>x.process==2)==1,"cost");Require(Equals(b.values["Target"],7),"chosen");
        });
        check("no suitable candidate skips with zero cost",()=>{var r=ForgeMvp.Evaluate(choose.Replace("[9, 7, 5]","[9, 8]"));Require(r.error==null&&r.skipped&&r.cost==0&&r.product==null,r.error);});
        check("nested loops scope break/continue and function return",()=>{
            string prefix="333(calc, n):\n    total = 0\n    123(i in [1, 2, 3]):\n        123(j in [1, 2, 3]):\n            1?(j == 1):\n                ~~\n            1?(j == 3):\n                !!\n            total = total + i\n        1?(i == n):\n            << total\n    << -1\na = calc(2)\nb = calc(3)\ni = 0\nc = 0\n888(i < 5):\n    i = i + 1\n    1?(i == 2):\n        ~~\n    1?(i == 4):\n        !!\n    c = c + i";
            var r=Good(prefix);Require(Equals(r.values["a"],3)&&Equals(r.values["b"],6)&&Equals(r.values["c"],4),"scope");
            var english=Good(prefix.Replace("333(calc, n):","def calc(n):").Replace("123(i in [1, 2, 3]):","for i in [1, 2, 3]:").Replace("123(j in [1, 2, 3]):","for j in [1, 2, 3]:").Replace("1?","if").Replace("~~","continue").Replace("!!","break").Replace("<<","return").Replace("888","while"));Require(Equals(r.values["c"],english.values["c"]),"while parity");
        });
        check("literal and opaque text contexts; whole identifiers",()=>{var r=Good("Value = 100\nOther = 888\nZero = 000\nshift = 3\ntext = \"100(Blade) # !! << 000(v)\"\n# 888(True): ~~");Require(Equals(r.values["Value"],100)&&Equals(r.values["Other"],888)&&Equals(r.values["Zero"],0),"literal");var tokens=BlueprintLexer.Scan("Value = 100\ntext = \"!! 000(v)\" # 1?\nshift = 888");Require(tokens.All(t=>t.Feature==BlueprintFeature.None),"opaque");});
        check("longest compound tokens distinguish completion/comparisons",()=>{var tokens=BlueprintLexer.Scan("!! ~~ << 1? 0? !Blade != <= >=",true);Require(tokens.Count(t=>t.Feature!=BlueprintFeature.None)==5,"compound");Require(tokens.Any(t=>t.Raw=="!"&&t.Feature==BlueprintFeature.None)&&tokens.Any(t=>t.Raw=="!="),"finish");});
        check("tolerant highlighting exposes exact raw spans without markup",()=>{string source="a = 1\n100(Blade)\n000([1])\n<< x\n1?(\n# !!\nx = \"66(\"";var tokens=ForgeSyntax.Scan(source);Require(tokens.Count(t=>t.kind==ForgeSyntax.Kind.Reserved)==2,"purple");Require(tokens.Any(t=>source.Substring(t.start,t.length)=="1"&&t.kind==ForgeSyntax.Kind.Material),"pink assignment");foreach(var t in tokens)Require(t.start>=0&&t.start+t.length<=source.Length,"span");});
        check("UTF8 roundtrip preserves zero padding, symbols, CRLF and Korean",()=>{string raw="# 한글\r\na = 000([1,2])\r\n!! ~~ << 1? 0?";var bytes=Encoding.UTF8.GetBytes(raw);Require(Encoding.UTF8.GetString(bytes)==raw,"roundtrip");});
        check("flat integer collections, copy semantics and membership",()=>{var r=Good("a = [1, 2]\nb = a\nb[0] = 8\nd = {1: 3}\nd[2] = 4\ns = {0, 0, 1}\nt = (6, 7)\nallowed = 0 in s\nsize = 1111(s)\nlo = 000(t)\nhi = 999(t)");Require(((List<int>)r.values["a"])[0]==1&&Equals(r.values["size"],2)&&Equals(r.values["allowed"],true)&&Equals(r.values["lo"],6)&&Equals(r.values["hi"],7),"collections");});
        check("compile errors have original line and no receipts",()=>{
            foreach(var input in new[]{"!!", "~~", "<< 1", "x = missing(1)", "x = 101()", "1?(True):\n  x = 1", "333 f():\n    !!", "333 f():\n    << f()"}){var r=ForgeMvp.Evaluate(input+"\n"+ForgeEconomyChecks.Basic);Require(r.compileError&&r.errorLine>0&&r.steps.Count==0,input+": "+r.error);}
        });
        check("runtime errors retain prior cost and precise source line",()=>{var r=ForgeMvp.Evaluate("Blade = 1(1)\nd = {}\nx = d[4]\n2(Blade, 7)\n3(Blade)\n4(Blade, 0)\n!Blade");Require(!r.compileError&&r.errorLine==3&&r.cost==10&&r.product==null,r.error);});
        check("while budget stops without registering finished item",()=>{var r=ForgeMvp.Evaluate("Blade = 1(1)\n888(True):\n    x = 1\n2(Blade, 7)\n3(Blade)\n4(Blade, 0)\n!Blade");Require(!r.compileError&&r.errorMessage.Contains("10000")&&(r.errorLine==2||r.errorLine==3)&&r.cost==10&&r.product==null,r.error);});
        check("bad runtime arguments fail and English/numeric errors match",()=>{foreach(var pair in new[]{new[]{"abs(-2147483648)","101(-2147483648)"},new[]{"min([])","000([])"},new[]{"random(7,1)","66(7,1)"}}){var a=ForgeMvp.Evaluate("x = "+pair[0]+"\n"+ForgeEconomyChecks.Basic);var b=ForgeMvp.Evaluate("x = "+pair[1]+"\n"+ForgeEconomyChecks.Basic);Require(a.error!=null&&a.error==b.error,a.error+" / "+b.error);}});
        check("deterministic inclusive random and full integer range",()=>{var a=Good("x = 66(-2147483648, 2147483647)\ny = 66(7,7)");var b=Good("x = random(-2147483648, 2147483647)\ny = random(7,7)");Require(Equals(a.values["x"],b.values["x"])&&Equals(a.values["y"],7),"seed");var endpoints=new HashSet<int>();for(uint seed=0;seed<100;seed++){var r=ForgeMvp.Evaluate("x = 66(1,7)\n"+ForgeEconomyChecks.Basic,100,10,"",seed);endpoints.Add((int)r.values["x"]);}Require(endpoints.Contains(1)&&endpoints.Contains(7)&&endpoints.Count==7,"inclusive");});
        check("function processes share ledger and Piece identity",()=>{var r=Good("333 warm(p):\n    2(p, 7)\n    << 100(p)");Require(r.cost==12,"unused function has no cost");var text=ForgeEconomyChecks.Basic.Replace("2(Blade, 7)","H = warm(Blade)");r=ForgeMvp.Evaluate("def warm(p):\n    2(p, 7)\n    return heat(p)\n"+text);Require(r.error==null&&r.cost==12&&Equals(r.values["H"],7),r.error);});
        check("contextual tooltip distinguishes heat targets and random bounds",()=>{foreach(var item in new[]{new[]{"2(Blade, 7)","목표 열 단계"},new[]{"66(1, 7)","난수 범위의 최댓값"},new[]{"random(1, 7)","난수 범위의 최댓값"}}){var token=BlueprintLexer.Scan(item[0]).Last(t=>t.Kind==BlueprintTokenKind.Number);Require(BlueprintHelp.Describe(item[0],token).Contains(item[1]),item[0]);}});
        check("source file save/read preserves raw tokens and executable meaning",()=>{string raw="x = 000([3,1])\r\n# 한글 !! ~~ <<\r\n"+choose.Replace("return ","<< ").Replace("break","!!");string path=System.IO.Path.GetTempFileName();try{System.IO.File.WriteAllText(path,raw,new UTF8Encoding(false));string loaded=System.IO.File.ReadAllText(path,Encoding.UTF8);Require(raw==loaded,"source bytes");Require(BlueprintLexer.Scan(loaded).Any(t=>t.Raw=="000"&&t.Feature==BlueprintFeature.Min),"raw min");var r=ForgeMvp.Evaluate(loaded);Require(r.error==null&&r.cost==12,r.error);}finally{System.IO.File.Delete(path);}});
        check("call-depth, collection and dynamic parameter limits",()=>{
            var source=new StringBuilder();for(int i=0;i<33;i++)source.Append("def f"+i+"():\n    return "+(i==32?"1":"f"+(i+1)+"()")+"\n");source.Append(ForgeEconomyChecks.Basic);var r=ForgeMvp.Evaluate(source.ToString());Require(r.compileError&&r.errorMessage.Contains("32"),r.error);
            r=ForgeMvp.Evaluate("x = ["+string.Join(",",Enumerable.Repeat("1",1025))+"]\n"+ForgeEconomyChecks.Basic);Require(r.error!=null&&r.cost==0&&r.errorMessage.Contains("1024"),r.error);
            r=ForgeMvp.Evaluate("def f(n):\n    return abs(n)\nx = f(True)\n"+ForgeEconomyChecks.Basic);Require(r.error!=null&&!r.compileError&&r.errorLine==2,r.error);
        });
        check("elif aliases select only first true branch and preserve fallback",()=>{
            foreach(int n in new[]{0,1,2,3}){
                string prefix="n = "+n+"\nif n == 0:\n    chosen = 10\nelif n == 1:\n    chosen = 20\nelif n <= 2:\n    chosen = 30\nelse:\n    chosen = 40";
                var english=Good(prefix);var numeric=Good(prefix.Replace("elif ","01? ").Replace("if ","1? ").Replace("else:","0?:"));
                Require(Equals(english.values["chosen"],10*(n+1))&&Equals(english.values["chosen"],numeric.values["chosen"]),"branch "+n);
            }
            var r=Good("x = 0\n1?(True):\n    x = 1\n01?(1 // 0 == 0):\n    x = 2");Require(Equals(r.values["x"],1),"skipped condition");
            r=Good("x = 0\nif False:\n    x = 1\n01?(True):\n    if False:\n        x = 2\n    elif True:\n        x = 3\n    else:\n        x = 4\nelse:\n    x = 5");Require(Equals(r.values["x"],3),"nested mixed");
        });
        check("elif token, purple span, tooltip and opaque text",()=>{
            string raw="01?(True):\n    x = 01\ns = \"01? elif\" # 01?\nelifValue = 1";
            var tokens=BlueprintLexer.Scan(raw,true);var token=tokens.Single(t=>t.Feature==BlueprintFeature.Elif);Require(token.Raw=="01?"&&token.Length==3,"compound");
            Require(ForgeSyntax.Scan(raw).Any(t=>t.start==0&&t.length==3&&t.kind==ForgeSyntax.Kind.Reserved),"purple");
            Require(BlueprintHelp.Describe(raw,token).Contains("01? ↔ elif"),"tooltip");
            Require(BlueprintLexer.Scan("01?(",true)[0].Feature==BlueprintFeature.Elif,"incomplete");
        });
        check("orphan, after-else and malformed elif report original line",()=>{
            foreach(var item in new[]{new[]{"01?(True):\n    x = 1","1"},new[]{"if True:\n    x = 1\nelse:\n    x = 2\nelif True:\n    x = 3","5"},new[]{"if False:\n    x = 1\n01?(True)\n    x = 2","3"}}){var r=ForgeMvp.Evaluate(item[0]+"\n"+ForgeEconomyChecks.Basic);Require(r.compileError&&r.errorLine==int.Parse(item[1])&&r.cost==0,r.error);}
        });
        check("function calls yellow, control keywords purple",()=>{
            string raw="def warm(p):\n    return heat(p)\nx = 100(Blade)\ny = warm(Blade)\nz = abs(-3)\n333(cool, p):\n    << p\nif True and not False:\n    x = 1";
            var tokens=ForgeSyntax.Scan(raw);
            foreach(string name in new[]{"warm","heat","100","abs","cool"})Require(tokens.Where(t=>raw.Substring(t.start,t.length)==name).All(t=>t.kind==ForgeSyntax.Kind.Function),name);
            foreach(string name in new[]{"def","return","333","<<","if","True","and","not","False"})Require(tokens.Any(t=>raw.Substring(t.start,t.length)==name&&t.kind==ForgeSyntax.Kind.Reserved),name);
        });
        check("parenthesized numeric definition retained; leading colon rejected",()=>{
            foreach(string header in new[]{"333 warm(p, target):","333(warm, p, target):","def warm(p, target):"}){
                var r=ForgeMvp.Evaluate(header+"\n    2(p, target)\n    << 100(p)\n"+ForgeEconomyChecks.Basic.Replace("2(Blade, 7)","H = warm(Blade, 7)"));
                Require(r.error==null&&r.cost==12&&Equals(r.values["H"],7),header+": "+r.error);
            }
            var bad=ForgeMvp.Evaluate("333: answer()\n    << 1\n"+ForgeEconomyChecks.Basic);Require(bad.compileError&&bad.cost==0,"leading colon rejected");
        });
        log.AppendLine("TOTAL language "+count+" passed");return log.ToString();
    }
    public static void Main(){Console.WriteLine(Run());Console.WriteLine(ForgeDiagnosticChecks.Run());Console.WriteLine(ForgeEconomyChecks.Run());}
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Blueprint Language")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run()+ForgeDiagnosticChecks.Run()+ForgeEconomyChecks.Run());}
#endif
}
