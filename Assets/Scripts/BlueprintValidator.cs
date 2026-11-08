using System;
using System.Collections.Generic;

public enum BlueprintType { Unknown, Int, Bool, None, String, Piece, List, Tuple, Set, Dict, Void }
public sealed class BlueprintProgram
{
    public List<BlueprintStatement> Body;
    public Dictionary<string,BlueprintStatement> Functions = new Dictionary<string,BlueprintStatement>(StringComparer.Ordinal);
    public HashSet<string> Effectful = new HashSet<string>(StringComparer.Ordinal);
}
public sealed class BlueprintValidator
{
    readonly BlueprintProgram program;
    int expressionDepth;
    readonly Dictionary<string,int> callDepths=new Dictionary<string,int>();
    bool hasFinish;
    public BlueprintValidator(List<BlueprintStatement> body) {program=new BlueprintProgram {Body=body};}
    void Error(BlueprintToken token,string message) {throw new BlueprintException(token.Line,message);}
    public BlueprintProgram Validate()
    {
        foreach(var s in program.Body) if(s.Kind==BlueprintStatementKind.Def) {
            if(BlueprintAliases.Find(s.Name)!=null || program.Functions.ContainsKey(s.Name)) Error(s.Token,"중복 또는 내장 함수 이름: "+s.Name);
            program.Functions.Add(s.Name,s);
        }
        // Effects and recursion are resolved from the same canonical calls used by execution.
        foreach(var pair in program.Functions) VisitCalls(pair.Key,new HashSet<string>());
        bool changed;
        do { changed=false;foreach(var pair in program.Functions) if(!program.Effectful.Contains(pair.Key)&&HasEffects(pair.Value.Body)){program.Effectful.Add(pair.Key);changed=true;} }while(changed);
        foreach(var f in program.Functions.Values) {
            var names=new Dictionary<string,BlueprintType>();
            foreach(var arg in f.Parameters) {if(names.ContainsKey(arg))Error(f.Token,"중복 매개변수: "+arg);names[arg]=BlueprintType.Unknown;}
            Statements(f.Body,names,true,0,false);
        }
        Statements(program.Body,new Dictionary<string,BlueprintType>(),false,0,true);
        if(!hasFinish) throw new BlueprintException(program.Body.Count==0?1:program.Body[program.Body.Count-1].Token.Line,"완성품 반환 !Blade가 필요합니다. 마지막 명령 뒤에 추가하세요.");
        return program;
    }
    int VisitCalls(string name,HashSet<string> path)
    {
        if(path.Contains(name))Error(program.Functions[name].Token,"재귀 호출은 지원하지 않습니다: "+name);
        if(path.Count>=32)Error(program.Functions[name].Token,"함수 호출 체인은32단계 이하여야 합니다.");
        if(callDepths.TryGetValue(name,out int cached))return cached;
        path.Add(name);int depth=1;
        foreach(var call in Calls(program.Functions[name].Body))if(call.Name!=null&&program.Functions.ContainsKey(call.Name))depth=Math.Max(depth,1+VisitCalls(call.Name,path));
        path.Remove(name);if(depth>32)Error(program.Functions[name].Token,"함수 호출 체인은32단계 이하여야 합니다.");
        callDepths[name]=depth;return depth;
    }
    IEnumerable<BlueprintExpr> ExprCalls(BlueprintExpr e)
    {
        if(e==null)yield break;
        // Iterative traversal also handles long malformed arithmetic without overflowing the host stack.
        var stack=new Stack<BlueprintExpr>();stack.Push(e);
        while(stack.Count>0){var next=stack.Pop();if(next.Kind==BlueprintExprKind.Call)yield return next;foreach(var arg in next.Args)stack.Push(arg);}
    }
    IEnumerable<BlueprintExpr> Calls(List<BlueprintStatement> body)
    {
        foreach(var s in body){foreach(var call in ExprCalls(s.Expr))yield return call;foreach(var call in ExprCalls(s.Target))yield return call;
            foreach(var call in Calls(s.Body))yield return call;foreach(var call in Calls(s.Other))yield return call;}
    }
    bool HasEffects(List<BlueprintStatement> body) {foreach(var c in Calls(body))if(c.Process>0 || c.Name!=null&&program.Effectful.Contains(c.Name))return true;return false;}
    bool Known(BlueprintType value,params BlueprintType[] allowed) {return value==BlueprintType.Unknown || Array.IndexOf(allowed,value)>=0;}
    void Statements(List<BlueprintStatement> body,Dictionary<string,BlueprintType> names,bool function,int loops,bool root)
    {
        bool ended=false;
        foreach(var s in body) {
            if(ended)Error(s.Token,"완성품 반환 뒤에는 명령을 쓸 수 없습니다.");
            switch(s.Kind) {
                case BlueprintStatementKind.Def:if(!root)Error(s.Token,"함수는 최상위에서만 정의하세요.");break;
                case BlueprintStatementKind.Assign:
                    var assigned=Expr(s.Expr,names,s.Target.Kind==BlueprintExprKind.Name);
                    if(assigned==BlueprintType.Void)Error(s.Token,"공정 함수2~4는 반환값이 없습니다.");
                    if(s.Target.Kind==BlueprintExprKind.Name) {
                        try{PythonIdentifier.ParseAssignment(s.Target.Token.Raw);}catch(Exception ex){Error(s.Target.Token,ex.Message);}
                        names[s.Target.Name]=assigned;
                    }else{
                        if(s.Target.Args[0].Kind!=BlueprintExprKind.Name)Error(s.Token,"이름으로 저장한 컬렉션의 인덱스만 변경할 수 있습니다.");
                        Expr(s.Target,names,false);var container=Expr(s.Target.Args[0],names,false);
                        if(!Known(container,BlueprintType.List,BlueprintType.Dict))Error(s.Token,"리스트와 딕셔너리만 변경할 수 있습니다.");
                        if(!Known(assigned,BlueprintType.Int))Error(s.Token,"컬렉션 값은 정수여야 합니다.");
                    }break;
                case BlueprintStatementKind.Call:
                    Expr(s.Expr,names,true);if(s.Expr.Process==1)Error(s.Token,"작업물 참조를 변수에 저장하세요.");break;
                case BlueprintStatementKind.If:
                    if(!Known(Expr(s.Expr,names,false),BlueprintType.Bool))Error(s.Token,"조건은 bool이어야 합니다.");
                    var yes=new Dictionary<string,BlueprintType>(names);var no=new Dictionary<string,BlueprintType>(names);
                    Statements(s.Body,yes,function,loops,false);Statements(s.Other,no,function,loops,false);
                    foreach(var item in yes)if(no.ContainsKey(item.Key))names[item.Key]=no[item.Key]==item.Value?item.Value:BlueprintType.Unknown;
                    break;
                case BlueprintStatementKind.For:
                case BlueprintStatementKind.While:
                    var type=Expr(s.Expr,names,false);
                    if(s.Kind==BlueprintStatementKind.For ? !Known(type,BlueprintType.List,BlueprintType.Tuple) : !Known(type,BlueprintType.Bool))Error(s.Token,s.Kind==BlueprintStatementKind.For?"for 대상은 리스트 또는 튜플이어야 합니다.":"조건은 bool이어야 합니다.");
                    var local=new Dictionary<string,BlueprintType>(names);if(s.Kind==BlueprintStatementKind.For)local[s.Name]=BlueprintType.Int;
                    Statements(s.Body,local,function,loops+1,false);
                    // Loops may execute zero times; preserve only previously defined names.
                    foreach(var key in new List<string>(names.Keys))if(local.ContainsKey(key)&&local[key]!=names[key])names[key]=BlueprintType.Unknown;
                    break;
                case BlueprintStatementKind.Break:
                case BlueprintStatementKind.Continue:if(loops==0)Error(s.Token,"break/continue는 현재 함수의 반복문 안에서만 사용할 수 있습니다.");break;
                case BlueprintStatementKind.Return:
                    if(!function)Error(s.Token,"return은 함수 안에서만 사용할 수 있습니다.");if(s.Expr!=null)Expr(s.Expr,names,false);break;
                case BlueprintStatementKind.Finish:
                    if(function||loops>0)Error(s.Token,"!는 최상위 또는 그 조건문 안에서만 사용할 수 있습니다.");
                    if(!Known(Expr(s.Expr,names,false),BlueprintType.Piece))Error(s.Token,"완성품에는 작업물 변수가 필요합니다.");
                    hasFinish=true;ended=true;break;
            }
        }
    }
    BlueprintType Expr(BlueprintExpr e,Dictionary<string,BlueprintType> names,bool allowProcess)
    {
        if(++expressionDepth>64)Error(e.Token,"표현식 깊이가 너무 큽니다.");
        try {return ExprCore(e,names,allowProcess);}finally{expressionDepth--;}
    }
    BlueprintType ExprCore(BlueprintExpr e,Dictionary<string,BlueprintType> names,bool allowProcess)
    {
        if(e.Kind==BlueprintExprKind.Literal)return e.Value==null?BlueprintType.None:e.Value is int?BlueprintType.Int:e.Value is bool?BlueprintType.Bool:BlueprintType.String;
        if(e.Kind==BlueprintExprKind.Name){if(!names.TryGetValue(e.Name,out var type))Error(e.Token,"미선언 변수 "+e.Name+" · 사용 전에 모든 경로에서 값을 대입하세요.");return type;}
        var args=new List<BlueprintType>();foreach(var a in e.Args)args.Add(Expr(a,names,false));
        if(e.Kind==BlueprintExprKind.Call) {
            if(e.Process>0 || e.Name!=null&&program.Effectful.Contains(e.Name))if(!allowProcess)Error(e.Token,"공정 포함 함수는 별도의 대입 또는 호출 줄에서 실행하세요.");
            int arity;
            if(e.Process>0)arity=e.Process==1||e.Process==3?1:2;
            else if(e.Feature!=BlueprintFeature.None)arity=BlueprintAliases.Find(e.Feature).Arity;
            else {if(!program.Functions.TryGetValue(e.Name,out var f)) {Error(e.Token,"정의되지 않은 함수 "+e.Name);return BlueprintType.Unknown;} arity=f.Parameters.Count;}
            if(args.Count!=arity)Error(e.Token,(e.Feature!=BlueprintFeature.None?BlueprintAliases.Find(e.Feature).English:e.Process>0?e.Process.ToString():e.Name)+"에 필요한 인자 수: "+arity);
            if(e.Process>0){
                if(!Known(args[0],e.Process==1?BlueprintType.Int:BlueprintType.Piece))Error(e.Token,e.Process==1?"재료 번호는 정수여야 합니다.":"첫 인자는 작업물이어야 합니다.");
                if(args.Count==2&&!Known(args[1],BlueprintType.Int))Error(e.Token,"두 번째 인자는 정수여야 합니다.");
                if(e.Process==1 && e.Args[0].Kind==BlueprintExprKind.Literal && !Equals(e.Args[0].Value,1))Error(e.Token,"MVP 작업대 재료는 철(1)입니다.");
                if(e.Process==4 && e.Args[1].Kind==BlueprintExprKind.Literal && !Equals(e.Args[1].Value,0))Error(e.Token,"MVP 냉각 재료는 공기(0)입니다.");
                return e.Process==1?BlueprintType.Piece:BlueprintType.Void;
            }
            switch(e.Feature){
                case BlueprintFeature.Heat:if(!Known(args[0],BlueprintType.Piece))Error(e.Token,"heat에는 작업물이 필요합니다.");return BlueprintType.Int;
                case BlueprintFeature.Abs:case BlueprintFeature.Random:foreach(var a in args)if(!Known(a,BlueprintType.Int))Error(e.Token,"정수 인자가 필요합니다.");return BlueprintType.Int;
                case BlueprintFeature.Len:if(!Known(args[0],BlueprintType.List,BlueprintType.Tuple,BlueprintType.Set,BlueprintType.Dict))Error(e.Token,"len에는 컬렉션이 필요합니다.");return BlueprintType.Int;
                case BlueprintFeature.Min:case BlueprintFeature.Max:if(!Known(args[0],BlueprintType.List,BlueprintType.Tuple))Error(e.Token,"min/max에는 정수 리스트 또는 튜플이 필요합니다.");return BlueprintType.Int;
                case BlueprintFeature.Set:return BlueprintType.Set;
            }
            return BlueprintType.Unknown;
        }
        if(e.Kind==BlueprintExprKind.Index){if(!Known(args[0],BlueprintType.List,BlueprintType.Tuple,BlueprintType.Dict)||!Known(args[1],BlueprintType.Int))Error(e.Token,"정수 인덱스와 리스트/튜플/딕셔너리가 필요합니다.");return BlueprintType.Int;}
        if(e.Kind==BlueprintExprKind.List||e.Kind==BlueprintExprKind.Tuple||e.Kind==BlueprintExprKind.Set||e.Kind==BlueprintExprKind.Dict){foreach(var a in args)if(!Known(a,BlueprintType.Int))Error(e.Token,"컬렉션은 정수만 담을 수 있습니다.");return e.Kind==BlueprintExprKind.List?BlueprintType.List:e.Kind==BlueprintExprKind.Tuple?BlueprintType.Tuple:e.Kind==BlueprintExprKind.Set?BlueprintType.Set:BlueprintType.Dict;}
        if(e.Kind==BlueprintExprKind.Unary){if(!Known(args[0],e.Token.Raw=="not"?BlueprintType.Bool:BlueprintType.Int))Error(e.Token,"단항 연산의 타입이 잘못되었습니다.");return e.Token.Raw=="not"?BlueprintType.Bool:BlueprintType.Int;}
        if(e.Token.Raw=="and"||e.Token.Raw=="or"){foreach(var a in args)if(!Known(a,BlueprintType.Bool))Error(e.Token,"논리 연산에는 bool이 필요합니다.");return BlueprintType.Bool;}
        if(e.Token.Raw=="=="||e.Token.Raw=="!="||e.Token.Raw=="in")return BlueprintType.Bool;
        foreach(var a in args)if(!Known(a,BlueprintType.Int))Error(e.Token,"정수 연산이 필요합니다.");
        return e.Token.Raw=="<"||e.Token.Raw==">"||e.Token.Raw=="<="||e.Token.Raw==">="?BlueprintType.Bool:BlueprintType.Int;
    }
}
