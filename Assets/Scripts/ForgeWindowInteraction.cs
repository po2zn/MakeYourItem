using System;
using UnityEngine;

[Flags]
public enum ForgeWindowEdges { None=0, Left=1, Right=2, Bottom=4, Horizontal=Left|Right, All=Horizontal|Bottom }

public struct ForgeWindowRules
{
    public bool Movable;
    public ForgeWindowEdges Edges;
    public Vector2 Minimum,Maximum;
    public float HeaderHeight,Grip;
    public ForgeWindowRules(bool movable,ForgeWindowEdges edges,float minimumWidth,float minimumHeight,float headerHeight=0,float grip=8)
    {Movable=movable;Edges=edges;Minimum=new Vector2(minimumWidth,minimumHeight);Maximum=new Vector2(float.PositiveInfinity,float.PositiveInfinity);HeaderHeight=headerHeight;Grip=grip;}
}

// Bounds and pointer coordinates must be expressed in the same space.
// Owners retain their z-order, buttons, scrolling and workspace transforms.
public interface IForgeWindow
{
    Rect Bounds { get; set; }
    ForgeWindowRules Rules { get; }
}

public sealed class ForgeWindowFrame : IForgeWindow
{
    public Rect Bounds { get; set; }
    public ForgeWindowRules Rules { get; set; }
    public ForgeWindowFrame(ForgeWindowRules rules){Rules=rules;}
}

public sealed class ForgeWindowAdapter : IForgeWindow
{
    readonly Func<Rect> read;
    readonly Action<Rect> write;
    public Rect Bounds {get=>read();set=>write(value);}
    public ForgeWindowRules Rules { get; }
    public ForgeWindowAdapter(Func<Rect> read,Action<Rect> write,ForgeWindowRules rules)
    {this.read=read;this.write=write;Rules=rules;}
}

public static class ForgeWindowGeometry
{
    public static int HitResize(IForgeWindow window,Rect bounds,Vector2 pointer)
    {
        var rules=window.Rules;float half=rules.Grip*.5f;
        float top=bounds.y+rules.HeaderHeight,height=Mathf.Max(0,bounds.height-rules.HeaderHeight);
        if((rules.Edges&ForgeWindowEdges.Bottom)!=0&&new Rect(bounds.x,bounds.yMax-half,bounds.width,rules.Grip).Contains(pointer))return 2;
        if((rules.Edges&ForgeWindowEdges.Left)!=0&&new Rect(bounds.x-half,top,rules.Grip,height).Contains(pointer))return 0;
        if((rules.Edges&ForgeWindowEdges.Right)!=0&&new Rect(bounds.xMax-half,top,rules.Grip,height).Contains(pointer))return 1;
        return -1;
    }
    public static Rect Resize(IForgeWindow window,Rect start,int edge,Vector2 delta)
    {
        var rules=window.Rules;var next=start;
        if(edge<0||edge>2||(rules.Edges&(ForgeWindowEdges)(1<<edge))==0)return next;
        if(edge<2){
            float width=Mathf.Clamp(start.width+(edge==0?-delta.x:delta.x),rules.Minimum.x,Mathf.Max(rules.Minimum.x,rules.Maximum.x));
            // Anchored windows keep their origin even when the left grip is used.
            if(edge==0&&rules.Movable)next.x=start.xMax-width;
            next.width=width;
        }else next.height=Mathf.Clamp(start.height+delta.y,rules.Minimum.y,Mathf.Max(rules.Minimum.y,rules.Maximum.y));
        return next;
    }
    public static Rect Move(IForgeWindow window,Rect start,Vector2 delta)
    {if(window.Rules.Movable)start.position+=delta;return start;}
    public static int CursorDirection(int edge)=>edge==2?2:edge==0||edge==1?1:0;
}

// Reusable IMGUI pointer capture for independently drawn windows.
public sealed class ForgeWindowInteraction
{
    int edge=-1,control;
    Rect start;
    Vector2 pointer;
    public bool Active=>edge>=0;
    public void Cancel(){if(Active&&GUIUtility.hotControl==control)GUIUtility.hotControl=0;edge=-1;}
    public int Cursor(IForgeWindow window,Vector2 mouse)=>ForgeWindowGeometry.CursorDirection(Active?edge:ForgeWindowGeometry.HitResize(window,window.Bounds,mouse));
    public void Handle(IForgeWindow window,int hint,Rect moveArea,bool enabled=true)
    {
        var ev=Event.current;control=GUIUtility.GetControlID(hint,FocusType.Passive);
        if(!enabled){Cancel();return;}
        if(Active&&GUIUtility.hotControl!=control){edge=-1;return;}
        int hit=ForgeWindowGeometry.HitResize(window,window.Bounds,ev.mousePosition);
        if(hit<0&&window.Rules.Movable&&moveArea.Contains(ev.mousePosition))hit=3;
        if(ev.type==EventType.MouseDown&&ev.button==0&&hit>=0&&GUIUtility.hotControl==0){
            edge=hit;start=window.Bounds;pointer=ev.mousePosition;GUIUtility.hotControl=control;GUI.FocusControl(null);ev.Use();
        }
        if(Active&&ev.type==EventType.MouseDrag&&ev.button==0){
            Vector2 delta=ev.mousePosition-pointer;
            window.Bounds=edge==3?ForgeWindowGeometry.Move(window,start,delta):ForgeWindowGeometry.Resize(window,start,edge,delta);ev.Use();
        }
        if(Active&&ev.rawType==EventType.MouseUp&&ev.button==0){Cancel();ev.Use();}
    }
}
