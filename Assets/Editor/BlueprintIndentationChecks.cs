using System;
using System.Text;
public static class BlueprintIndentationChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static string Run()
    {
        int count=0;var log=new StringBuilder();Action<string,Action> check=(name,test)=>{test();count++;log.AppendLine("PASS "+name);};
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
            string source="333 warm(p):";var e=BlueprintIndentation.Enter(source,source.Length,source.Length);source=e.Text+"2(p, 7)";
            e=BlueprintIndentation.Enter(source,source.Length,source.Length);source=e.Text+"<< 100(p)";
            source+="\n"+ForgeEconomyChecks.Basic.Replace("2(Blade, 7)","H = warm(Blade)");var r=ForgeMvp.Evaluate(source);Require(r.error==null&&r.cost==0&&Equals(r.values["H"],7),r.error);
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
