using System;
using System.Collections.Generic;

// Saved gameplay state; execution sessions and scene objects deliberately live elsewhere.
[Serializable]
public sealed class ForgeBlacksmiths
{
    [Serializable] public sealed class Smith { public int favor, required=1; public double decayAt; }
    [Serializable] public sealed class GiantRune { public int rune; public double x,y,expiresAt; public bool timed; }
    public const double RuneLifetimeHours=48, RuneHalfExtent=.25;
    public static double GradeBonus(int grade,int effect)=>effect==1?(grade==1?.02:grade==2?.10:.15):effect==2?grade*.01:grade==1?.10:grade==2?.20:.25;
    public static bool ValidCoordinate(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value)&&value>=0&&value<=3;
    public static bool SamePosition(GiantRune r,double x,double y)=>Math.Abs(r.x-x)<1e-9&&Math.Abs(r.y-y)<1e-9;
    public double Remaining(int rune,double x,double y,double now){var r=runes.Find(v=>v.rune==rune&&SamePosition(v,x,y));return r==null?0:Math.Max(0,r.expiresAt-now); }
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
        runes.RemoveAll(r=>r==null||r.rune<1||r.rune>9||!ValidCoordinate(r.x)||!ValidCoordinate(r.y));
        foreach(var r in runes)if(!r.timed){r.expiresAt=now+RuneLifetimeHours;r.timed=true;}
        runes.RemoveAll(r=>double.IsNaN(r.expiresAt)||double.IsInfinity(r.expiresAt)||r.expiresAt<=now);
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
        var s=smiths[id];diamonds-=amount;s.favor=Math.Max(-100,Math.Min(100,s.favor+(amount==s.required?15:-10)));
        int next=Next(8)+1;if(next>=s.required)next++;s.required=next;return true;
    }
    public double RuneBonus(int id,int effect,double now=0)
    {
        double best=0;foreach(var r in runes)if((!r.timed||now<r.expiresAt)&&Math.Abs(X(id)-r.x)<=RuneHalfExtent+1e-9&&Math.Abs(Y(id)-r.y)<=RuneHalfExtent+1e-9)
            best=Math.Max(best,GradeBonus((r.rune-1)/3+1,effect));
        return best;
    }
    public void Chances(int id,double now,out double destruction,out double great)
    {
        int favor=smiths[id].favor;
        destruction=Math.Max(0,.01+Math.Max(0,-favor)*.0009-RuneBonus(id,2,now));
        great=Math.Min(1-destruction,(Burning(id,now)?.5:.01+Math.Max(0,favor)*.0009)+RuneBonus(id,1,now));
    }
}

