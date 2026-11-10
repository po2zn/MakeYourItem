using System;
using System.Collections.Generic;

// Saved gameplay state; execution sessions and scene objects deliberately live elsewhere.
[Serializable]
public sealed class ForgeBlacksmiths
{
    [Serializable] public sealed class Smith { public int favor, required=1; public double decayAt; }
    [Serializable] public sealed class GiantRune { public int rune,x,y; }
    public const int FavorResearchCost=25,PositionResearchCost=25,DemandResearchCost=50;
    public static readonly int[] Counts={1,2,3,4,9,16};
    public static readonly int[] HireCosts={100,250,600,1500,3500};
    public List<Smith> smiths=new List<Smith>();
    public List<GiantRune> runes=new List<GiantRune>();
    public int hireTier, burningDay, burningSmith=-1;
    public double burningUntil;
    public bool favorUnlocked, positionUnlocked, demandUnlocked;
    public uint randomState=927451;
    public int Next(int count){randomState=unchecked(randomState*1664525u+1013904223u);return (int)((randomState>>8)%(uint)count);}
    public static int X(int id)=>id%4;
    public static int Y(int id)=>id/4;
    public void Normalize(double now)
    {
        hireTier=Math.Max(0,Math.Min(5,hireTier));
        if(smiths==null)smiths=new List<Smith>();if(runes==null)runes=new List<GiantRune>();
        while(smiths.Count<Counts[hireTier])smiths.Add(new Smith{required=Next(9)+1,decayAt=now});
        if(smiths.Count>Counts[hireTier])smiths.RemoveRange(Counts[hireTier],smiths.Count-Counts[hireTier]);
        for(int i=0;i<smiths.Count;i++){
            if(smiths[i]==null)smiths[i]=new Smith{required=Next(9)+1,decayAt=now};
            var s=smiths[i];s.favor=Math.Max(-100,Math.Min(100,s.favor));s.required=Math.Max(1,Math.Min(9,s.required));
            if(double.IsNaN(s.decayAt)||double.IsInfinity(s.decayAt)||s.decayAt>now)s.decayAt=now;
        }
        runes.RemoveAll(r=>r==null||r.rune<1||r.rune>9||r.x<0||r.x>3||r.y<0||r.y>3);
    }
    public void Tick(int day,double now)
    {
        Normalize(now);
        foreach(var s in smiths){double periods=Math.Floor(Math.Max(0,now-s.decayAt)/2);if(periods>0){s.favor=(int)Math.Max(-100,s.favor-periods);s.decayAt+=periods*2;}}
        if(burningDay!=day){burningDay=day;burningSmith=Next(smiths.Count);burningUntil=now+2;}
    }
    public bool Burning(int id,double now)=>burningSmith==id&&now<burningUntil;
    public bool Hire(ref int diamonds,double now)
    {
        if(hireTier>=5||diamonds<HireCosts[hireTier])return false;
        diamonds-=HireCosts[hireTier++];Normalize(now);return true;
    }
    public bool Gift(int id,int amount,ref int diamonds)
    {
        if(id<0||id>=smiths.Count||amount<1||amount>9||diamonds<amount)return false;
        var s=smiths[id];diamonds-=amount;s.favor=Math.Max(-100,Math.Min(100,s.favor+(amount==s.required?5:-10)));
        int next=Next(8)+1;if(next>=s.required)next++;s.required=next;return true;
    }
    public double RuneBonus(int id,int family)
    {
        double best=0;foreach(var r in runes)if((r.rune-1)%3+1==family&&Math.Abs(X(id)-r.x)+Math.Abs(Y(id)-r.y)<=1)
            best=Math.Max(best,((r.rune-1)/3+1)*.01);
        return best;
    }
    public void Chances(int id,double now,out double destruction,out double great)
    {
        int favor=smiths[id].favor;
        destruction=Math.Max(0,.01+Math.Max(0,-favor)*.0009-RuneBonus(id,2));
        great=Burning(id,now)?.5:Math.Min(.5,.01+Math.Max(0,favor)*.0009+RuneBonus(id,1));
    }
}

