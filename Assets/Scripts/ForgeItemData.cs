using System;
using System.Collections.Generic;

[Serializable]
public sealed class ForgeItemData
{
    public ForgeDragonBone dragonBone;
    public ForgeStarlight starlight;
    public ForgeCoolingGraph cooling;
    public int idealTemperature, lastHammerTemperature;
    public bool perfectQuench;
    public int hardness, handle, charge, sealBonus, magicGrade, luckyDay, luckyCode, resonanceTarget=-1;
    public bool polished, plated, star, frost, cracked, refined, activated, magic, protectedItem, cursed, destroyed, recovered;
    public double strength, lastStrike=-100;
    public double smithSaleMultiplier=1;
    public int[] materials=new int[10];
    public List<int> seals=new List<int>(), runes=new List<int>();
    public ForgeItemData Clone(){var c=(ForgeItemData)MemberwiseClone();c.dragonBone=dragonBone?.Clone();c.starlight=starlight?.Clone();c.cooling=cooling?.Clone();c.materials=new int[10];if(materials!=null)Array.Copy(materials,c.materials,Math.Min(materials.Length,c.materials.Length));c.seals=seals==null?new List<int>():new List<int>(seals);c.runes=runes==null?new List<int>():new List<int>(runes);return c;}
}
public sealed class ForgeRunContext
{
    public Func<BlueprintFeature,List<object>,object> SmithCall;
    public Func<int,Func<ForgeRunContext,BlueprintRuntime.Session>,int> StartSmith;
    public Action<BlueprintRuntime.Session> RefreshResources;
    public Func<int> Day=()=>1;
    public Func<double> Seconds=()=>0;
    public int Seed=72431;
    public float Speed=1;
    public float StarSwapSeconds=.4f,StarRemoveSeconds=.25f;
    public ForgeMvp.Piece PracticePiece,PracticeBlade;
    public bool CoolingUnlocked,DragonUnlocked;
    public ForgeDragonSettings DragonSettings=new ForgeDragonSettings();
    public ForgeCoolingSettings CoolingSettings=new ForgeCoolingSettings();
    public BlueprintObservation Observation;
    public int LuckyCode => ForgeLuck.Code(Seed,Day());
}
public static class ForgeLuck
{
    public static int Code(int seed,int day)
    {
        // Retain the old query number so existing seeds keep the same daily codes.
        var excluded=new HashSet<int>{666,404,777,778};
        foreach(var a in BlueprintAliases.All)if(int.TryParse(a.Symbol,out int n))excluded.Add(n);
        var candidates=new List<int>();for(int n=100;n<=999;n++)if(!excluded.Contains(n))candidates.Add(n);
        // Shuffle the eligible numbers once per seed; successive dates visit different entries.
        uint state=unchecked((uint)seed);
        for(int i=candidates.Count-1;i>0;i--){state=unchecked(state*1664525u+1013904223u);int j=(int)(state%(uint)(i+1));int n=candidates[i];candidates[i]=candidates[j];candidates[j]=n;}
        return candidates[(Math.Max(1,day)-1)%candidates.Count];
    }
}
