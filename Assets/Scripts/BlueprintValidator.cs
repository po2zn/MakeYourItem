using System;
using System.Collections.Generic;

public enum BlueprintType { Unknown, Int, Bool, None, String, Piece, List, Tuple, Set, Dict, Void }
public sealed class BlueprintProgram
{
    public List<BlueprintStatement> Body;
    public bool DebugOnly;
    public Dictionary<string,BlueprintStatement> Functions = new Dictionary<string,BlueprintStatement>(StringComparer.Ordinal);
    public HashSet<string> Effectful = new HashSet<string>(StringComparer.Ordinal);
}
public sealed class BlueprintValidator
{
    readonly BlueprintProgram program;
    int expressionDepth;
    bool hasFinish, hasImports, hasStarImport;
    readonly HashSet<string> dynamicNames=new HashSet<string>(StringComparer.Ordinal);
    readonly ForgeTechnology technology;
    readonly bool deferErrors;
    public BlueprintValidator(List<BlueprintStatement> body,ForgeTechnology technology=null,bool deferErrors=false) {program=new BlueprintProgram {Body=body};this.technology=technology;this.deferErrors=deferErrors;}
    void Error(BlueprintToken token,string message) {throw new BlueprintException(token.Line,message);}
    public BlueprintProgram Validate()
    {
        if(!deferErrors&&technology!=null)foreach(var call in Calls(program.Body))
            if(ForgeTechnology.RequiresPurchase(call.Feature)&&!technology.Owns(call.Feature))
                Error(call.Token,"테크 트리에서 먼저 구매해야 하는 기능입니다: "+BlueprintAliases.Find(call.Feature).Symbol+"()");
        foreach(var s in program.Body) if(s.Kind==BlueprintStatementKind.Def) {
            if(BlueprintAliases.Find(s.Name)!=null || program.Functions.ContainsKey(s.Name)) Error(s.Token,"중복 또는 내장 함수 이름: "+s.Name);
            program.Functions.Add(s.Name,s);
        }
        CollectBindings(program.Body);
        // Monotone effect propagation also terminates for recursive call graphs.
        // Call depth is checked on the actual runtime path, not the static graph.
        bool changed;
        do { changed=false;foreach(var pair in program.Functions) if(!program.Effectful.Contains(pair.Key)&&HasEffects(pair.Value.Body)){program.Effectful.Add(pair.Key);changed=true;} }while(changed);
        foreach(var f in program.Functions.Values) {
            var names=new Dictionary<string,BlueprintType>();
            foreach(var arg in f.Parameters) {if(names.ContainsKey(arg)){if(!deferErrors)Error(f.Token,"중복 매개변수: "+arg);f.Error=new BlueprintException(f.Token.Line,"중복 매개변수: "+arg);}names[arg]=BlueprintType.Unknown;}
            Statements(f.Body,names,true,0,false);
        }
        Statements(program.Body,new Dictionary<string,BlueprintType>(),false,0,true);
        bool hasPrint=false;foreach(var call in Calls(program.Body))if(call.Feature==BlueprintFeature.Print)hasPrint=true;
        program.DebugOnly=!hasImports&&!hasFinish&&hasPrint&&!HasEffects(program.Body);
        return program;
    }
    void CollectBindings(List<BlueprintStatement> body)
    {
        foreach(var s in body){
            if(s.Kind==BlueprintStatementKind.Assign&&s.Target.Kind==BlueprintExprKind.Name)dynamicNames.Add(s.Target.Name);
            if(s.Kind==BlueprintStatementKind.For)dynamicNames.Add(s.Name);
            foreach(var parameter in s.Parameters)dynamicNames.Add(parameter);
            if(s.Kind==BlueprintStatementKind.Import||s.Kind==BlueprintStatementKind.FromImport){hasImports=true;foreach(var item in s.Imports)if(item.Name=="*")hasStarImport=true;else dynamicNames.Add(item.Alias);}
            CollectBindings(s.Body);CollectBindings(s.Other);
        }
    }
    IEnumerable<BlueprintExpr> ExprCalls(BlueprintExpr e)
    {
        if(e==null)yield break;
        // Iterative traversal also handles long malformed arithmetic without overflowing the host stack.
        var stack=new Stack<BlueprintExpr>();stack.Push(e);
        while(stack.Count>0){var next=stack.Pop();if(next.Kind==BlueprintExprKind.Call)yield return next;foreach(var arg in next.Args)stack.Push(arg);if(next.Callee!=null)stack.Push(next.Callee);}
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
            if(s.Error!=null)continue;
            try {
            if(ended)Error(s.Token,"완성품 반환 뒤에는 명령을 쓸 수 없습니다.");
            if(technology!=null&&Array.IndexOf(ForgeTechnology.ControlFeatures,s.Token.Feature)>=0&&!technology.Owns(s.Token.Feature))
                Error(s.Token,"먼저 테크 트리의 '제어문 해금 · 자동화 시작'을 구매하세요: "+s.Token.Raw);
            switch(s.Kind) {
                case BlueprintStatementKind.Import:
                case BlueprintStatementKind.FromImport:
                    foreach(var item in s.Imports){if(item.Name=="*"&&function)Error(s.Token,"import *는 함수 밖에서만 사용할 수 있습니다.");if(item.Alias!=null)names[item.Alias]=BlueprintType.Unknown;}break;
                case BlueprintStatementKind.Def:if(!root)Error(s.Token,"함수는 최상위에서만 정의하세요.");break;
                case BlueprintStatementKind.Assign:
                    var assigned=Expr(s.Expr,names,s.Target.Kind==BlueprintExprKind.Name);

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
                    Expr(s.Expr,names,true);break;
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
                    if(!Known(Expr(s.Expr,names,false),BlueprintType.Piece,BlueprintType.Int))Error(s.Token,"제출에는 작업물 또는 재료 번호가 필요합니다.");
                    hasFinish=true;ended=true;break;
            }
            }catch(BlueprintException error){if(!deferErrors)throw;s.Error=error;}
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
        // Variables are dynamic bindings in the current runtime scope. A value may
        // have been assigned by an earlier iteration or a selected branch. This
        // single-pass validator cannot prove its presence/type at the point of use.
        // Keep syntax/literal/arity checks here; Eval resolves names and types when read.
        if(e.Kind==BlueprintExprKind.Name)return BlueprintType.Unknown;
        var args=new List<BlueprintType>();foreach(var a in e.Args)args.Add(Expr(a,names,false));
        if(e.Kind==BlueprintExprKind.Attribute)return BlueprintType.Unknown;
        if(e.Kind==BlueprintExprKind.Call) {
            if(deferErrors&&technology!=null&&ForgeTechnology.RequiresPurchase(e.Feature)&&!technology.Owns(e.Feature))Error(e.Token,"테크 트리에서 먼저 구매해야 하는 기능입니다: "+BlueprintAliases.Find(e.Feature).Symbol+"()");
            if(e.Callee!=null){Expr(e.Callee,names,false);return BlueprintType.Unknown;}
            int arity;
            if(e.Process>0)arity=e.Process==1||e.Process==3?1:2;
            else if(e.Feature!=BlueprintFeature.None)arity=BlueprintAliases.Find(e.Feature).Arity;
            else {if(dynamicNames.Contains(e.Name)||hasStarImport)return BlueprintType.Unknown;if(!program.Functions.TryGetValue(e.Name,out var f)) {Error(e.Token,"정의되지 않은 함수 "+e.Name);return BlueprintType.Unknown;} arity=f.Parameters.Count;}
            if(arity>=0&&args.Count!=arity)Error(e.Token,(e.Feature!=BlueprintFeature.None?BlueprintAliases.Find(e.Feature).English:e.Process>0?e.Process.ToString():e.Name)+"에 필요한 인자 수: "+arity);
            if(e.Process>0){
                if(!Known(args[0],e.Process==1?BlueprintType.Int:BlueprintType.Piece))Error(e.Token,e.Process==1?"재료 번호는 정수여야 합니다.":"첫 인자는 작업물이어야 합니다.");
                if(args.Count==2&&!Known(args[1],BlueprintType.Int))Error(e.Token,"두 번째 인자는 정수여야 합니다.");
                if(e.Process==1 && e.Args[0].Kind==BlueprintExprKind.Literal && e.Args[0].Value is int material && ForgeMaterialGuide.Find(material)==null)Error(e.Token,"등록되지 않은 재료 번호: "+material);
                if(e.Process==4 && e.Args[1].Kind==BlueprintExprKind.Literal && !Equals(e.Args[1].Value,0) && !Equals(e.Args[1].Value,3) && !Equals(e.Args[1].Value,8))Error(e.Token,"냉각 재료는 공기(0), 물(3), 서리수(8)입니다.");
                return e.Process==1?BlueprintType.Piece:BlueprintType.None;
            }
            if(ForgeSmithLanguage.IsSmith(e.Feature))return e.Feature==BlueprintFeature.SmithPosition?BlueprintType.Tuple:e.Feature==BlueprintFeature.SmithGift?BlueprintType.None:BlueprintType.Int;
            if(e.Feature==BlueprintFeature.GiantRune)return BlueprintType.None;
            if(ForgeExpansion.IsExtended(e.Feature))return ForgeExpansion.ReturnType(e.Feature);
            switch(e.Feature){
                case BlueprintFeature.IsDay:return BlueprintType.Int;
                case BlueprintFeature.NightTime:
                    if(args.Count>1)Error(e.Token,"24는 인자 없이 호출하거나 24(60)으로 사용하세요.");
                    if(args.Count==1 && (!Known(args[0],BlueprintType.Int) || e.Args[0].Kind==BlueprintExprKind.Literal && !Equals(e.Args[0].Value,60)))Error(e.Token,"시각 조회의 인자는 정수 60이어야 합니다.");
                    return BlueprintType.Int;
                case BlueprintFeature.Print:return BlueprintType.None;
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
