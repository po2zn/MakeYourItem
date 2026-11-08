using System;
using System.Collections.Generic;

public enum BlueprintExprKind { Literal, Name, Unary, Binary, Call, List, Tuple, Set, Dict, Index }
public sealed class BlueprintExpr
{
    public BlueprintExprKind Kind;
    public BlueprintToken Token;
    public object Value;
    public string Name;
    public BlueprintFeature Feature;
    public int Process;
    public List<BlueprintExpr> Args = new List<BlueprintExpr>();
}
public enum BlueprintStatementKind { Assign, Call, If, For, While, Def, Return, Break, Continue, Finish }
public sealed class BlueprintStatement
{
    public BlueprintStatementKind Kind;
    public BlueprintToken Token;
    public string Name;
    public BlueprintExpr Expr, Target;
    public List<string> Parameters = new List<string>();
    public List<BlueprintStatement> Body = new List<BlueprintStatement>(), Other = new List<BlueprintStatement>();
}
public sealed class BlueprintParser
{
    readonly List<BlueprintToken> tokens;
    int cursor, depth, conditionDepth;
    BlueprintToken Current => tokens[Math.Min(cursor,tokens.Count-1)];
    BlueprintToken Take() { return tokens[cursor++]; }
    bool Match(string raw) { if(Current.Raw!=raw)return false; cursor++; return true; }
    void Need(string raw) { if(!Match(raw)) Fail("'"+raw+"'가 필요합니다."); }
    void Fail(string message) { throw new BlueprintException(Current.Line,message); }
    void Lines() { while(Current.Kind==BlueprintTokenKind.NewLine) Take(); }
    void EndLine() { if(Current.Kind!=BlueprintTokenKind.NewLine) Fail("줄 끝에 예상하지 않은 내용: "+Current.Raw); Take(); }
    string Identifier()
    {
        var token=Take();
        if(token.Kind!=BlueprintTokenKind.Name || token.Feature!=BlueprintFeature.None) throw new BlueprintException(token.Line,"이름이 필요합니다: "+token.Raw);
        try { return PythonIdentifier.ParseAssignment(token.Raw); } catch(Exception ex) { throw new BlueprintException(token.Line,ex.Message); }
    }
    public BlueprintParser(string source)
    { tokens=BlueprintLexer.Scan(source).FindAll(t=>t.Kind!=BlueprintTokenKind.Comment); }
    public List<BlueprintStatement> Parse()
    {
        var body=new List<BlueprintStatement>(); Lines();
        while(Current.Kind!=BlueprintTokenKind.End) { body.Add(Statement()); Lines(); }
        return body;
    }
    List<BlueprintStatement> Block()
    {
        Need(":"); EndLine(); Lines();
        if(Current.Kind!=BlueprintTokenKind.Indent) Fail("들여쓴 블록 본문이 필요합니다."); Take();
        var body=new List<BlueprintStatement>(); Lines();
        while(Current.Kind!=BlueprintTokenKind.Dedent && Current.Kind!=BlueprintTokenKind.End) { body.Add(Statement()); Lines(); }
        if(body.Count==0) Fail("빈 블록은 사용할 수 없습니다.");
        if(Current.Kind!=BlueprintTokenKind.Dedent) Fail("블록이 끝나지 않았습니다."); Take();
        return body;
    }
    BlueprintStatement Conditional()
    {
        int initialDepth=conditionDepth;
        BlueprintStatement first=null, previous=null;
        try {
            do {
                if(++conditionDepth>32) Fail("조건 분기 중첩은32단계 이하여야 합니다.");
                var branch=new BlueprintStatement {Token=Take(),Kind=BlueprintStatementKind.If};
                branch.Expr=Expression(); branch.Body=Block(); Lines();
                if(first==null)first=branch; else previous.Other.Add(branch);
                previous=branch;
            } while(Current.Feature==BlueprintFeature.Elif);
            if(Current.Feature==BlueprintFeature.Else) {Take();previous.Other=Block();}
            // elif lowers to an if in the preceding false branch; evaluation stays shared.
            return first;
        } finally {conditionDepth=initialDepth;}
    }
    BlueprintStatement Statement()
    {
        var token=Current; var statement=new BlueprintStatement { Token=token };
        switch(token.Feature)
        {
            case BlueprintFeature.If:
                return Conditional();
            case BlueprintFeature.While:
                Take(); statement.Kind=BlueprintStatementKind.While; statement.Expr=Expression(); statement.Body=Block(); return statement;
            case BlueprintFeature.For:
                Take(); statement.Kind=BlueprintStatementKind.For; bool wrapped=Match("("); statement.Name=Identifier(); Need("in");
                statement.Expr=Expression(); if(wrapped) Need(")"); statement.Body=Block(); return statement;
            case BlueprintFeature.Def:
                Take(); statement.Kind=BlueprintStatementKind.Def;
                // Canonical: 333 choose(args):. Also accept explicit numeric header 333(choose, args):.
                bool flat=token.Raw=="333" && Match("(");
                statement.Name=Identifier();
                if(flat) {
                    if(Match(",")) { do { statement.Parameters.Add(Identifier()); } while(Match(",")); }
                    Need(")");
                } else {
                    Need("("); if(!Match(")")) { do { statement.Parameters.Add(Identifier()); } while(Match(",")); Need(")"); }
                }
                if(statement.Parameters.Count>8) throw new BlueprintException(token.Line,"매개변수는 최대8개입니다.");
                statement.Body=Block(); return statement;
            case BlueprintFeature.Return:
                Take(); statement.Kind=BlueprintStatementKind.Return;
                if(Current.Kind!=BlueprintTokenKind.NewLine) statement.Expr=Expression(); EndLine(); return statement;
            case BlueprintFeature.Break:
            case BlueprintFeature.Continue:
                Take(); statement.Kind=token.Feature==BlueprintFeature.Break ? BlueprintStatementKind.Break : BlueprintStatementKind.Continue;
                EndLine(); return statement;
            case BlueprintFeature.Elif: Fail("elif/01?에 연결된 if/1?가 없습니다. 같은 들여쓰기의 if 뒤, else 앞에 작성하세요."); break;
            case BlueprintFeature.Else: Fail("else/0?에 연결된 if/1?가 없습니다."); break;
        }
        if(Match("!")) { statement.Kind=BlueprintStatementKind.Finish; statement.Expr=Expression(); EndLine(); return statement; }
        var value=Expression();
        if(Match("=")) {
            if(value.Kind!=BlueprintExprKind.Name && value.Kind!=BlueprintExprKind.Index) throw new BlueprintException(token.Line,"대입 대상은 이름 또는 인덱스여야 합니다.");
            statement.Kind=BlueprintStatementKind.Assign; statement.Target=value; statement.Expr=Expression();
        } else {
            if(value.Kind!=BlueprintExprKind.Call) throw new BlueprintException(token.Line,"대입 또는 함수 호출이 필요합니다.");
            statement.Kind=BlueprintStatementKind.Call; statement.Expr=value;
        }
        EndLine(); return statement;
    }
    BlueprintExpr Node(BlueprintExprKind kind, BlueprintToken token, params BlueprintExpr[] args)
    { var node=new BlueprintExpr { Kind=kind,Token=token }; node.Args.AddRange(args); return node; }
    BlueprintExpr Expression()
    {
        if(++depth>32) Fail("표현식 중첩은32단계 이하여야 합니다.");
        try { return Or(); } finally { depth--; }
    }
    BlueprintExpr Or() { var left=And(); while(Current.Raw=="or") { var op=Take(); left=Node(BlueprintExprKind.Binary,op,left,And()); } return left; }
    BlueprintExpr And() { var left=Not(); while(Current.Raw=="and") { var op=Take(); left=Node(BlueprintExprKind.Binary,op,left,Not()); } return left; }
    BlueprintExpr Not() { if(Current.Raw=="not") { var op=Take(); if(++depth>32)Fail("표현식 중첩 제한"); try{return Node(BlueprintExprKind.Unary,op,Not());}finally{depth--;} } return Compare(); }
    bool Comparison(string op) => op=="=="||op=="!="||op=="<"||op==">"||op=="<="||op==">="||op=="in";
    BlueprintExpr Compare()
    {
        var left=Sum(); if(Comparison(Current.Raw)) { var op=Take(); left=Node(BlueprintExprKind.Binary,op,left,Sum()); }
        if(Comparison(Current.Raw)) Fail("연쇄 비교 대신 and로 조건을 연결하세요."); return left;
    }
    BlueprintExpr Sum() { var left=Product(); while(Current.Raw=="+"||Current.Raw=="-") { var op=Take();left=Node(BlueprintExprKind.Binary,op,left,Product()); } return left; }
    BlueprintExpr Product() { var left=Unary(); while(Current.Raw=="*"||Current.Raw=="//"||Current.Raw=="%") { var op=Take();left=Node(BlueprintExprKind.Binary,op,left,Unary()); } return left; }
    BlueprintExpr Unary()
    {
        if(Current.Raw=="-") {
            var op=Take();
            if(Current.Kind==BlueprintTokenKind.Number && Current.Raw=="2147483648") { Take();var n=Node(BlueprintExprKind.Literal,op);n.Value=int.MinValue;return n; }
            if(++depth>32)Fail("표현식 중첩 제한");try{return Node(BlueprintExprKind.Unary,op,Unary());}finally{depth--;}
        }
        return Atom();
    }
    BlueprintExpr Atom()
    {
        var token=Take(); BlueprintExpr value;
        bool callable=token.Kind==BlueprintTokenKind.Name || token.Kind==BlueprintTokenKind.Number;
        if(callable && Current.Raw=="(") {
            Take(); value=Node(BlueprintExprKind.Call,token); value.Feature=token.Feature;
            if(token.Kind==BlueprintTokenKind.Number && value.Feature==BlueprintFeature.None) {
                if(!int.TryParse(token.Raw,out value.Process)||value.Process<1||value.Process>4) throw new BlueprintException(token.Line,"지원하지 않는 공정 번호: "+token.Raw);
            } else if(value.Feature==BlueprintFeature.None) {
                try{value.Name=PythonIdentifier.Parse(token.Raw);}catch(Exception ex){throw new BlueprintException(token.Line,ex.Message);}
            } else if(BlueprintAliases.Find(value.Feature).Syntax!=BlueprintSyntaxKind.Builtin) throw new BlueprintException(token.Line,"제어문은 문장 시작 위치에서 사용하세요.");
            if(!Match(")")) { do { value.Args.Add(Expression()); } while(Match(",")); Need(")"); }
        }
        else if(token.Kind==BlueprintTokenKind.Number) {
            if(!int.TryParse(token.Raw,out int n)) throw new BlueprintException(token.Line,"정수 범위 초과"); value=Node(BlueprintExprKind.Literal,token);value.Value=n;
        }
        else if(token.Kind==BlueprintTokenKind.String) { value=Node(BlueprintExprKind.Literal,token);value.Value=Decode(token); }
        else if(token.Kind==BlueprintTokenKind.Name && (token.Raw=="True"||token.Raw=="False"||token.Raw=="None")) {
            value=Node(BlueprintExprKind.Literal,token);value.Value=token.Raw=="None"?null:(object)(token.Raw=="True");
        }
        else if(token.Kind==BlueprintTokenKind.Name && token.Feature==BlueprintFeature.None) {
            value=Node(BlueprintExprKind.Name,token);try{value.Name=PythonIdentifier.Parse(token.Raw);}catch(Exception ex){throw new BlueprintException(token.Line,ex.Message);}
        }
        else if(token.Raw=="[") {
            value=Node(BlueprintExprKind.List,token); if(!Match("]")) { do{value.Args.Add(Expression());}while(Match(",") && Current.Raw!="]");Need("]"); }
        }
        else if(token.Raw=="(") {
            if(Match(")")) value=Node(BlueprintExprKind.Tuple,token);
            else { var first=Expression(); if(Match(",")) { value=Node(BlueprintExprKind.Tuple,token,first); if(Current.Raw!=")")do{value.Args.Add(Expression());}while(Match(",")&&Current.Raw!=")");Need(")"); } else {Need(")");value=first;} }
        }
        else if(token.Raw=="{") {
            if(Match("}")) value=Node(BlueprintExprKind.Dict,token);
            else {
                var first=Expression(); bool dictionary=Match(":");value=Node(dictionary?BlueprintExprKind.Dict:BlueprintExprKind.Set,token,first);
                if(dictionary)value.Args.Add(Expression());
                while(Match(",")&&Current.Raw!="}") {value.Args.Add(Expression());if(dictionary){Need(":");value.Args.Add(Expression());}}
                Need("}");
            }
        }
        else throw new BlueprintException(token.Line,"표현식이 필요합니다: "+token.Raw);
        while(Match("[")) {var index=Expression();Need("]");value=Node(BlueprintExprKind.Index,token,value,index);}
        return value;
    }
    static string Decode(BlueprintToken token)
    {
        var text=new System.Text.StringBuilder();
        for(int i=1;i<token.Raw.Length-1;i++) {
            char c=token.Raw[i];
            if(c=='\\') {c=token.Raw[++i];switch(c){case 'n':c='\n';break;case 'r':c='\r';break;case 't':c='\t';break;case '\\':case '\'':case '"':break;default:throw new BlueprintException(token.Line,"지원하지 않는 문자열 이스케이프");}}
            text.Append(c);
        }
        return text.ToString();
    }
}
