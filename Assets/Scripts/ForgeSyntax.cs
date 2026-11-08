using System.Collections.Generic;
using System.Text.RegularExpressions;

// Presentation-only tokens: indices always refer to the original editable source.
public static class ForgeSyntax
{
    public enum Kind { Plain, Comment, Material, Keyword, Function }
    public struct Token
    {
        public int start, length;
        public Kind kind;
        public Token(int start, int length, Kind kind)
        { this.start = start; this.length = length; this.kind = kind; }
    }
    static readonly Regex Tokens = new Regex(@"#[^\r\n]*|[^\s()=,!#]+|[()=,!]", RegexOptions.Compiled);
    static readonly Regex Integer = new Regex(@"^[0-9]+$", RegexOptions.Compiled);
    public static List<Token> Scan(string source)
    {
        var result = new List<Token>();
        int function = 0, argument = 0, line = -1;
        foreach (Match match in Tokens.Matches(source))
        {
            int currentLine = source.LastIndexOf('\n', match.Index);
            if (currentLine != line) { function = 0; argument = 0; line = currentLine; }
            string value = match.Value;
            Kind kind = Kind.Plain;
            if (value[0] == '#') kind = Kind.Comment;
            else if (value == "!" || PythonIdentifier.IsKeyword(value) || value == "__debug__") kind = Kind.Keyword;
            else if (Integer.IsMatch(value))
            {
                int next = match.Index + match.Length;
                while (next < source.Length && (source[next] == ' ' || source[next] == '	')) next++;
                if (next < source.Length && source[next] == '(')
                {
                    kind = Kind.Function;
                    int.TryParse(value, out function);
                    argument = 0;
                }
                else if ((function == 1 && argument == 1) || (function == 4 && argument == 2))
                    kind = Kind.Material;
            }
            if (value == "(") argument = 1;
            else if (value == ",") argument++;
            else if (value == ")" || value == "=") { function = 0; argument = 0; }
            result.Add(new Token(match.Index, match.Length, kind));
        }
        return result;
    }
}
