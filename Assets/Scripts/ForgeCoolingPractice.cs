using System;
using System.Collections.Generic;

public sealed class ForgeCoolingPractice : IDisposable {
    readonly ForgeMvp.Piece original;readonly long water;readonly Dictionary<int,long> resources;
    public ForgeMvp.Piece Blade {get;private set;}
    public BlueprintRuntime.Session Session {get;private set;}
    public readonly BlueprintObservation Observation=new BlueprintObservation{Enabled=true};
    public readonly BlueprintBreakpoints Breakpoints=new BlueprintBreakpoints();
    public uint Generation {get;private set;}
    public long Remaining=>Session?.MaterialCount(8)??water;
    public long Spent=>water-Remaining;
    public ForgeCoolingPractice(ForgeMvp.Piece blade,long frostWater,bool unlocked,IReadOnlyDictionary<int,long> stocks=null){if(!unlocked)throw new ArgumentException("서리수 공급을 먼저 해금하세요.");if(blade==null||blade.data.cooling==null&&blade.data.dragonBone==null)throw new ArgumentException("냉각 도면이 있는 칼날을 선택하세요.");resources=new Dictionary<int,long>();if(stocks!=null)foreach(var pair in stocks)resources[pair.Key]=pair.Value;original=Copy(blade);water=Math.Max(0,frostWater);resources[8]=water;Reset();}
    public static ForgeMvp.Piece Copy(ForgeMvp.Piece p)=>new ForgeMvp.Piece{id=p.id,material=p.material,heat=p.heat,shaped=p.shaped,traits=p.traits,data=p.data.Clone()};
    public BlueprintRuntime.Session Start(string code){if(Session!=null)return Session;Session=BlueprintRuntime.StartSession(code,0,0,materialStocks:resources,context:new ForgeRunContext{PracticeBlade=Blade,CoolingUnlocked=Blade.material!=9,DragonUnlocked=Blade.material==9,Observation=Observation,Seconds=()=>0,Day=()=>1});return Session;}
    public void Reset(){Dispose();Blade=Copy(original);Observation.Clear();Breakpoints.Restart();}
    public void Dispose(){Generation++;Session?.Dispose();Session=null;}
}

// Receipts, not speculative interpreter state, own the displayed graph and animation.
public sealed class ForgeCoolingViewState {
    public ForgeMvp.Piece Blade {get;private set;}
    public uint Generation {get;private set;}
    public float BoneDuration;public int A=-1,B=-1,BonePosition=-1,BoneLeft,BoneRight;public int[] Path;public float Progress=1;public string Latest="";
    public void Clear(){Generation++;Blade=null;BonePosition=-1;A=B=-1;Path=null;Progress=1;Latest="칼날을 선택하거나 작업대에 올려주세요";}
    public void Select(ForgeMvp.Piece blade){Clear();if(blade!=null&&(blade.material==1||blade.material==7||blade.material==9)&&!blade.data.destroyed&&!blade.data.recovered&&(blade.data.cooling!=null||blade.data.dragonBone!=null)){Blade=ForgeCoolingPractice.Copy(blade);Latest=Blade.material==9?"용뼈 #"+Blade.id+" · "+Blade.data.dragonBone.State:"칼날 #"+Blade.id+" · 실제 통로 비용은 서리수 개수입니다.";}}
    public uint Receipt(ForgeMvp.Step step){
        if(step.finished||step.data!=null&&(step.data.destroyed||step.data.recovered)||step.process==1&&step.material!=1&&step.material!=7&&step.material!=9){Clear();return Generation;}
        if(step.data?.cooling==null&&step.data?.dragonBone==null)return Generation;
        if(Blade==null||Blade.id!=step.pieceId){Clear();}
        Blade=new ForgeMvp.Piece{id=step.pieceId,material=step.material,heat=step.heat,shaped=step.shaped,traits=step.traits,data=step.data.Clone()};
        if(step.material==9){BonePosition=step.bonePosition;BoneLeft=step.boneLeft;BoneRight=step.boneRight;Latest=step.line+"번째 줄: "+step.text;BoneDuration=step.duration;Progress=step.process==93&&!step.data.dragonBone.IsCut(step.bonePosition)?0:1;}
        if(step.coolingCall>0){A=step.coolingA;B=step.coolingB;Path=step.coolingPath==null?null:(int[])step.coolingPath.Clone();Progress=Path==null?1:0;Latest=step.line+"번째 줄: "+step.text;}
        return Generation;
    }
    public void Animate(uint generation,float progress){if(generation==Generation)Progress=Math.Max(0,Math.Min(1,progress));}
}
