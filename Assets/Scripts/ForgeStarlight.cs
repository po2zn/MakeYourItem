using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ForgeStarlight
{
    [SerializeField] int[] initial;
    [SerializeField] List<int> values;
    [SerializeField] List<bool> revealed;
    [SerializeField] int swaps, removals;
    [SerializeField] double seconds;
    public int Count => values.Count;
    public int Swaps => swaps;
    public int Removals => removals;
    public double Seconds => seconds;
    public IReadOnlyList<int> Initial => Array.AsReadOnly(initial);
    public IReadOnlyList<int> Values => values.AsReadOnly();
    public bool Revealed(int index) => revealed[index];
    public ForgeStarlight(int[] source) {
        if(source==null||source.Length!=5)throw new ArgumentException("최초 별빛은 5개여야 합니다.");
        foreach(int value in source)if(value<0||value>9)throw new ArgumentException("별빛 값은 0~9입니다.");
        initial=(int[])source.Clone(); Reset();
    }
    public ForgeStarlight Clone(){var c=(ForgeStarlight)MemberwiseClone();c.initial=(int[])initial.Clone();c.values=new List<int>(values);c.revealed=new List<bool>(revealed);return c;}
    public void Reset(){values=new List<int>(initial);revealed=new List<bool>(new bool[initial.Length]);swaps=removals=0;seconds=0;}
    public void CheckIndex(int index){if(index<0||index>=Count)throw new ArgumentException("별빛 위치 "+index+"는 범위를 벗어났습니다. 현재 개수 "+Count+", 위치는 0부터 시작합니다.");}
    public int Read(int index){CheckIndex(index);revealed[index]=true;return values[index];}
    static double Cost(double cost){return double.IsNaN(cost)||double.IsInfinity(cost)?0:Math.Max(0,cost);}
    public void Swap(int a,int b,double cost){CheckIndex(a);CheckIndex(b);if(a==b)return;int v=values[a];values[a]=values[b];values[b]=v;bool r=revealed[a];revealed[a]=revealed[b];revealed[b]=r;swaps++;seconds+=Cost(cost);}
    public void Remove(int index,double cost){CheckIndex(index);values.RemoveAt(index);revealed.RemoveAt(index);removals++;seconds+=Cost(cost);}
    public bool Perfect {
        get {
            if(Count==0)return false;
            var required=new HashSet<int>(initial);required.Remove(0);
            if(required.Count==0||Count!=required.Count)return false;
            int previous=0;
            foreach(int value in values){if(value<=previous||!required.Contains(value))return false;previous=value;}
            return true;
        }
    }
    public int Bonus => Perfect?ForgeBalance.StarlightBonus:0;
    public static bool IsFeature(BlueprintFeature f)=>f>=BlueprintFeature.StarlightCount&&f<=BlueprintFeature.StarlightRemove;
}

// Owns only a clone; disposal invalidates all receipts before replacing that clone.
public sealed class ForgeStarlightPractice : IDisposable
{
    public ForgeMvp.Piece Piece {get;private set;}
    public BlueprintRuntime.Session Session {get;private set;}
    public uint Generation {get;private set;}
    public ForgeStarlightPractice(ForgeStarlight source){Piece=new ForgeMvp.Piece{id=1,material=7};Piece.data.starlight=source.Clone();Reset();}
    public void Reset(){Dispose();Piece=new ForgeMvp.Piece{id=1,material=7,data=Piece.data.Clone()};Piece.data.starlight.Reset();}
    public BlueprintRuntime.Session Run(string code,float swapSeconds,float removeSeconds){Dispose();Session=BlueprintRuntime.StartSession(code,0,0,materialStocks:new Dictionary<int,long>(),context:new ForgeRunContext{PracticePiece=Piece,StarSwapSeconds=swapSeconds,StarRemoveSeconds=removeSeconds});return Session;}
    public void Dispose(){Generation++;Session?.Dispose();Session=null;}
}
