using System;
using System.Text;
public static class BlueprintIndentationChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static string Run()
    {
        int count=0;var log=new StringBuilder();Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
        check("split Shift Tab key and character preserve selected code",()=>{
            foreach(string source in new[]{"    first()\n        second()","\tfirst()\r\n\t\tsecond()","first()\nsecond()"})foreach(bool reverse in new[]{false,true}){
                string text=source;int cursor=reverse?text.Length:0,anchor=reverse?0:text.Length;
                for(int repeat=0;repeat<3;repeat++){
                    Require(BlueprintIndentation.TryTabEvent(text,cursor,anchor,true,'\0',true,out var key),"key consumed");
                    Require(BlueprintIndentation.TryTabEvent(key.Text,key.Cursor,key.Selection,false,'\t',false,out var character),"separate character consumed even without shift");
                    Require(character.Text==key.Text&&character.Cursor==key.Cursor&&character.Selection==key.Selection,"second event is a no-op");
                    Require(character.Text.Contains("first()")&&character.Text.Contains("second()"),"selected letters survive");
                    text=character.Text;cursor=character.Cursor;anchor=character.Selection;
                }
                Require(text==source.Replace("    ","").Replace("\t",""),"all text retained at zero indentation");
            }
        });
        check("Tab event variants indent once and leave normal typing native",()=>{
            const string source="a()\nb()";
            Require(BlueprintIndentation.TryTabEvent(source,0,source.Length,true,'\t',false,out var key),"combined key and character");
            Require(key.Text=="    a()\n    b()","single indent");
            Require(BlueprintIndentation.TryTabEvent(key.Text,key.Cursor,key.Selection,false,'\t',false,out var after)&&after.Text==key.Text,"no double indent or replacement");
            Require(!BlueprintIndentation.TryTabEvent(source,0,source.Length,false,'x',false,out _),"ordinary typing still reaches native editor");
        });
        check("literal tabs and four spaces execute identically including stepping",()=>{
            foreach(var header in new[]{"333 make():","def make():"}){
                string tabbed=header+"\n\tres=1(1)\n\t2(res)\n\t3(res)\n\t4(res,0)\n\t!res\nmake()";
                var a=ForgeMvp.Evaluate(tabbed);var b=ForgeMvp.Evaluate(tabbed.Replace("\t","    "));
                Require(a.error==null&&b.error==null&&a.steps.Count==b.steps.Count&&ForgePricing.Quote(a.product).SalePrice==28,"tabbed function");
                using(var session=BlueprintRuntime.StartSession(tabbed,100,10,deferErrors:true)){
                    for(int i=0;i<40&&!session.Completed;i++)session.Advance().GetAwaiter().GetResult();
                    Require(session.Completed&&session.Evaluation.error==null&&session.Evaluation.steps.Count==a.steps.Count,"tabbed stepping");
                }
            }
            var mixed=ForgeMvp.Evaluate("1?(True):\n\tprint(1)\n    print(2)\n\t1?(True):\n    \tprint(3)");
            Require(mixed.error==null&&mixed.steps.Count==3,"mixed equivalent levels");
        });
        check("selected tab and space lines outdent together in either direction",()=>{
            string input="\tfirst\r\n    second\r\n\t\tthird\r\nlast";
            int end=input.IndexOf("last",StringComparison.Ordinal);
            foreach(bool reverse in new[]{false,true}){
                var e=BlueprintIndentation.Tab(input,reverse?end:0,reverse?0:end,true);
                Require(e.Text=="first\r\nsecond\r\n    third\r\nlast","all selected lines move left and next line stays");
                Require((e.Cursor>e.Selection)==reverse,"selection direction preserved");
                e=BlueprintIndentation.Tab(e.Text,e.Cursor,e.Selection,true);
                Require(e.Text=="first\r\nsecond\r\nthird\r\nlast","repeated shift tab retains block selection");
            }
            var partial=BlueprintIndentation.Tab(" \t  x",5,5,true);Require(partial.Text=="    x","mixed indentation previous level");
        });
        check("Enter and Backspace honor tab indentation width",()=>{
            string line="\t1?(True):";Require(BlueprintIndentation.Enter(line,line.Length,line.Length).Text==line+"\n        ","nested enter");
            Require(BlueprintIndentation.Backspace("\t\tx",2,2,out var e)&&e.Text=="    x","tab backspace");
            Require(BlueprintIndentation.Tab("\tx",1,1,false).Text=="\t    x","tab visual column");
        });
        check("all English and numeric block headers indent four spaces",()=>{
            foreach(string h in new[]{"def():","def warm(p):","333 warm(p):","333 answer():","if True:","1?(True):","elif True:","01?(True):","else:","0?:","while True:","888(True):","for x in [1]:","123(x in [1]):"}){
                var e=BlueprintIndentation.Enter(h,h.Length,h.Length);Require(e.Text==h+"\n    "&&e.Cursor==e.Text.Length&&e.Selection==e.Cursor,h);
            }
        });
        check("nested block and ordinary line preserve indentation",()=>{
            string s="    if True:";var e=BlueprintIndentation.Enter(s,s.Length,s.Length);Require(e.Text==s+"\n        ","nested");
            s="        x = 1";e=BlueprintIndentation.Enter(s,s.Length,s.Length);Require(e.Text==s+"\n        ","ordinary");
        });
        check("comments strings and dictionary colons do not open blocks",()=>{
            foreach(string s in new[]{"# def():","x = \"if True:\"","x = {1:","print():"}){var e=BlueprintIndentation.Enter(s,s.Length,s.Length);Require(e.Text==s+"\n",s);}
            string h="if True: # 설명";Require(BlueprintIndentation.Enter(h,h.Length,h.Length).Text==h+"\n    ","trailing comment");
        });
        check("Enter replaces selection and preserves right-side source",()=>{
            var e=BlueprintIndentation.Enter("    abcXYZ",7,10);Require(e.Text=="    abc\n    "&&e.Cursor==e.Text.Length,"selection");
            e=BlueprintIndentation.Enter("    abcXYZ",7,7);Require(e.Text=="    abc\n    XYZ"&&e.Cursor==12,"split");
        });
        check("CRLF and Korean source offsets retained",()=>{
            string s="# 한글\r\n    값 = 1";var e=BlueprintIndentation.Enter(s,s.Length,s.Length);Require(e.Text==s+"\r\n    ","CRLF");
        });
        check("Tab inserts spaces to next four-column stop",()=>{
            var e=BlueprintIndentation.Tab("  x",2,2,false);Require(e.Text=="    x"&&e.Cursor==4,"partial");
            e=BlueprintIndentation.Tab("",0,0,false);Require(e.Text=="    "&&e.Cursor==4,"empty");
        });
        check("selected lines indent and outdent with reverse selection",()=>{
            string s="a\nb\nc";var e=BlueprintIndentation.Tab(s,3,0,false);Require(e.Text=="    a\n    b\nc"&&e.Cursor==11&&e.Selection==4,"reverse indent");
            e=BlueprintIndentation.Tab(e.Text,e.Cursor,e.Selection,true);Require(e.Text==s&&e.Cursor==3&&e.Selection==0,"reverse outdent");
            e=BlueprintIndentation.Tab(s,0,2,false);Require(e.Text=="    a\nb\nc","exclude next line start");
        });
        check("Shift Tab affects line indentation only",()=>{
            var e=BlueprintIndentation.Tab("      abc",9,9,true);Require(e.Text=="    abc"&&e.Cursor==7,"partial outdent");
            e=BlueprintIndentation.Tab("abc",2,2,true);Require(e.Text=="abc"&&e.Cursor==2,"root no-op");
        });
        check("Backspace removes one indent step but leaves normal text native",()=>{
            Require(BlueprintIndentation.Backspace("        x",8,8,out var e)&&e.Text=="    x"&&e.Cursor==4,"four spaces");
            Require(BlueprintIndentation.Backspace("      x",6,6,out e)&&e.Text=="    x","partial");
            Require(!BlueprintIndentation.Backspace("    x",5,5,out e),"normal text");
            Require(!BlueprintIndentation.Backspace("    x",4,0,out e),"selection native");
        });
        check("automatically indented function produces valid crafting source",()=>{
            string source="333 warm(p):";var e=BlueprintIndentation.Enter(source,source.Length,source.Length);source=e.Text+"2(p)";
            e=BlueprintIndentation.Enter(source,source.Length,source.Length);source=e.Text+"<< 100(p)";
            source+="\n"+ForgeEconomyChecks.Basic.Replace("2(Blade)","H = warm(Blade)");var r=ForgeMvp.Evaluate(source);Require(r.error==null&&r.cost==0&&Equals(r.values["H"],900),r.error);
        });
        check("split Return key and character input produces one newline",()=>{
            string text="def warm(p):";
            Require(!BlueprintIndentation.IndentNativeNewline(text,text,text.Length,text.Length,out var edit),"key-only must not insert");
            Require(BlueprintIndentation.IndentNativeNewline(text,text+"\n",text.Length,text.Length,out edit),"native newline recognized");
            Require(edit.Text==text+"\n    ","one newline with indent");
            Require(!BlueprintIndentation.IndentNativeNewline(edit.Text,edit.Text,edit.Cursor,edit.Cursor,out var unchanged),"repaint must not repeat");
            Require(!BlueprintIndentation.IndentNativeNewline("한","한글",1,1,out unchanged),"IME commit is not newline");
        });
        check("repeated Enter follows blank final line without doubling",()=>{
            string text="";float scroll=0;const float height=100,lineHeight=20;
            for(int i=1;i<=50;i++){
                Require(BlueprintIndentation.IndentNativeNewline(text,text+"\n",text.Length,text.Length,out var edit),"newline");text=edit.Text;
                Require(BlueprintViewport.Row(text,text.Length)==i,"exactly one row per Enter");
                float content=BlueprintViewport.ContentHeight(text,lineHeight,16);
                scroll=BlueprintViewport.FollowY(text,text.Length,lineHeight,8,height,content,scroll);
                float caret=8+i*lineHeight;Require(caret>=scroll&&caret+lineHeight<=scroll+height,"caret visible "+i);
            }
            Require(scroll>0,"viewport moved");
            Require(BlueprintViewport.FollowY(text,0,lineHeight,8,height,BlueprintViewport.ContentHeight(text,lineHeight,16),scroll)==4,"navigate back to first row");
        });
        check("CRLF native newline and already visible caret retain state",()=>{
            string text="# header\r\n    x = 1";
            Require(BlueprintIndentation.IndentNativeNewline(text,text+"\r\n",text.Length,text.Length,out var edit),"CRLF input");
            Require(edit.Text==text+"\r\n    ","one CRLF with indent");
            Require(BlueprintViewport.FollowY("a\nb",3,20,8,100,100,0)==0,"no unnecessary scroll");
        });
        log.AppendLine("TOTAL indentation "+count+" passed");return log.ToString();
    }
    public static void Main(){Console.WriteLine(Run());BlueprintLanguageChecks.Main();}
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Code Indentation")]
    public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
