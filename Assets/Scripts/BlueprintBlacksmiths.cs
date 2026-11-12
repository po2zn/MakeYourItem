using System;
using System.Collections.Generic;

public sealed partial class BlueprintRuntime
{
    object DispatchSmith(List<object> args)
    {
        if(args.Count<2)Fail("90904(번호, 함수명, 추가 인자...) 형식으로 호출하세요.");
        int id=Int(args[0]);
        if(!(args[1] is Function))Fail("두 번째 인자는 호출하지 않은 함수명이어야 합니다.");
        var function=(Function)args[1];
        if(args.Count-2!=function.Definition.Parameters.Count)Fail("대장장이 함수의 인자 수가 맞지 않습니다.");
        if(context.StartSmith==null)Fail("대장장이 병렬 실행은 실제 공방에서만 가능합니다.");
        var arguments=args.GetRange(2,args.Count-2);
        foreach(var value in arguments)if(value is ForgeMvp.Piece)Fail("작업물은 대장장이 간에 전달할 수 없습니다. 각자 재료를 올리세요.");
        try{return context.StartSmith(id,childContext=>{
            var child=new BlueprintRuntime(program,gold,iron,owner+" · 대장장이 "+id,unchecked(randomState+(uint)id+1),stocks,moduleSources,moduleTechnology,clockHour,childContext);
            var modules=new Dictionary<Module,Module>();
            Func<Module,Module> clone=null;
            clone=original=>{
                if(modules.TryGetValue(original,out var existing))return existing;
                var copy=new Module{Name=original.Name,IsMain=original==function.Module,Globals=new Dictionary<string,object>(StringComparer.Ordinal)};
                modules.Add(original,copy);
                foreach(var pair in original.Globals){
                    if(pair.Value is Function f)copy.Globals[pair.Key]=new Function{Module=clone(f.Module),Definition=f.Definition,Locals=new HashSet<string>(f.Locals)};
                    else if(pair.Value is Module m)copy.Globals[pair.Key]=clone(m);
                    else if(!(pair.Value is ForgeMvp.Piece))copy.Globals[pair.Key]=child.Copy(pair.Value);
                }
                return copy;
            };
            var target=new Function{Module=clone(function.Module),Definition=function.Definition,Locals=new HashSet<string>(function.Locals)};
            child.currentModule=target.Module;
            var copied=new List<object>();foreach(var value in arguments)copied.Add(child.Copy(value));
            return new Session(child,false,async()=>{
                var returned=await child.Invoke(target,copied);
                if(returned is ForgeMvp.Piece piece&&!child.submittedPieces.Contains(piece)){
                    child.EnsureLive(piece);piece.data.materials=(int[])child.usedMaterials.Clone();
                    child.result.product=child.ledger.CompleteNext(child.owner,piece.heat,piece.shaped,piece.traits,piece.material,piece.data,child.productionSeconds);
                    child.AddStep(0,piece,0,0,"대장장이 제작 함수 반환 · 제출",true);
                    child.submittedPieces.Add(piece);Array.Clear(child.usedMaterials,0,child.usedMaterials.Length);
                }
            });
        });}catch(ArgumentException error){Fail(error.Message);return -1;}
    }
}
