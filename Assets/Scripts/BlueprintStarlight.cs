using System;
using System.Collections.Generic;
public sealed partial class BlueprintRuntime
{
    object StarlightCall(BlueprintFeature feature,List<object> args)
    {
        var alias=BlueprintAliases.Find(feature);
        if(args.Count!=alias.Arity)Fail(alias.Symbol+"에 필요한 인자 수: "+alias.Arity);
        if(feature==BlueprintFeature.StarlightEngrave&&moduleTechnology!=null&&!moduleTechnology.MaterialAvailable(7))Fail("별철 공급을 먼저 해금하세요.");
        var p=Piece(args[0]);EnsureLive(p);
        if(p.material!=7||p.data.starlight==null)Fail("70~74는 별철 검날 작업물에 사용하세요.");
        var light=p.data.starlight;int a=args.Count>1?Int(args[1]):-1,b=args.Count>2?Int(args[2]):-1;
        try{if(args.Count>1)light.CheckIndex(a);if(args.Count>2)light.CheckIndex(b);}catch(ArgumentException error){Fail(error.Message);}
        var before=light.Clone();float seconds=0;object returned=null;string message;
        switch(feature){
            case BlueprintFeature.StarlightEngrave:
                seconds=.8f;
                bool already=light.Engraved;bool earned=light.Engrave(clockHour());
                message=already?"각인 유지 · 추가 중첩 없음":earned?"각인 완료 · 판매 보너스 +100G":"각인 시간 아님 · 01:00 이상~05:00 미만에 다시 사용하세요";break;
            case BlueprintFeature.StarlightCount:returned=light.Count;message="개수 읽기 · "+light.Count+"개";break;
            case BlueprintFeature.StarlightRead:returned=light.Read(a);message="읽기 · 위치 "+a+" = "+returned;break;
            case BlueprintFeature.StarlightSwap:seconds=a==b?0:ValidStarSeconds(context.StarSwapSeconds);light.Swap(a,b,seconds/Math.Max(1,context.Speed));message=a==b?"같은 위치 · 변경 없음":"교환 · 위치 "+a+" ↔ "+b;break;
            default:seconds=ValidStarSeconds(context.StarRemoveSeconds);light.Remove(a,seconds/Math.Max(1,context.Speed));message="제거 · 위치 "+a+" · 뒤의 값이 앞으로 이동";break;
        }
        Action(int.Parse(alias.Symbol),p,"별빛 "+message,seconds);
        var step=result.steps[result.steps.Count-1];step.starlightBefore=before;step.starA=a;step.starB=b;
        return returned;
    }
    static float ValidStarSeconds(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(0,value);
}
