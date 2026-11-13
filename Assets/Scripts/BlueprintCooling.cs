using System;
using System.Collections.Generic;
public sealed partial class BlueprintRuntime {
    void CoolingPermission(){if(moduleTechnology!=null?!moduleTechnology.MaterialAvailable(8):!context.CoolingUnlocked)Fail("서리수 공급을 먼저 해금하세요.");}
    ForgeCoolingGraph CoolingGraph(ForgeMvp.Piece p){EnsureLive(p);if(p.material!=1&&p.material!=7)Fail("철 또는 별철 칼날이 필요합니다.");if(p.data.cooling==null)Fail("이 칼날에는 냉각 도면이 없습니다. 새 칼날을 작업대에 올려주세요.");return p.data.cooling;}
    object CoolingCall(BlueprintFeature feature,List<object> args){
        CoolingPermission();var alias=BlueprintAliases.Find(feature);if(args.Count!=alias.Arity)Fail(alias.Symbol+"에 필요한 인자 수: "+alias.Arity);
        var p=Piece(args[0]);var graph=CoolingGraph(p);int a=-1,b=-1,cost=0;object returned=null;int[] path=null;string text;
        try{
            if(feature==BlueprintFeature.CoolingCount){returned=graph.Count;text="지점 수 "+returned;}
            else if(feature==BlueprintFeature.CoolingStart){returned=graph.Start;text="입구 "+returned;}
            else if(feature==BlueprintFeature.CoolingEnd){returned=graph.End;text="출구 "+returned;}
            else if(feature==BlueprintFeature.CoolingCost){a=Int(args[1]);b=Int(args[2]);int edge=graph.Cost(a,b);returned=edge;text="통로 "+a+" ↔ "+b+" · "+(edge<0?"연결 없음 (-1)":ForgeCoolingGraph.StockUnits(edge)+"개");}
            else {
                if(Int(args[1])!=8)Fail("FrostWater에는 기존 재료 번호 8을 전달하세요.");
                if(!p.shaped)Fail("먼저 칼날을 성형하세요.");if(p.coolingBusy)Fail("이미 냉각 중인 칼날입니다.");
                var sequence=Sequence(args[2]);cost=ForgeCoolingGraph.StockUnits(graph.PathCost(sequence));path=new int[sequence.Count];sequence.CopyTo(path,0);
                // All object/path/range checks precede the single inventory debit.
                Consume(8,cost);ApplyCooling(p,8);
                var receipt=result.steps[result.steps.Count-1];receipt.coolingCall=84;receipt.coolingCost=cost;receipt.coolingPath=path;
                receipt.text="84 냉각 · 경로 ["+string.Join(", ",path)+"] · 서리수 "+cost+"개 · "+receipt.text;
                if(session!=null){p.coolingBusy=true;receipt.coolingTarget=p;}
                return null;
            }
        }catch(ArgumentException error){Fail(error.Message);return null;}
        Action(int.Parse(alias.Symbol),p,alias.Symbol+" 반환 · "+text,0);var step=result.steps[result.steps.Count-1];step.coolingCall=int.Parse(alias.Symbol);step.coolingA=a;step.coolingB=b;step.coolingCost=returned is int n?n:0;return returned;
    }
}