public static class ForgeSmithLanguage
{
    public static string Example(BlueprintFeature feature)
    {
        switch(feature){
            case BlueprintFeature.SmithGift:return "90901(0, 2)";
            case BlueprintFeature.SmithRun:return "333 sword():\n    p = 1(1)\n    2(p, 7)\n    3(p)\n    4(p, 0)\n    5(p, 2)\n    << p\nprint(90904(1, sword))";
            case BlueprintFeature.SmithBusy:return "print(90907(0))";
            case BlueprintFeature.GiantRune:return "90910(1, 0, 0)";
            default:foreach(var a in Aliases)if(a.Id==feature)return "print("+a.Symbol+"())";return "";
        }
    }
    public static bool IsSmith(BlueprintFeature f)=>f>=BlueprintFeature.SmithDemand&&f<=BlueprintFeature.SmithBusy;
    public static readonly BlueprintAlias[] Aliases={
        new BlueprintAlias(BlueprintFeature.SmithDemand,"smith_demand","90909",BlueprintSyntaxKind.Builtin,"요구 다이아 수량 1~9. 생략 시 현재 대장장이. 선물을 줄 때마다 재추첨합니다.","90909([대장장이 번호]) → 정수 · 다이아 연구",-1),
        new BlueprintAlias(BlueprintFeature.SmithGift,"smith_gift","90901",BlueprintSyntaxKind.Builtin,"다이아 선물. 정확하면 호감도 +5, 틀리면 -10. 수량은 1~9.","90901(수량) 또는 90901(번호, 수량) · 기본 제공",-1),
        new BlueprintAlias(BlueprintFeature.SmithFavor,"smith_favor","90902",BlueprintSyntaxKind.Builtin,"대장장이 호감도 -100~100 조회. 게임 시간 2시간마다 -1.","90902([번호]) → 정수 · 다이아 연구",-1),
        new BlueprintAlias(BlueprintFeature.SmithPosition,"smith_position","90903",BlueprintSyntaxKind.Builtin,"대장장이 공방 격자 좌표 (x,y). 첫 대장장이는 (0,0).", "90903([번호]) → 튜플 · 다이아 연구",-1),
        new BlueprintAlias(BlueprintFeature.SmithRun,"smith_run","90904",BlueprintSyntaxKind.Builtin,"지정한 대장장이에게 함수를 맡겨 병렬 실행. 함수명에 괄호를 붙이지 마세요. 이미 작업 중이면 -1.","90904(번호, 함수, 추가 인자...) → 번호 또는 -1 · 기본 제공",-1),
        new BlueprintAlias(BlueprintFeature.SmithCount,"smith_count","90905",BlueprintSyntaxKind.Builtin,"고용한 대장장이 수. 번호는 0부터 시작합니다.","90905() → 정수",0),
        new BlueprintAlias(BlueprintFeature.SmithSelf,"smith_self","90906",BlueprintSyntaxKind.Builtin,"현재 코드를 실행하는 대장장이 번호.","90906() → 정수",0),
        new BlueprintAlias(BlueprintFeature.SmithBusy,"smith_busy","90907",BlueprintSyntaxKind.Builtin,"대장장이가 작업 중이면 1, 대기 중이면 0.","90907(번호) → 정수",1),
        new BlueprintAlias(BlueprintFeature.GiantRune,"giant_rune","90910",BlueprintSyntaxKind.Builtin,"수정 1개와 등급별 다이아 25/50/75개로 거대한 룬 설치. 인접 한 칸에 계열별 대성공/파괴 방지/판매 보너스. 같은 계열은 가장 강한 효과만 적용.","90910(룬 번호 1~9, x 0~3, y 0~3) → None",3)
    };
}
