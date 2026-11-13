using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ForgeCoolingSettings {
    [Range(4,8)] public int points=4;
    [Range(1,99)] public int minimumCost=1,maximumCost=9;
}
[Serializable]
public sealed class ForgeCoolingGraph {
    public const int MaxPathLength=128;
    // A graph cost unit is exactly one unit in the existing material-8 inventory.
    public const int StockUnitsPerCost=1;
    [SerializeField] int count,start,end;
    [SerializeField] int[] costs;
    [SerializeField] Vector2[] positions;
    public int Count=>count; public int Start=>start; public int End=>end;
    public Vector2 Position(int point){Check(point);return positions[point];}
    public void Check(int point){if(point<0||point>=count)throw new ArgumentException("지점 "+point+" 접근 불가. 유효 위치는 0~"+(count-1)+"입니다.");}
    public int Cost(int a,int b){Check(a);Check(b);return costs[a*count+b];}
    public ForgeCoolingGraph Clone(){var c=(ForgeCoolingGraph)MemberwiseClone();c.costs=(int[])costs.Clone();c.positions=(Vector2[])positions.Clone();return c;}
    public static int StockUnits(int cost)=>checked(cost*StockUnitsPerCost);
    public int PathCost(IList<int> path){
        if(path==null||path.Count<2||path.Count>MaxPathLength)throw new ArgumentException("경로는 정수 지점 2~"+MaxPathLength+"개로 구성하세요.");
        foreach(int point in path)Check(point);
        if(path[0]!=start||path[path.Count-1]!=end)throw new ArgumentException("경로는 입구 "+start+"에서 시작해 출구 "+end+"에서 끝나야 합니다.");
        int sum=0;for(int i=1;i<path.Count;i++){int cost=Cost(path[i-1],path[i]);if(cost<1)throw new ArgumentException("경로의 "+path[i-1]+"번과 "+path[i]+"번 지점이 연결되지 않았습니다.");sum=checked(sum+cost);}return sum;
    }
    public static ForgeCoolingGraph Legacy(string owner,int material){uint seed=2166136261u;foreach(char c in owner??"")seed=unchecked((seed^c)*16777619u);return Create(seed^(uint)material);}
    public static ForgeCoolingGraph Create(uint seed,ForgeCoolingSettings settings=null){
        settings=settings??new ForgeCoolingSettings();int n=Math.Max(4,Math.Min(8,settings.points));
        int low=Math.Max(1,Math.Min(99,settings.minimumCost)),high=Math.Max(low,Math.Min(99,settings.maximumCost));
        var g=new ForgeCoolingGraph{count=n,start=0,end=n-1,costs=new int[n*n],positions=new Vector2[n]};for(int i=0;i<n*n;i++)g.costs[i]=-1;
        // Two side rails form a planar cycle; the inlet and outlet sit at the blade's tips.
        g.positions[0]=new Vector2(.5f,.9f);g.positions[n-1]=new Vector2(.5f,.1f);
        int left=(n-2+1)/2,right=n-2-left;
        for(int i=1;i<n-1;i++){bool l=i<=left;int row=l?i:i-left;int rows=l?left:right;g.positions[i]=new Vector2(l?.28f:.72f,.9f-.8f*row/(rows+1));}
        int NextCost(){seed=unchecked(seed*1664525u+1013904223u);return low+(int)(seed%(uint)(high-low+1));}
        void Edge(int a,int b,int cost){if(a==b||g.costs[a*n+b]>0)return;g.costs[a*n+b]=g.costs[b*n+a]=cost;}
        int previous=0;for(int i=1;i<=left;i++){Edge(previous,i,NextCost());previous=i;}Edge(previous,n-1,NextCost());
        previous=0;for(int i=left+1;i<n-1;i++){Edge(previous,i,NextCost());previous=i;}Edge(previous,n-1,NextCost());
        if(n>4)Edge(left,left+1,NextCost());
        return g;
    }
    public static ForgeCoolingGraph Tutorial(){var g=Create(1);g.costs[1]=g.costs[4]=2;g.costs[7]=g.costs[13]=9;g.costs[2]=g.costs[8]=5;g.costs[11]=g.costs[14]=1;return g;}
    public List<int> DefaultPath(){var path=new List<int>{0};int left=(count-1)/2;for(int i=1;i<=left;i++)path.Add(i);path.Add(end);return path;}
    public static bool IsFeature(BlueprintFeature f)=>f>=BlueprintFeature.CoolingCount&&f<=BlueprintFeature.CoolingRun;
}
