using System;
using System.Collections.Generic;

public enum BlueprintTokenKind { Name, Number, String, Comment, Symbol, NewLine, Indent, Dedent, End, Error }
public sealed class BlueprintToken
{
    public BlueprintTokenKind Kind;
    public string Raw;
    public int Start, Length, Line;
    public BlueprintFeature Feature;
    public BlueprintToken(BlueprintTokenKind kind, string raw, int start, int length, int line)
    { Kind=kind; Raw=raw; Start=start; Length=length; Line=line; }
}
public sealed class BlueprintException : Exception
{
    public int Line { get; }
    public string ModuleName { get; set; }
    public BlueprintException(int line, string message) : base(message) { Line=line; }
}
public static class BlueprintLexer
{
    // Preserve a strictly lexed prefix; never reinterpret invalid indentation or strings.
    public static List<BlueprintToken> ScanDeferred(string source)
    {
        source=source??"";
        try{return Scan(source);}
        catch(BlueprintException error){
            int start=0;
            for(int line=1;line<error.Line;line++){int next=source.IndexOf('\n',start);if(next<0)break;start=next+1;}
            var prefix=Scan(source.Substring(0,start));
            int tail=prefix.Count-1;
            while(tail>0&&prefix[tail-1].Kind==BlueprintTokenKind.Dedent)tail--;
            prefix.Insert(tail,new BlueprintToken(BlueprintTokenKind.Error,error.Message,start,0,error.Line));
            prefix.Insert(tail+1,new BlueprintToken(BlueprintTokenKind.NewLine,"\n",start,0,error.Line));
            return prefix;
        }
    }
    static bool IsSymbol(char c) => "()[]{}:,.=+-*/%<>!~?".IndexOf(c)>=0;
    public static List<BlueprintToken> Scan(string source, bool tolerant = false)
    {
        source = source ?? "";
        if (!tolerant && System.Text.Encoding.UTF8.GetByteCount(source)>65536) throw new BlueprintException(1,"도면은64KiB 이하여야 합니다.");
        var raw = new List<BlueprintToken>();
        int index=0, line=1;
        bool statementStart=true;
        while(index<source.Length)
        {
            char c=source[index];
            if(c=='\r' || c=='\n') {
                int start=index++; if(c=='\r' && index<source.Length && source[index]=='\n') index++;
                raw.Add(new BlueprintToken(BlueprintTokenKind.NewLine, "\n",start,index-start,line++)); statementStart=true; continue;
            }
            if(char.IsWhiteSpace(c)) { index++; continue; }
            int begin=index;
            BlueprintTokenKind kind;
            if(c=='#') { while(index<source.Length && source[index]!='\r' && source[index]!='\n') index++; kind=BlueprintTokenKind.Comment; }
            else if(c=='\'' || c=='"') {
                char quote=c; index++; bool closed=false;
                while(index<source.Length && source[index]!='\r' && source[index]!='\n') {
                    if(source[index]=='\\') { index++; if(index<source.Length && source[index]!='\r' && source[index]!='\n') index++; }
                    else if(source[index++]==quote) { closed=true; break; }
                }
                if(!closed && !tolerant) throw new BlueprintException(line,"문자열을 닫는 따옴표가 필요합니다.");
                kind=BlueprintTokenKind.String;
            }
            // Match the query before scanning its numeric prefix or the '?' symbol.
            else if(index+3<source.Length && source.Substring(index,4)=="777?") { index+=4; kind=BlueprintTokenKind.Symbol; }
            // Longest conditional symbol first: 01? must remain one raw token.
            else if(index+2<source.Length && source.Substring(index,3)=="01?") { index+=3; kind=BlueprintTokenKind.Symbol; }
            else if(index+1<source.Length && (source.Substring(index,2)=="1?" || source.Substring(index,2)=="0?")) { index+=2; kind=BlueprintTokenKind.Symbol; }
            else if(IsSymbol(c)) {
                index++;
                if(index<source.Length) {
                    string pair=source.Substring(begin,2);
                    if(pair=="!!"||pair=="~~"||pair=="<<"||pair=="<="||pair==">="||pair=="=="||pair=="!="||pair=="//") index++;
                }
                kind=BlueprintTokenKind.Symbol;
            }
            else {
                while(index<source.Length && !char.IsWhiteSpace(source[index]) && !IsSymbol(source[index]) && source[index]!='#' && source[index]!='\'' && source[index]!='"') index++;
                string text=source.Substring(begin,index-begin); bool number=text.Length>0;
                foreach(char digit in text) if(digit<'0'||digit>'9') { number=false; break; }
                kind=number ? BlueprintTokenKind.Number : BlueprintTokenKind.Name;
            }
            var token=new BlueprintToken(kind,source.Substring(begin,index-begin),begin,index-begin,line);
            if(kind!=BlueprintTokenKind.String && kind!=BlueprintTokenKind.Comment) {
                var alias=BlueprintAliases.Find(token.Raw);
                if(alias!=null) {
                    int next=index; while(next<source.Length && (source[next]==' '||source[next]=='\t')) next++;
                    bool call=next<source.Length && source[next]=='(';
                    if(alias.Syntax==BlueprintSyntaxKind.Builtin ? call :
                       token.Raw==alias.English || kind==BlueprintTokenKind.Symbol ||
                       (statementStart && (call || alias.Id==BlueprintFeature.Def))) token.Feature=alias.Id;
                }
                statementStart=false;
            }
            raw.Add(token);
        }
        // Legacy straight-line sources used Trim on every line. Only block programs require indentation.
        bool blocks=raw.Exists(t=>t.Feature==BlueprintFeature.If||t.Feature==BlueprintFeature.While||t.Feature==BlueprintFeature.For||t.Feature==BlueprintFeature.Def||t.Feature==BlueprintFeature.Else||t.Feature==BlueprintFeature.Elif);
        var output=new List<BlueprintToken>(); var levels=new Stack<int>(); levels.Push(0);
        bool first=true; int lineStart=0;
        foreach(var token in raw) {
            if(token.Kind==BlueprintTokenKind.NewLine) { output.Add(token); first=true; lineStart=token.Start+token.Length; continue; }
            if(first && token.Kind!=BlueprintTokenKind.Comment) {
                int indent=0;
                for(int pos=lineStart;pos<token.Start;pos++) {
                    if(source[pos]=='\t' && blocks && !tolerant) throw new BlueprintException(token.Line,"탭 대신 공백4칸을 사용하세요.");
                    indent+=source[pos]=='\t'?4:1;
                }
                if(!blocks) indent=0;
                if(blocks && indent%4!=0 && !tolerant) throw new BlueprintException(token.Line,"들여쓰기는 공백4칸 단위입니다.");
                if(indent>levels.Peek()) {
                    if((indent!=levels.Peek()+4 || levels.Count>=33) && !tolerant) throw new BlueprintException(token.Line,"블록 들여쓰기 또는 중첩 깊이가 잘못되었습니다.");
                    levels.Push(indent); output.Add(new BlueprintToken(BlueprintTokenKind.Indent,"",token.Start,0,token.Line));
                } else {
                    while(indent<levels.Peek()) { levels.Pop(); output.Add(new BlueprintToken(BlueprintTokenKind.Dedent,"",token.Start,0,token.Line)); }
                    if(indent!=levels.Peek() && !tolerant) throw new BlueprintException(token.Line,"들여쓰기 깊이가 이전 블록과 맞지 않습니다.");
                }
                first=false;
            }
            output.Add(token);
        }
        if(output.Count==0 || output[output.Count-1].Kind!=BlueprintTokenKind.NewLine) output.Add(new BlueprintToken(BlueprintTokenKind.NewLine,"\n",source.Length,0,line));
        while(levels.Count>1) { levels.Pop(); output.Add(new BlueprintToken(BlueprintTokenKind.Dedent,"",source.Length,0,line)); }
        output.Add(new BlueprintToken(BlueprintTokenKind.End,"",source.Length,0,line));
        return output;
    }
}
