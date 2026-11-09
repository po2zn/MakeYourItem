using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// The interpreter owns preview state only. Craft replays successful process receipts.
public sealed partial class BlueprintRuntime
{
    public const int CommandLimit=10000, CallLimit=32, CollectionLimit=1024;
    readonly BlueprintProgram program;
    readonly ForgeMvp.Evaluation result=new ForgeMvp.Evaluation();
    readonly ForgeProductionLedger ledger=new ForgeProductionLedger();
    readonly string owner;
    int gold, iron, commands, calls, allocated, printCount, printCharacters;
    readonly Dictionary<int,long> stocks;
    uint randomState;
    int currentLine=1, nextPieceId;
    enum Flow { Next, Break, Continue, Return, Finish }
    struct Signal { public Flow Flow; public object Value; public Signal(Flow flow,object value=null){Flow=flow;Value=value;} }
    BlueprintRuntime(BlueprintProgram program,int gold,int iron,string owner,uint seed,IReadOnlyDictionary<int,long> materialStocks=null,IReadOnlyDictionary<string,string> modules=null,ForgeTechnology technology=null)
    {InitializeModules(modules,technology,program);stocks=materialStocks==null?null:new Dictionary<int,long>();if(materialStocks!=null)foreach(var item in materialStocks)stocks[item.Key]=item.Value;this.program=program;this.gold=gold;this.iron=iron;this.owner=owner;randomState=seed;result.debugOnly=program.DebugOnly;}
    public static ForgeMvp.Evaluation Evaluate(string source,int gold,int iron,string owner,uint seed,IReadOnlyDictionary<int,long> materialStocks=null,ForgeTechnology technology=null,IReadOnlyDictionary<string,string> modules=null)
    {
        BlueprintProgram program;
        try {program=new BlueprintValidator(new BlueprintParser(source).Parse(),technology).Validate();}
        catch(BlueprintException e){return Failure(new ForgeMvp.Evaluation(),e,true);}
        var runtime=new BlueprintRuntime(program,gold,iron,owner,seed,materialStocks,modules,technology);
        try {
            runtime.Block(program.Body,runtime.result.values);
            runtime.result.skipped=runtime.result.steps.Count==0;
            runtime.result.debugOnly=runtime.result.product==null&&runtime.result.steps.Count>0&&runtime.result.steps.TrueForAll(step=>step.debugOutput);
        }catch(BlueprintException e){Failure(runtime.result,e,false);}
        catch(OverflowException){Failure(runtime.result,new BlueprintException(runtime.currentLine,"정수 범위 초과"),false);}
        return runtime.result;
    }
    static ForgeMvp.Evaluation Failure(ForgeMvp.Evaluation r,BlueprintException e,bool compile)
    {r.compileError=compile;r.errorLine=e.Line;r.errorModule=e.ModuleName;r.errorMessage=e.Message;r.error=(compile?"컴파일 오류":"공정 오류")+" · "+(e.ModuleName==null?"":e.ModuleName+" · ")+e.Line+"줄: "+e.Message;return r;}
    void Fail(string message){throw new BlueprintException(currentLine,message){ModuleName=currentModule.IsMain?null:currentModule.Name};}
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
            case BlueprintStatementKind.Import:
            case BlueprintStatementKind.FromImport:Import(s,env);break;
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
                if(!currentModule.IsMain)Fail("가져온 코드창에서는 !로 제출할 수 없습니다. 작업물을 반환하고 실행한 창에서 제출하세요. 직접 실행 전용 코드는 if __name__ == \"__main__\": 안에 작성하세요.");
                var submitted=Eval(s.Expr,env);currentLine=s.Token.Line;
                var piece=submitted is int material?LoadMaterial(material,0):Piece(submitted);
                result.product=ledger.Complete(owner,piece.heat,piece.shaped,piece.traits,piece.material);
                AddStep(0,piece,0,0,result.product.DisplayName+" 제출 · 평가 "+ForgePricing.Quote(result.product).SalePrice+"G",true);return new Signal(Flow.Finish);
        }}return new Signal(Flow.Next);
    }
    IList<int> Sequence(object value){if(value is List<int> l)return l;if(value is int[] t)return t;Fail("정수 리스트 또는 튜플이 필요합니다.");return null;}
    void CheckIndex(int key,int count){if(key<0||key>=count)Fail("인덱스 범위를 벗어났습니다: "+key);}
    object Eval(BlueprintExpr e,Dictionary<string,object> env)
    {
        Tick(e.Token);
        switch(e.Kind){
            case BlueprintExprKind.Literal:return e.Value;
            case BlueprintExprKind.Name:return Lookup(e.Name,env);
            case BlueprintExprKind.Attribute:
                var target=Eval(e.Args[0],env);currentLine=e.Token.Line;return Member(target,e.Name);
            case BlueprintExprKind.Unary:var unary=Eval(e.Args[0],env);currentLine=e.Token.Line;return e.Token.Raw=="not"?(object)!Bool(unary):checked(-Int(unary));
            case BlueprintExprKind.Binary:return Binary(e,env);
            case BlueprintExprKind.Call:
                object callable=e.Callee!=null?Eval(e.Callee,env):e.Process==0&&e.Feature==BlueprintFeature.None?Lookup(e.Name,env):null;
                var args=new List<object>();foreach(var a in e.Args)args.Add(Eval(a,env));currentLine=e.Token.Line;return e.Callee!=null||e.Process==0&&e.Feature==BlueprintFeature.None?Invoke(callable,args):Call(e,args);
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
    static string PrintValue(object value)
    {
        if(value==null)return "None";
        if(value is Module module)return "<module "+module.Name+">";
        if(value is Function function)return "<function "+function.Module.Name+"."+function.Definition.Name+">";
        if(value is string text)return text;
        if(value is bool flag)return flag?"True":"False";
        if(value is int number)return number.ToString(CultureInfo.InvariantCulture);
        if(value is ForgeMvp.Piece piece)return "작업물(재료="+ForgeMaterialGuide.Find(piece.material).Name+", 열="+PrintValue(piece.heat)+", 성형="+PrintValue(piece.shaped)+", 특성="+piece.traits+")";
        if(value is Dictionary<int,int> map){
            var pairs=new List<string>();foreach(var pair in map)pairs.Add(PrintValue(pair.Key)+": "+PrintValue(pair.Value));
            return "{"+string.Join(", ",pairs)+"}";
        }
        var items=new List<string>();
        if(value is HashSet<int> set){
            if(set.Count==0)return "set()";
            var sorted=new List<int>(set);sorted.Sort();foreach(int item in sorted)items.Add(PrintValue(item));
            return "{"+string.Join(", ",items)+"}";
        }
        if(value is IList<int> sequence){
            foreach(int item in sequence)items.Add(PrintValue(item));
            string joined=string.Join(", ",items);
            return value is int[]?"("+joined+(items.Count==1?",":"")+")":"["+joined+"]";
        }
        throw new InvalidOperationException("Unsupported blueprint value");
    }
    object Call(BlueprintExpr e,List<object> args)
    {
        if(e.Process>0)return Process(e.Process,args);
        switch(e.Feature){
            case BlueprintFeature.Print:
                var output=new StringBuilder();
                for(int i=0;i<args.Count;i++){
                    string part=PrintValue(args[i]);
                    if((long)output.Length+(i>0?1:0)+part.Length>65536-printCharacters)Fail("한 실행의 출력 한도(256개/65536자)를 초과했습니다.");
                    if(i>0)output.Append(' ');output.Append(part);
                }
                string message=output.ToString();
                if(printCount>=256 || message.Length>65536-printCharacters)Fail("한 실행의 출력 한도(256개/65536자)를 초과했습니다.");
                printCount++;printCharacters+=message.Length;
                result.steps.Add(new ForgeMvp.Step{line=currentLine,module=currentModule.IsMain?null:currentModule.Name,debugOutput=true,text=message});return null;
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
        Fail("지원하지 않는 함수입니다.");return null;
    }
    object Process(int id,List<object> args)
    {
        if(id==1)return LoadMaterial(Int(args[0]),10);
        var piece=Piece(args[0]);int heat=piece.heat;bool shaped=piece.shaped;var traits=piece.traits;
        if(id==2){heat=Int(args[1]);if(heat<1||heat>9)Fail("열 단계 범위: 1~9");traits&=~ForgeTraits.AirCooled;}
        if(id==3 && heat>=6)shaped=true;
        if(id==4){if(Int(args[1])!=0)Fail("현재 지원하는 냉각 재료는 공기(0)입니다.");heat=0;traits|=ForgeTraits.AirCooled;}
        // Validate and pay before committing state. Failed calls leave no receipt.
        if(!ledger.TrySpend(id==2?2:0,0,ref gold,ref iron))Fail("자금 또는 철 부족");
        piece.heat=heat;piece.shaped=shaped;piece.traits=traits;
        Record(id,piece,id==2?2:0,0,id==2?"가열 · 연료 2G":id==3?(heat>=6?"타격 · 검날 성형":"타격 · 열 부족으로 성형 효과 없음"):"공기 냉각 · 0G",false);return null;
    }
    ForgeMvp.Piece LoadMaterial(int material,int cost)
    {
        var entry=ForgeMaterialGuide.Find(material);
        if(entry==null)Fail("등록되지 않은 재료 번호: "+material);
        if(material>1&&stocks!=null&&(!stocks.TryGetValue(material,out long amount)||amount<1))Fail(entry.Name+" 재료가 잠겨 있거나 보유량이 부족합니다.");
        var piece=new ForgeMvp.Piece{id=++nextPieceId,material=material};
        AddStep(1,piece,cost,material==1?1:0,entry.Name+" 올리기 · "+cost+"G");
        if(material>1&&stocks!=null)stocks[material]--;
        return piece;
    }
    void AddStep(int process,ForgeMvp.Piece piece,int cost,int ironCost,string text,bool finished=false)
    {if(!ledger.TrySpend(cost,ironCost,ref gold,ref iron))Fail("자금 또는 철 부족");Record(process,piece,cost,ironCost,text,finished);}
    void Record(int process,ForgeMvp.Piece piece,int cost,int ironCost,string text,bool finished)
    {result.steps.Add(new ForgeMvp.Step{line=currentLine,module=currentModule.IsMain?null:currentModule.Name,process=process,pieceId=piece.id,material=piece.material,materialCount=process==1&&piece.material!=0?1:0,heat=piece.heat,shaped=piece.shaped,traits=piece.traits,cost=cost,iron=ironCost,text=text,finished=finished});result.cost=ledger.CashCost;result.ironConsumed=ledger.IronConsumed;}
}
