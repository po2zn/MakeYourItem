using System;
using System.Collections.Generic;

[Serializable]
public sealed class ForgeItemData
{
    public int hardness, handle, charge, sealBonus, magicGrade, luckyDay, luckyCode, resonanceTarget=-1;
    public bool polished, plated, star, frost, cracked, refined, activated, magic, protectedItem, cursed, destroyed, recovered;
    public double strength, lastStrike=-100;
    public int[] materials=new int[10];
    public List<int> seals=new List<int>(), runes=new List<int>();
    public ForgeItemData Clone(){var c=(ForgeItemData)MemberwiseClone();c.materials=(int[])materials.Clone();c.seals=new List<int>(seals);c.runes=new List<int>(runes);return c;}
}
public sealed class ForgeRunContext
{
    public Func<int> Day=()=>1;
    public Func<double> Seconds=()=>0;
    public int Seed=72431;
    public float Speed=1;
    public int LuckyCode => ForgeLuck.Code(Seed,Day());
}
public static class ForgeLuck
{
    public static int Code(int seed,int day)
    {
        var excluded=new HashSet<int>{666,404,777};
        foreach(var a in BlueprintAliases.All)if(int.TryParse(a.Symbol,out int n))excluded.Add(n);
        var candidates=new List<int>();for(int n=100;n<=999;n++)if(!excluded.Contains(n))candidates.Add(n);
        // Shuffle the eligible numbers once per seed; successive dates visit different entries.
        uint state=unchecked((uint)seed);
        for(int i=candidates.Count-1;i>0;i--){state=unchecked(state*1664525u+1013904223u);int j=(int)(state%(uint)(i+1));int n=candidates[i];candidates[i]=candidates[j];candidates[j]=n;}
        return candidates[(Math.Max(1,day)-1)%candidates.Count];
    }
}