public static class ForgeSmithLanguage
{
    public const string RunGuideBody="90904(번호, 함수명)\n고용한 대장장이에게 미리 만든 함수를 맡깁니다. 함수명 뒤에 괄호를 붙이지 마세요.\n\n90904(1, sword) → 1번(두 번째) 대장장이\n90904(2, sword) → 2번(세 번째) 대장장이\nsword() → 0번(첫 번째) 대장장이\n\n두 명을 함께 일시키려면 90904(1, sword)를 먼저 쓰고, 다음 줄에 sword()를 쓰세요. 0번은 코드창을 실행 중이므로 90904(0, sword)로 다시 맡길 수 없습니다.\n\n함수에 전달할 값이 있으면 뒤에 적습니다: 90904(1, sword, 1). 접수 성공 시 번호, 이미 작업 중이면 -1을 반환합니다.";
    public const string RunGuideNote="";
    public static string Example(BlueprintFeature feature)
    {
        switch(feature){
            case BlueprintFeature.SmithGift:return "90901(0, 2)";
            case BlueprintFeature.SmithRun:return "333 sword():\n    r = 1(1)\n    2(r)\n    3(r)\n    4(r, 0)\n    !r\n\n90904(1, sword) # 1번에게 맡기기\nsword() # 0번도 제작";
            case BlueprintFeature.SmithBusy:return "print(90907(0))";
            case BlueprintFeature.GiantRune:return "90910(1, 0.25, 0.25)";
            case BlueprintFeature.GiantRuneTime:return "print(90911(1, 0.25, 0.25))";
            default:foreach(var a in Aliases)if(a.Id==feature)return "print("+a.Symbol+"())";return "";
        }
    }
    public static bool IsGiantRune(BlueprintFeature f)=>f==BlueprintFeature.GiantRune||f==BlueprintFeature.GiantRuneTime;
    public static bool IsSmith(BlueprintFeature f)=>f>=BlueprintFeature.SmithDemand&&f<=BlueprintFeature.SmithBusy;
    public static string GuideTitle(BlueprintFeature feature)
    {
        switch(feature){
            case BlueprintFeature.SmithDemand:return "원하는 선물 수량 조회";
            case BlueprintFeature.SmithGift:return "다이아몬드 선물하기";
            case BlueprintFeature.SmithFavor:return "호감도 조회";
            case BlueprintFeature.SmithPosition:return "공방 위치 조회";
            case BlueprintFeature.SmithRun:return "다른 대장장이에게 작업 맡기기";
            case BlueprintFeature.SmithCount:return "고용 인원 조회";
            case BlueprintFeature.SmithSelf:return "현재 대장장이 번호 조회";
            case BlueprintFeature.SmithBusy:return "작업 중인지 확인";
            case BlueprintFeature.GiantRune:return "거대한 룬 설치";
            case BlueprintFeature.GiantRuneTime:return "거대한 룬 남은 시간 조회";
            default:throw new ArgumentOutOfRangeException(nameof(feature));
        }
    }
    public static readonly BlueprintAlias[] Aliases={
        new BlueprintAlias(BlueprintFeature.SmithDemand,"smith_demand","90909",BlueprintSyntaxKind.Builtin,"요구 다이아 수량 1~9. 생략 시 현재 대장장이. 선물을 줄 때마다 재추첨합니다.","90909([대장장이 번호]) → 정수 · 다이아 연구",-1),
        new BlueprintAlias(BlueprintFeature.SmithGift,"smith_gift","90901",BlueprintSyntaxKind.Builtin,"다이아 선물. 정확하면 호감도 +15, 틀리면 -10. 수량은 1~9.","90901(수량) 또는 90901(번호, 수량) · 기본 제공",-1),
        new BlueprintAlias(BlueprintFeature.SmithFavor,"smith_favor","90902",BlueprintSyntaxKind.Builtin,"대장장이 호감도 -100~100 조회. 게임 시간 2시간마다 -1.","90902([번호]) → 정수 · 다이아 연구",-1),
        new BlueprintAlias(BlueprintFeature.SmithPosition,"smith_position","90903",BlueprintSyntaxKind.Builtin,"대장장이 공방 격자 좌표 (x,y). 첫 대장장이는 (0,0).", "90903([번호]) → 튜플 · 다이아 연구",-1),
        new BlueprintAlias(BlueprintFeature.SmithRun,"smith_run","90904",BlueprintSyntaxKind.Builtin,"지정한 대장장이에게 함수를 맡겨 병렬 실행. 함수명에 괄호를 붙이지 마세요. 이미 작업 중이면 -1.","90904(번호, 함수, 추가 인자...) → 번호 또는 -1 · 기본 제공",-1),
        new BlueprintAlias(BlueprintFeature.SmithCount,"smith_count","90905",BlueprintSyntaxKind.Builtin,"고용한 대장장이 수. 번호는 0부터 시작합니다.","90905() → 정수",0),
        new BlueprintAlias(BlueprintFeature.SmithSelf,"smith_self","90906",BlueprintSyntaxKind.Builtin,"현재 코드를 실행하는 대장장이 번호.","90906() → 정수",0),
        new BlueprintAlias(BlueprintFeature.SmithBusy,"smith_busy","90907",BlueprintSyntaxKind.Builtin,"대장장이가 작업 중이면 1, 대기 중이면 0.","90907(번호) → 정수",1),
        new BlueprintAlias(BlueprintFeature.GiantRune,"giant_rune","90910",BlueprintSyntaxKind.Builtin,"룬 번호와 같은 번호의 재료 1개 + 다이아 25/50/75개로 설치합니다. x, y에 소수를 쓸 수 있습니다. 설치 좌표 중심으로 좌우·상하 0.25씩, 가로·세로 0.5 범위에 적용합니다.\n1등급(1~3): 대성공 +2%p / 파괴 −1%p / 판매가 +10%\n2등급(4~6): 대성공 +10%p / 파괴 −2%p / 판매가 +20%\n3등급(7~9): 대성공 +15%p / 파괴 −3%p / 판매가 +25%\n범위 안 대장장이에게 세 효과를 모두 적용하며, 겹치면 최고 등급만 적용합니다. 게임 시간 48시간 후 사라집니다. 정지는 시간을 소모하지 않습니다. 남은 시간: 90911(룬 번호, x, y).", "90910(룬 번호 1~9, x 0~3, y 0~3) → None",3),
        new BlueprintAlias(BlueprintFeature.GiantRuneTime,"giant_rune_time","90911",BlueprintSyntaxKind.Builtin,"지정한 룬 번호와 설치 좌표의 남은 게임 시간을 소수 시간 단위로 반환합니다. 24.5는 24시간 30분입니다. 없거나 만료됐다면 0을 반환합니다. 조회로 지속 시간이 늘어나지 않습니다. 룬 각인·거대한 룬 연구에 함께 포함됩니다.","90911(룬 번호 1~9, x 0~3, y 0~3) → 소수 시간",3)
    };
}
