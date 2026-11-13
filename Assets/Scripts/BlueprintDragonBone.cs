using System;
using System.Collections.Generic;
public sealed partial class BlueprintRuntime {
 object BoneCall(BlueprintFeature feature,List<object> args){
 var alias=BlueprintAliases.Find(feature);if(args.Count!=alias.Arity)Fail(alias.Symbol+"에 필요한 인자 수: "+alias.Arity);
 if(!(context.DragonUnlocked||moduleTechnology!=null&&moduleTechnology.MaterialAvailable(9)))Fail("용뼈 공급을 먼저 해금하세요.");
 var p=Piece(args[0]);EnsureLive(p);if(p.material!=9||p.data.dragonBone==null)Fail("90~95는 용뼈 작업물에 사용하세요.");var b=p.data.dragonBone;int n=args.Count>1?Int(args[1]):0;
 try {
 object value=null;
 switch(feature){case BlueprintFeature.BoneLength:value=b.Length;break;case BlueprintFeature.BoneCount:value=b.Count;break;case BlueprintFeature.BonePosition:value=b.Position(n);break;case BlueprintFeature.BoneIsCut:value=b.IsCut(n);break;case BlueprintFeature.BoneCost:value=b.Cost;break;
 case BlueprintFeature.BoneCut:
 int left,right;int amount=b.CutLength(n,out left,out right);Action cancel;var complete=b.BeginCut(n,context.Speed,out cancel);
 Action(93,p,"용뼈 절단 · 위치 "+n+" · 구간 ["+left+", "+right+"] · 비용 "+amount,b.Duration(amount,1));var step=result.steps[result.steps.Count-1];step.bonePosition=n;step.boneLeft=left;step.boneRight=right;step.cancelWork=cancel;step.completeWork=()=>{complete();step.data=p.data.Clone();};if(session==null)step.CompleteWork();return null;
 }
 Action(int.Parse(alias.Symbol),p,alias.Symbol+" 반환값 "+value,0);return value;
 }catch(ArgumentException e){Fail(e.Message);return null;}
 }
 void BoneHeatReceipt(ForgeMvp.Piece p){if(p.material!=9||p.data.dragonBone==null)return;var step=result.steps[result.steps.Count-1];int temperature=p.heat;step.completeWork=()=>{if(!p.data.destroyed&&!p.data.recovered)p.data.dragonBone.Heated(temperature);step.data=p.data.Clone();};if(session==null)step.CompleteWork();}
}
