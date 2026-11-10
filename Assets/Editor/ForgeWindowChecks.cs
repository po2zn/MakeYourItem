using System;
using UnityEngine;

public static class ForgeWindowChecks
{
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Main()
    {
        Rect start=new Rect(100,80,500,300);
        var fixedWindow=new ForgeWindowFrame(new ForgeWindowRules(false,ForgeWindowEdges.Horizontal,340,220){Maximum=new Vector2(700,300)}){Bounds=start};
        foreach(int side in new[]{0,1}){
            float direction=side==0?-1:1;
            var larger=ForgeWindowGeometry.Resize(fixedWindow,start,side,new Vector2(direction*120,50));
            Require(larger.x==100&&larger.y==80&&larger.width==620&&larger.height==300,"anchored horizontal resize "+side);
            var smaller=ForgeWindowGeometry.Resize(fixedWindow,start,side,new Vector2(-direction*1000,0));
            Require(smaller.width==340&&smaller.position==start.position,"minimum size "+side);
            Require(ForgeWindowGeometry.Resize(fixedWindow,start,side,new Vector2(direction*1000,0)).width==700,"maximum size "+side);
        }
        Require(ForgeWindowGeometry.Move(fixedWindow,start,new Vector2(30,40))==start,"fixed title cannot move");
        Require(ForgeWindowGeometry.Resize(fixedWindow,start,2,new Vector2(0,90))==start,"vertical resize disabled");
        Require(ForgeWindowGeometry.HitResize(fixedWindow,start,new Vector2(100,200))==0,"left grip");
        Require(ForgeWindowGeometry.HitResize(fixedWindow,start,new Vector2(600,200))==1,"right grip");
        Require(ForgeWindowGeometry.HitResize(fixedWindow,start,new Vector2(300,380))==-1,"no bottom grip");
        Require(ForgeWindowGeometry.HitResize(fixedWindow,start,new Vector2(300,100))==-1,"title is not a grip");
        var movable=new ForgeWindowFrame(new ForgeWindowRules(true,ForgeWindowEdges.All,280,180,34)){Bounds=start};
        var left=ForgeWindowGeometry.Resize(movable,start,0,new Vector2(50,0));
        Require(left.x==150&&left.xMax==start.xMax&&left.width==450,"movable left edge keeps opposite side");
        Require(ForgeWindowGeometry.Resize(movable,start,2,new Vector2(0,-999)).height==180,"bottom minimum");
        Require(ForgeWindowGeometry.Move(movable,start,new Vector2(20,-10)).position==new Vector2(120,70),"move enabled");
        Require(ForgeWindowGeometry.HitResize(movable,start,new Vector2(100,90))==-1,"header button exclusion");
        Require(ForgeWindowGeometry.HitResize(movable,start,new Vector2(100,200))==0,"below-header grip");
        movable.Rules=new ForgeWindowRules(true,ForgeWindowEdges.None,280,180);
        Require(ForgeWindowGeometry.Resize(movable,start,1,new Vector2(100,0))==start,"collapsed cannot resize");
        Require(ForgeWindowGeometry.HitResize(movable,start,new Vector2(600,200))==-1,"collapsed has no grips");
        Rect external=start;
        var adapter=new ForgeWindowAdapter(()=>external,r=>external=r,fixedWindow.Rules);
        adapter.Bounds=ForgeWindowGeometry.Resize(adapter,start,1,new Vector2(60,0));
        Require(external.width==560&&adapter.Bounds==external,"adapter writes existing window state");
        fixedWindow.Rules=new ForgeWindowRules(false,ForgeWindowEdges.Horizontal,200,220){Maximum=new Vector2(200,300)};
        Require(ForgeWindowGeometry.Resize(fixedWindow,start,1,new Vector2(100,0)).width==200,"small screen bounds");
        Console.WriteLine("PASS windows: anchored/movable resize, minimum/maximum bounds, disabled edges, hit tests, header exclusion, adapter state and small screens");
        ForgeTutorialChecks.Main();
    }
}
