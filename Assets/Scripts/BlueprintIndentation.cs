using System;
using System.Collections.Generic;

// Pure text edits: indices always refer to plain UTF-16 source, never display markup.
public static class BlueprintIndentation
{
    public const int Width=4;
    public struct Edit { public string Text; public int Cursor, Selection; public Edit(string text,int cursor,int selection){Text=text;Cursor=cursor;Selection=selection;} }
    static int LineStart(string text,int position){return position==0?0:text.LastIndexOf('\n',position-1)+1;}
    static Edit Replace(string text,int start,int end,string insert){string result=text.Substring(0,start)+insert+text.Substring(end);return new Edit(result,start+insert.Length,start+insert.Length);}
    public static Edit Enter(string text,int cursor,int selection)
    {
        int start=Math.Min(cursor,selection),end=Math.Max(cursor,selection),line=LineStart(text,start);
        string prefix=text.Substring(line,start-line);int spaces=0;
        for(int i=0;i<prefix.Length&&(prefix[i]==' '||prefix[i]=='\t');i++)spaces+=prefix[i]=='\t'?Width:1;
        var tokens=BlueprintLexer.Scan(prefix,true).FindAll(t=>t.Length>0&&t.Kind!=BlueprintTokenKind.Comment&&t.Kind!=BlueprintTokenKind.NewLine);
        if(tokens.Count>0&&tokens[tokens.Count-1].Raw==":"){
            var feature=tokens[0].Feature;
            if(feature==BlueprintFeature.Match||feature==BlueprintFeature.Case||feature==BlueprintFeature.Default||feature==BlueprintFeature.Def||feature==BlueprintFeature.If||feature==BlueprintFeature.Elif||feature==BlueprintFeature.Else||feature==BlueprintFeature.For||feature==BlueprintFeature.While)spaces+=Width;
        }
        string newline=text.Contains("\r\n")?"\r\n":"\n";
        return Replace(text,start,end,newline+new string(' ',spaces));
    }
    // Let TextArea own newline insertion. Only decorate its single completed edit.
    public static bool IndentNativeNewline(string before,string after,int cursor,int selection,out Edit edit)
    {
        edit=new Edit(after,0,0);
        int start=Math.Min(cursor,selection),end=Math.Max(cursor,selection);
        if(start<0||end>before.Length)return false;
        string left=before.Substring(0,start),right=before.Substring(end);
        if(after!=left+"\n"+right&&after!=left+"\r\n"+right)return false;
        edit=Enter(before,cursor,selection);return true;
    }
    // Windows may deliver a Tab key event followed by a separate tab character.
    // Only the key event edits indentation; the character must never replace a selection.
    public static bool TryTabEvent(string text,int cursor,int selection,bool tabKey,char character,bool shift,out Edit edit)
    {
        edit=new Edit(text,cursor,selection);
        if(tabKey){edit=Tab(text,cursor,selection,shift);return true;}
        return character=='\t';
    }
    public static Edit Tab(string text,int cursor,int selection,bool outdent)
    {
        int low=Math.Min(cursor,selection),high=Math.Max(cursor,selection);
        if(low==high&&!outdent){int column=0;for(int i=LineStart(text,cursor);i<cursor;i++)column+=text[i]=='\t'?Width:1;return Replace(text,cursor,cursor,new string(' ',Width-column%Width));}
        int first=LineStart(text,low),last=LineStart(text,high);
        if(high>low&&high==last)last=LineStart(text,high-1);
        var edits=new List<int[]>();
        for(int line=first;line<=last;){
            int characters=0,columns=0;
            while(line+characters<text.Length&&(text[line+characters]==' '||text[line+characters]=='\t')){columns+=text[line+characters]=='\t'?Width:1;characters++;}
            int remaining=Math.Max(0,columns-(columns%Width==0?Width:columns%Width));
            edits.Add(outdent?new[]{line,characters,remaining}:new[]{line,0,Width});
            int next=text.IndexOf('\n',line);if(next<0)break;line=next+1;
        }
        int mappedCursor=cursor,mappedSelection=selection;
        for(int i=edits.Count-1;i>=0;i--){var e=edits[i];text=text.Remove(e[0],e[1]).Insert(e[0],new string(' ',e[2]));mappedCursor=Map(mappedCursor,e);mappedSelection=Map(mappedSelection,e);}
        return new Edit(text,mappedCursor,mappedSelection);
    }
    static int Map(int position,int[] edit){if(position<edit[0])return position;return edit[0]+edit[2]+Math.Max(0,position-edit[0]-edit[1]);}
    public static bool Backspace(string text,int cursor,int selection,out Edit edit)
    {
        edit=new Edit(text,cursor,selection);if(cursor!=selection||cursor==0)return false;
        int line=LineStart(text,cursor);if(cursor==line)return false;
        int columns=0;for(int i=line;i<cursor;i++){if(text[i]!=' '&&text[i]!='\t')return false;columns+=text[i]=='\t'?Width:1;}
        int count=columns%Width;if(count==0)count=Width;
        edit=Replace(text,line,cursor,new string(' ',Math.Max(0,columns-count)));return true;
    }
}
