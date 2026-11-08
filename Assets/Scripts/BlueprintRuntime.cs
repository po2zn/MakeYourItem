using System;
using System.Collections.Generic;

// The interpreter owns preview state only. Craft replays successful process receipts.
public sealed class BlueprintRuntime
{
    public const int CommandLimit=10000, CallLimit=32, CollectionLimit=1024;
    readonly BlueprintProgram program;
    readonly ForgeMvp.Evaluation result=new ForgeMvp.Evaluation();
    readonly ForgeProductionLedger ledger=new ForgeProductionLedger();
    readonly string owner;
    int gold, iron, commands, calls, allocated;
    uint randomState;
    int currentLine=1;
    enum Flow { Next, Break, Continue, Return, Finish }
    struct Signal { public Flow Flow; public object Value; public Signal(Flow flow,object value=null){Flow=flow;Value=value;} }
    BlueprintRuntime(BlueprintProgram program,int gold,int iron,string owner,uint seed)
    {this.program=program;this.gold=gold;this.iron=iron;this.owner=owner;randomState=seed;}
    public static ForgeMvp.Evaluation Evaluate(string source,int gold,int iron,string owner,uint seed)
    {
        BlueprintProgram program;
        try {program=new BlueprintValidator(new BlueprintParser(source).Parse()).Validate();}
        catch(BlueprintException e){return Failure(new ForgeMvp.Evaluation(),e,true);}
        var runtime=new BlueprintRuntime(program,gold,iron,owner,seed);
        try {
            runtime.Block(program.Body,runtime.result.values);
            if(runtime.result.product==null && runtime.result.steps.Count>0)runtime.Fail("실행한 공정에 완성품 판정이 없습니다.");
            runtime.result.skipped=runtime.result.steps.Count==0;
        }catch(BlueprintException e){Failure(runtime.result,e,false);}
        catch(OverflowException){Failure(runtime.result,new BlueprintException(runtime.currentLine,"정수 범위 초과"),false);}
        return runtime.result;
    }
    static ForgeMvp.Evaluation Failure(ForgeMvp.Evaluation r,BlueprintException e,bool compile)
    {r.compileError=compile;r.errorLine=e.Line;r.errorMessage=e.Message;r.error=(compile?"컴파일 오류":"공정 오류")+" · "+e.Line+"줄: "+e.Message;return r;}
    void Fail(string message){throw new BlueprintException(currentLine,message);}
    void Tick(BlueprintToken token){currentLine=token.Line;if(++commands>CommandLimit)Fail("도면 실행 명령 수 제한(10000)에 도달했습니다.");}
    int Int(object value){if(!(value is int))Fail("정수 인자가 필요합니다.");return (int)value;}
    bool Bool(object value){if(!(value is bool))Fail("조건은 bool이어야 합니다.");return (bool)value;}
    ForgeMvp.Piece Piece(object value){if(!(value is ForgeMvp.Piece))Fail("작업물이 필요합니다.");return (ForgeMvp.Piece)value;}
    void Allocate(int count){if(count>CollectionLimit)Fail("컬렉션은 최대1024개입니다.");allocated+=count;if(allocated>16384)Fail("컬렉션 누적 복사 제한(16384)에 도달했습니다.");}
    object Copy(object value)
    {
        if(value is List<int> l){Allocate(l.Count);return new List<int>(l);}
        if(value is int[] t){Allocate(t.Length);return t.Clone();}
        if(value is HashSet<int> s){Allocate(s.Count);return new HashSet<int>(s);}
        if(value is Dictionary<int,int> d){Allocate(d.Count);return new Dictionary<int,int>(d);}
        return value;
    }
    Signal Block(List<BlueprintStatement> body,Dictionary<string,object> env)
    {
        foreach(var s in body){Tick(s.Token);switch(s.Kind){
            case BlueprintStatementKind.Def:break;
            case BlueprintStatementKind.Assign:
                var value=Eval(s.Expr,env);
                if(s.Target.Kind==BlueprintExprKind.Name)env[s.Target.Name]=Copy(value);
                else {
                    var target=Eval(s.Target.Args[0],env);int key=Int(Eval(s.Target.Args[1],env));int n=Int(value);
                    if(target is List<int> list){CheckIndex(key,list.Count);list[key]=n;}
                    else if(target is Dictionary<int,int> dict){if(!dict.ContainsKey(key)){Allocate(1);if(dict.Count>=CollectionLimit)Fail("컬렉션은 최대1024개입니다.");}dict[key]=n;}
                    else Fail("리스트와 딕셔너리만 변경할 수 있습니다.");
                }break;
            case BlueprintStatementKind.Call:Eval(s.Expr,env);break;
            case BlueprintStatementKind.If:
                var branch=Block(Bool(Eval(s.Expr,env))?s.Body:s.Other,env);if(branch.Flow!=Flow.Next)return branch;break;
            case BlueprintStatementKind.For:
                var items=Sequence(Copy(Eval(s.Expr,env)));
                foreach(int item in items){Tick(s.Token);env[s.Name]=item;var flow=Block(s.Body,env);if(flow.Flow==Flow.Break)break;if(flow.Flow==Flow.Return||flow.Flow==Flow.Finish)return flow;}break;
            case BlueprintStatementKind.While:
                while(Bool(Eval(s.Expr,env))){Tick(s.Token);var flow=Block(s.Body,env);if(flow.Flow==Flow.Break)break;if(flow.Flow==Flow.Return||flow.Flow==Flow.Finish)return flow;}break;
            case BlueprintStatementKind.Break:return new Signal(Flow.Break);
            case BlueprintStatementKind.Continue:return new Signal(Flow.Continue);
            case BlueprintStatementKind.Return:return new Signal(Flow.Return,s.Expr==null?null:Copy(Eval(s.Expr,env)));
            case BlueprintStatementKind.Finish:
                var piece=Piece(Eval(s.Expr,env));currentLine=s.Token.Line;
                if(!piece.shaped||piece.heat!=0)Fail("완성품 검사 실패: 타격과 공기 냉각이 필요합니다.");
                AddStep(0,piece,0,0,"철 검날 완성품 판정 성공",true);
                result.product=ledger.Complete(owner,piece.heat,piece.shaped,piece.traits);return new Signal(Flow.Finish);
        }}return new Signal(Flow.Next);
    }
    IList<int> Sequence(object value){if(value is List<int> l)return l;if(value is int[] t)return t;Fail("정수 리스트 또는 튜플이 필요합니다.");return null;}
    void CheckIndex(int key,int count){if(key<0||key>=count)Fail("인덱스 범위를 벗어났습니다: "+key);}
    object Eval(BlueprintExpr e,Dictionary<string,object> env)
    {
        Tick(e.Token);
        switch(e.Kind){
            case BlueprintExprKind.Literal:return e.Value;
            case BlueprintExprKind.Name:if(!env.TryGetValue(e.Name,out var value))Fail("미선언 변수 "+e.Name);return value;
            case BlueprintExprKind.Unary:var unary=Eval(e.Args[0],env);currentLine=e.Token.Line;return e.Token.Raw=="not"?(object)!Bool(unary):checked(-Int(unary));
            case BlueprintExprKind.Binary:return Binary(e,env);
            case BlueprintExprKind.Call:
                var args=new List<object>();foreach(var a in e.Args)args.Add(Eval(a,env));currentLine=e.Token.Line;return Call(e,args);
            case BlueprintExprKind.Index:
                var container=Eval(e.Args[0],env);int key=Int(Eval(e.Args[1],env));currentLine=e.Token.Line;
                if(container is Dictionary<int,int> dict){if(!dict.TryGetValue(key,out int n))Fail("딕셔너리에 키가 없습니다: "+key);return n;}
                var seq=Sequence(container);CheckIndex(key,seq.Count);return seq[key];
            default:
                var numbers=new List<int>();foreach(var a in e.Args)numbers.Add(Int(Eval(a,env)));currentLine=e.Token.Line;
                Allocate(e.Kind==BlueprintExprKind.Dict?numbers.Count/2:numbers.Count);
                if(e.Kind==BlueprintExprKind.List)return numbers;
                if(e.Kind==BlueprintExprKind.Tuple)return numbers.ToArray();
                if(e.Kind==BlueprintExprKind.Set)return new HashSet<int>(numbers);
                var map=new Dictionary<int,int>();for(int i=0;i<numbers.Count;i+=2)map[numbers[i]]=numbers[i+1];return map;
        }
    }
    object Binary(BlueprintExpr e,Dictionary<string,object> env)
    {
        string op=e.Token.Raw;var a=Eval(e.Args[0],env);
        if(op=="and"&&!Bool(a))return false;if(op=="or"&&Bool(a))return true;
        var b=Eval(e.Args[1],env);currentLine=e.Token.Line;
        if(op=="and"||op=="or")return Bool(b);
        if(op=="=="||op=="!="){bool equal=Equal(a,b);return op=="=="?equal:!equal;}
        if(op=="in") {int item=Int(a);if(b is Dictionary<int,int> d)return d.ContainsKey(item);if(b is HashSet<int> s)return s.Contains(item);return Sequence(b).Contains(item);}
        int x=Int(a),y=Int(b);
        switch(op){case "+":return checked(x+y);case "-":return checked(x-y);case "*":return checked(x*y);
            case "//":case "%":if(y==0)Fail("0으로 나눌 수 없습니다.");long q=(long)x/y,r=(long)x%y;if(r!=0&&(r<0)!=(y<0)){q--;r+=y;}return checked((int)(op=="//"?q:r));
            case "<":return x<y;case ">":return x>y;case "<=":return x<=y;case ">=":return x>=y;}
        Fail("지원하지 않는 연산자");return null;
    }
    bool Equal(object a,object b)
    {
        if(a==null||b==null)return a==b;if(a.GetType()!=b.GetType())return false;
        if(a is HashSet<int> s)return s.SetEquals((HashSet<int>)b);
        if(a is Dictionary<int,int> d){var other=(Dictionary<int,int>)b;if(d.Count!=other.Count)return false;foreach(var pair in d)if(!other.TryGetValue(pair.Key,out int n)||n!=pair.Value)return false;return true;}
        if(a is IList<int> l){var other=(IList<int>)b;if(l.Count!=other.Count)return false;for(int i=0;i<l.Count;i++)if(l[i]!=other[i])return false;return true;}
        return a.Equals(b);
    }
    object Call(BlueprintExpr e,List<object> args)
    {
        if(e.Process>0)return Process(e.Process,args);
        switch(e.Feature){
            case BlueprintFeature.Heat:return Piece(args[0]).heat;
            case BlueprintFeature.Abs:int n=Int(args[0]);return n<0?checked(-n):n;
            case BlueprintFeature.Len:if(args[0] is IList<int> l)return l.Count;if(args[0] is HashSet<int> s)return s.Count;if(args[0] is Dictionary<int,int> d)return d.Count;Fail("len에는 컬렉션이 필요합니다.");break;
            case BlueprintFeature.Min:case BlueprintFeature.Max:
                var seq=Sequence(args[0]);if(seq.Count==0)Fail("min/max에는 비어 있지 않은 목록이 필요합니다.");int best=seq[0];foreach(int item in seq)best=e.Feature==BlueprintFeature.Min?Math.Min(best,item):Math.Max(best,item);return best;
            case BlueprintFeature.Set:return new HashSet<int>();
            case BlueprintFeature.Random:
                int low=Int(args[0]),high=Int(args[1]);if(low>high)Fail("random 최솟값은 최댓값 이하여야 합니다.");
                ulong span=(ulong)((long)high-low+1);ulong bound=(1UL<<32)-((1UL<<32)%span);uint sample;
                do {Tick(e.Token);randomState=unchecked(randomState*1664525u+1013904223u);sample=randomState;}while((ulong)sample>=bound);
                return (int)((long)low+(long)((ulong)sample%span));
        }
        if(++calls>CallLimit)Fail("함수 호출 깊이 제한(32)에 도달했습니다.");
        try {var f=program.Functions[e.Name];var local=new Dictionary<string,object>();for(int i=0;i<args.Count;i++)local[f.Parameters[i]]=Copy(args[i]);return Block(f.Body,local).Value;}
        finally{calls--;}
    }
    object Process(int id,List<object> args)
    {
        if(id==1){if(Int(args[0])!=1)Fail("MVP 작업대 재료는 철(1)입니다.");var p=new ForgeMvp.Piece();AddStep(1,p,10,1,"철 올리기 · 10G");return p;}
        var piece=Piece(args[0]);int heat=piece.heat;bool shaped=piece.shaped;var traits=piece.traits;
        if(id==2){heat=Int(args[1]);if(heat<1||heat>9)Fail("열 단계 범위: 1~9");traits&=~ForgeTraits.AirCooled;}
        if(id==3){if(heat<6)Fail("실행 오류: 타격 전에 2(작업물, 6 이상)으로 가열해야 합니다.");shaped=true;}
        if(id==4){if(Int(args[1])!=0)Fail("MVP 냉각 재료는 공기(0)입니다.");heat=0;traits|=ForgeTraits.AirCooled;}
        // Validate and pay before committing state. Failed calls leave no receipt.
        if(!ledger.TrySpend(id==2?2:0,0,ref gold,ref iron))Fail("자금 또는 철 부족");
        piece.heat=heat;piece.shaped=shaped;piece.traits=traits;
        Record(id,piece,id==2?2:0,0,id==2?"가열 · 연료 2G":id==3?"타격 · 검날 성형":"공기 냉각 · 0G",false);return null;
    }
    void AddStep(int process,ForgeMvp.Piece piece,int cost,int ironCost,string text,bool finished=false)
    {if(!ledger.TrySpend(cost,ironCost,ref gold,ref iron))Fail("자금 또는 철 부족");Record(process,piece,cost,ironCost,text,finished);}
    void Record(int process,ForgeMvp.Piece piece,int cost,int ironCost,string text,bool finished)
    {result.steps.Add(new ForgeMvp.Step{line=currentLine,process=process,heat=piece.heat,shaped=piece.shaped,traits=piece.traits,cost=cost,iron=ironCost,text=text,finished=finished});result.cost=ledger.CashCost;result.ironConsumed=ledger.IronConsumed;}
}
