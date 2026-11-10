using System.Collections.Generic;
using System.Text.RegularExpressions;

// All spans point into unmodified source. The lexer is shared with execution.
public static class ForgeSyntax
{
    public enum Kind { Plain, Comment, Material, Keyword, Function, Reserved, Blacksmith }
    public struct Token
    {
        public int start, length;
        public Kind kind;
        public Token(int start,int length,Kind kind){this.start=start;this.length=length;this.kind=kind;}
    }
    public static List<Token> Scan(string source)
    {
        var result=new List<Token>();
        var tokens=BlueprintLexer.Scan(source,true);
        bool definitionNamePending=false;
        foreach(var token in tokens)
        {
            if(token.Kind==BlueprintTokenKind.NewLine){definitionNamePending=false;continue;}
            if(token.Length==0)continue;
            var kind=Kind.Plain;
            if(token.Kind==BlueprintTokenKind.Comment)kind=Kind.Comment;
            else if(ForgeSmithLanguage.IsSmith(token.Feature))kind=Kind.Blacksmith;
            else if(token.Feature!=BlueprintFeature.None)kind=BlueprintAliases.Find(token.Feature).Syntax==BlueprintSyntaxKind.Builtin?Kind.Function:Kind.Reserved;
            else if(token.Kind==BlueprintTokenKind.Name&&PythonIdentifier.IsKeyword(token.Raw))kind=Kind.Reserved;
            else if(token.Raw=="!")kind=Kind.Keyword;
            else if(token.Kind==BlueprintTokenKind.Name)
            {
                int next=token.Start+token.Length;while(next<source.Length&&(source[next]==' '||source[next]=='\t'))next++;
                if(definitionNamePending||next<source.Length&&source[next]=='(')kind=Kind.Function;
                definitionNamePending=false;
            }
            else if(token.Kind==BlueprintTokenKind.Number)
            {
                int next=token.Start+token.Length;while(next<source.Length&&(source[next]==' '||source[next]=='\t'))next++;
                int line=source.LastIndexOf('\n',System.Math.Max(0,token.Start-1))+1;
                string prefix=source.Substring(line,token.Start-line);
                if(next<source.Length&&source[next]=='(')kind=Kind.Function;
                else if(Regex.IsMatch(prefix,@"^\s*[^=]+\s*=\s*$")&&(next==source.Length||source[next]=='#'||source[next]=='\r'||source[next]=='\n'))kind=Kind.Material;
                else if(Regex.IsMatch(prefix,@"(?:^|[^0-9])1\(\s*$")||Regex.IsMatch(prefix,@"(?:^|[^0-9])4\([^(),]*,\s*$"))kind=Kind.Material;
            }
            if(token.Feature==BlueprintFeature.Def)definitionNamePending=true;
            result.Add(new Token(token.Start,token.Length,kind));
        }
        return result;
    }
}
