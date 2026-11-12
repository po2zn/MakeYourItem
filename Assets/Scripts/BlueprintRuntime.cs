using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

// The interpreter emits receipts; sessions suspend before each executed statement.
public sealed partial class BlueprintRuntime
{
    public const int CommandLimit=10000, CallLimit=32, CollectionLimit=1024;
    readonly BlueprintProgram program;
    readonly ForgeMvp.Evaluation result=new ForgeMvp.Evaluation();
    readonly ForgeProductionLedger ledger=new ForgeProductionLedger();
    readonly string owner;
    readonly Func<double> clockHour;
    readonly ForgeRunContext context;
    double productionSeconds;
    readonly double startedAt;
    readonly HashSet<ForgeMvp.Piece> submittedPieces=new HashSet<ForgeMvp.Piece>();
    readonly int[] usedMaterials=new int[10];
    Dictionary<int,int> pendingMaterials=new Dictionary<int,int>();
    int gold, iron, commands, calls, allocated, printCount, printCharacters;
    readonly Dictionary<int,long> stocks;
    uint randomState;
    int furnaceTemperature=ForgeBalance.DefaultFurnaceTemperature;
    int currentLine=1, nextPieceId;
    enum Flow { Next, Break, Continue, Return, Finish }
    struct Signal { public Flow Flow; public object Value; public Signal(Flow flow,object value=null){Flow=flow;Value=value;} }
    BlueprintRuntime(BlueprintProgram program,int gold,int iron,string owner,uint seed,IReadOnlyDictionary<int,long> materialStocks=null,IReadOnlyDictionary<string,string> modules=null,ForgeTechnology technology=null,Func<double> clockHour=null,ForgeRunContext context=null)
    {this.context=context??new ForgeRunContext();startedAt=this.context.Seconds();this.clockHour=clockHour??(()=>8d);InitializeModules(modules,technology,program);stocks=materialStocks==null?null:new Dictionary<int,long>();if(materialStocks!=null)foreach(var item in materialStocks)stocks[item.Key]=item.Value;this.program=program;this.gold=gold;this.iron=iron;this.owner=owner;randomState=seed;result.debugOnly=program.DebugOnly;}
    public static ForgeMvp.Evaluation Evaluate(string source,int gold,int iron,string owner,uint seed,IReadOnlyDictionary<int,long> materialStocks=null,ForgeTechnology technology=null,IReadOnlyDictionary<string,string> modules=null,Func<double> clockHour=null,ForgeRunContext context=null)
    {
        BlueprintProgram program;
        try {program=new BlueprintValidator(new BlueprintParser(source,technology:technology).Parse(),technology).Validate();}
        catch(BlueprintException e){return Failure(new ForgeMvp.Evaluation(),e,true);}
        var runtime=new BlueprintRuntime(program,gold,iron,owner,seed,materialStocks,modules,technology,clockHour,context);
        try {
            runtime.Block(program.Body,runtime.result.values).GetAwaiter().GetResult();
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
    bool WhileCondition(object value){if(value is int number)return number!=0;return Bool(value);}
    ForgeMvp.Piece Piece(object value){if(!(value is ForgeMvp.Piece))Fail("작업물이 필요합니다.");var piece=(ForgeMvp.Piece)value;if(submittedPieces.Contains(piece))Fail("이미 제출한 작업물은 다시 사용할 수 없습니다.");return piece;}
    void Allocate(int count){if(count>CollectionLimit)Fail("컬렉션은 최대1024개입니다.");allocated+=count;if(allocated>16384)Fail("컬렉션 누적 복사 제한(16384)에 도달했습니다.");}
    object Copy(object value)
    {
        if(value is List<int> l){Allocate(l.Count);return new List<int>(l);}
        if(value is int[] t){Allocate(t.Length);return t.Clone();}
        if(value is HashSet<int> s){Allocate(s.Count);return new HashSet<int>(s);}
        if(value is Dictionary<int,int> d){Allocate(d.Count);return new Dictionary<int,int>(d);}
        return value;
    }
    async Task<Signal> Block(List<BlueprintStatement> body,Dictionary<string,object> env,bool inLoop=false)
    {
        foreach(var s in body){await BeforeStatement(s.Token);Tick(s.Token);
            if(s.Error!=null)throw new BlueprintException(s.Error.Line,s.Error.Message){ModuleName=currentModule.IsMain?null:currentModule.Name};
            switch(s.Kind){
            case BlueprintStatementKind.Import:
            case BlueprintStatementKind.FromImport:await Import(s,env);break;
            case BlueprintStatementKind.Def:break;
            case BlueprintStatementKind.Assign:
                var value=await Eval(s.Expr,env);
                if(s.Target.Kind==BlueprintExprKind.Name)env[s.Target.Name]=Copy(value);
                else if(s.Target.Kind==BlueprintExprKind.Slice)await AssignSlice(s.Target,value,env);
                else {
                    var target=await Eval(s.Target.Args[0],env);int key=Int(await Eval(s.Target.Args[1],env));int n=Int(value);
                    if(target is List<int> list){CheckIndex(key,list.Count);list[key]=n;}
                    else if(target is Dictionary<int,int> dict){if(!dict.ContainsKey(key)){Allocate(1);if(dict.Count>=CollectionLimit)Fail("컬렉션은 최대1024개입니다.");}dict[key]=n;}
                    else Fail("리스트와 딕셔너리만 변경할 수 있습니다.");
                }break;
            case BlueprintStatementKind.Call:await Eval(s.Expr,env);break;
            case BlueprintStatementKind.If:
                var branch=await Block(Bool(await Eval(s.Expr,env))?s.Body:s.Other,env,inLoop);if(branch.Flow!=Flow.Next)return branch;break;
            case BlueprintStatementKind.For:
                var iterable=Copy(await Eval(s.Expr,env));
                var items=iterable is int?null:Sequence(iterable);
                int count=iterable is int repetitions?Math.Max(0,repetitions):items.Count;
                for(int index=0;index<=count;index++){
                    if(index>0){await BeforeStatement(s.Token);Tick(s.Token);}
                    if(index==count)break;
                    env[s.Name]=items==null?index:items[index];var flow=await Block(s.Body,env,true);if(flow.Flow==Flow.Break)break;if(flow.Flow==Flow.Return||flow.Flow==Flow.Finish)return flow;
                }break;
            case BlueprintStatementKind.While:
                while(WhileCondition(await Eval(s.Expr,env))){Tick(s.Token);var flow=await Block(s.Body,env,true);if(flow.Flow==Flow.Break)break;if(flow.Flow==Flow.Return||flow.Flow==Flow.Finish)return flow;await BeforeStatement(s.Token);Tick(s.Token);}break;
            case BlueprintStatementKind.Break:return new Signal(Flow.Break);
            case BlueprintStatementKind.Continue:return new Signal(Flow.Continue);
            case BlueprintStatementKind.Return:return new Signal(Flow.Return,s.Expr==null?null:Copy(await Eval(s.Expr,env)));
            case BlueprintStatementKind.Finish:
                if(!currentModule.IsMain&&localNames==null)Fail("가져온 코드창의 최상위에서는 !로 제출할 수 없습니다. 사용자 함수 안에서 제출하거나 작업물을 반환하고 실행한 창에서 제출하세요. 직접 실행 전용 코드는 if __name__ == \"__main__\": 안에 작성하세요.");
                var submitted=await Eval(s.Expr,env);currentLine=s.Token.Line;
                var piece=submitted is int material?LoadMaterial(material):Piece(submitted);
                EnsureLive(piece);
                piece.data.materials=(int[])usedMaterials.Clone();
                result.product=ledger.CompleteNext(owner,piece.heat,piece.shaped,piece.traits,piece.material,piece.data,productionSeconds);
                AddStep(0,piece,0,0,result.product.DisplayName+" 제출 · 평가 "+ForgePricing.Quote(result.product).SalePrice+"G",true);submittedPieces.Add(piece);Array.Clear(usedMaterials,0,usedMaterials.Length);if(!inLoop)return new Signal(Flow.Finish);break;
        }}return new Signal(Flow.Next);
    }
    IList<int> Sequence(object value){if(value is List<int> l)return l;if(value is int[] t)return t;Fail("정수 리스트 또는 튜플이 필요합니다.");return null;}
    void CheckIndex(int key,int count){if(key<0||key>=count)Fail("인덱스 범위를 벗어났습니다: "+key);}
    async Task<object> Eval(BlueprintExpr e,Dictionary<string,object> env)
    {
        Tick(e.Token);
        switch(e.Kind){
            case BlueprintExprKind.Literal:return e.Value;
            case BlueprintExprKind.Name:return Lookup(e.Name,env);
            case BlueprintExprKind.Attribute:
                var target=await Eval(e.Args[0],env);currentLine=e.Token.Line;return Member(target,e.Name);
            case BlueprintExprKind.Unary:var unary=await Eval(e.Args[0],env);currentLine=e.Token.Line;return e.Token.Raw=="not"?(object)!Bool(unary):checked(-Int(unary));
            case BlueprintExprKind.Binary:return await Binary(e,env);
            case BlueprintExprKind.Call:
                object callable=e.Callee!=null?await Eval(e.Callee,env):e.Process==0&&e.Feature==BlueprintFeature.None?Lookup(e.Name,env):null;
                var args=new List<object>();foreach(var a in e.Args)args.Add(await Eval(a,env));currentLine=e.Token.Line;return e.Callee!=null||e.Process==0&&e.Feature==BlueprintFeature.None?await Invoke(callable,args):Call(e,args);
            case BlueprintExprKind.Slice:return await Slice(e,env);
            case BlueprintExprKind.Index:
                var container=await Eval(e.Args[0],env);int key=Int(await Eval(e.Args[1],env));currentLine=e.Token.Line;
                if(container is Dictionary<int,int> dict){if(!dict.TryGetValue(key,out int n))Fail("딕셔너리에 키가 없습니다: "+key);return n;}
                var seq=Sequence(container);CheckIndex(key,seq.Count);return seq[key];
            default:
                var numbers=new List<int>();foreach(var a in e.Args)numbers.Add(Int(await Eval(a,env)));currentLine=e.Token.Line;
                Allocate(e.Kind==BlueprintExprKind.Dict?numbers.Count/2:numbers.Count);
                if(e.Kind==BlueprintExprKind.List)return numbers;
                if(e.Kind==BlueprintExprKind.Tuple)return numbers.ToArray();
                if(e.Kind==BlueprintExprKind.Set)return new HashSet<int>(numbers);
                var map=new Dictionary<int,int>();for(int i=0;i<numbers.Count;i+=2)map[numbers[i]]=numbers[i+1];return map;
        }
    }
    async Task<object> Binary(BlueprintExpr e,Dictionary<string,object> env)
    {
        string op=e.Token.Raw;var a=await Eval(e.Args[0],env);
        if(op=="and"&&!Bool(a))return false;if(op=="or"&&Bool(a))return true;
        var b=await Eval(e.Args[1],env);currentLine=e.Token.Line;
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
        if(e.Feature==BlueprintFeature.SmithRun)return DispatchSmith(args);
        if(ForgeSmithLanguage.IsSmith(e.Feature)||e.Feature==BlueprintFeature.GiantRune){
            if(context.SmithCall==null)Fail("대장장이 함수는 실제 공방 실행에서 사용할 수 있습니다.");
            try{return context.SmithCall(e.Feature,args);}catch(ArgumentException error){Fail(error.Message);}
        }
        if(ForgeExpansion.IsExtended(e.Feature))return Extended(e.Feature,args);
        switch(e.Feature){
            case BlueprintFeature.IsDay:
                double dayHour=clockHour();return dayHour>=6 && dayHour<18 ? 1 : 0;
            case BlueprintFeature.NightTime:
                if(args.Count>1)Fail("24는 인자 없이 호출하거나 24(60)으로 사용하세요.");
                if(args.Count==1 && Int(args[0])!=60)Fail("시각 조회의 인자는 정수 60이어야 합니다.");
                double hour=clockHour();
                return args.Count==1 ? (int)Math.Floor(hour*60) : hour>=6 && hour<18 ? 0 : 1;
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
            case BlueprintFeature.Heat:
                var measured=Piece(args[0]);EnsureLive(measured);
                if(args.Count==1)return measured.heat;
                if(Int(args[1])!=1)Fail("적정 온도 조회는 100(검날, 1)로 사용하세요.");
                if(measured.material!=1&&measured.material!=7)Fail("철 또는 별철 검날의 적정 온도만 측정할 수 있습니다.");
                if(measured.data.idealTemperature==0)Fail("먼저 2(검날)로 한 번 가열한 뒤 적정 온도를 측정하세요.");
                return measured.data.idealTemperature;
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
        if(id==1)return LoadMaterial(Int(args[0]));
        var piece=Piece(args[0]);EnsureLive(piece);
        if(id==4){Cool(piece,Int(args[1]));return null;}
        int heat=piece.heat;bool shaped=piece.shaped;var traits=piece.traits;
        if(id==2){
            heat=furnaceTemperature;traits&=~ForgeTraits.AirCooled;
            piece.data.lastHammerTemperature=0;piece.data.hardness=0;piece.data.perfectQuench=false;
            if((piece.material==1||piece.material==7)&&piece.data.idealTemperature==0){
                randomState=unchecked(randomState*1664525u+1013904223u);
                piece.data.idealTemperature=ForgeBalance.MinimumForgeTemperature+(int)(randomState%(ForgeBalance.MaximumIdealTemperature-ForgeBalance.MinimumForgeTemperature+1));
            }
        }
        if(id==3 && heat>=ForgeBalance.MinimumForgeTemperature){shaped=true;piece.data.lastHammerTemperature=heat;}
        if(id==3 && piece.data.magic){double completed=Now+.8/Math.Max(1,context.Speed);double strength=StrengthAt(piece,completed)+3;if(strength>9)piece.data.cracked=true;piece.data.strength=Math.Min(9,strength);piece.data.lastStrike=completed; }

        // Validate before committing state. Forging consumes materials, never gold.
        piece.heat=heat;piece.shaped=shaped;piece.traits=traits;
        Record(id,piece,0,0,id==2?"화로 가열 · "+heat+"°C · 0G":id==3?(heat>=ForgeBalance.MinimumForgeTemperature?"타격 · 검날 성형":"타격 · 열 부족으로 성형 효과 없음"):"공기 냉각 · 0G",false);return null;
    }
    ForgeMvp.Piece LoadMaterial(int material)
    {
        var entry=ForgeMaterialGuide.Find(material);
        if(entry==null)Fail("등록되지 않은 재료 번호: "+material);
        if(material>1&&stocks!=null&&(!stocks.TryGetValue(material,out long amount)||amount<1))Fail(entry.Name+" 재료가 잠겨 있거나 보유량이 부족합니다.");
        var piece=new ForgeMvp.Piece{id=++nextPieceId,material=material};
        if(material==6){randomState=unchecked(randomState*1664525u+1013904223u);piece.data.resonanceTarget=((int)(randomState%9)+1)*100;}
        AddStep(1,piece,0,material==1?1:0,entry.Name+" 올리기 · 0G");
        if(material>1&&stocks!=null)stocks[material]--;
        if(material>0)usedMaterials[material]++;
        return piece;
    }
    void AddStep(int process,ForgeMvp.Piece piece,int cost,int ironCost,string text,bool finished=false)
    {if(!ledger.TrySpend(cost,ironCost,ref gold,ref iron))Fail("자금 또는 철 부족");Record(process,piece,cost,ironCost,text,finished);}
    void Record(int process,ForgeMvp.Piece piece,int cost,int ironCost,string text,bool finished)
    {
        float duration=finished?0:.8f/Math.Max(1,context.Speed);productionSeconds+=duration;
        result.steps.Add(new ForgeMvp.Step{line=currentLine,module=currentModule.IsMain?null:currentModule.Name,day=context.Day(),minute=(int)(clockHour()*60),warning=text.Contains("경고"),process=process,pieceId=piece.id,material=piece.material,materialCount=process==1&&piece.material!=0?1:0,heat=piece.heat,shaped=piece.shaped,traits=piece.traits,cost=cost,iron=ironCost,text=text,finished=finished,data=piece.data.Clone(),consumed=new Dictionary<int,int>(pendingMaterials),duration=duration,workSpeed=context.Speed,productionSeconds=productionSeconds});
        pendingMaterials.Clear();result.cost=ledger.CashCost;result.ironConsumed=ledger.IronConsumed;
    }
}
