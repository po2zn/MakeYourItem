using UnityEngine;

// A non-modal, screen-space reference window. Only its own pointer input is captured.
public sealed class ForgeBookWindow
{
    public Rect Rect { get; private set; }
    public bool Dragging => gesture>=0;
    readonly ForgeGuideReader reader=new ForgeGuideReader();
    bool initialized;
    int gesture=-1,control;
    Vector2 startMouse;
    Rect startRect;
    public void EnsureInitialized()
    {
        if(initialized)return;
        initialized=true;
        Rect=new Rect(Mathf.Max(12,Screen.width-432),54,420,280);
    }
    public bool Contains(Vector2 mouse)
    {
        EnsureInitialized();
        return new Rect(Rect.x-4,Rect.y,Rect.width+8,Rect.height+4).Contains(mouse);
    }
    public void CancelDrag()
    {
        if(gesture>=0 && GUIUtility.hotControl==control)GUIUtility.hotControl=0;
        gesture=-1;
    }
    int ResizeEdge(Vector2 mouse)
    {
        if(new Rect(Rect.x-4,Rect.y+30,8,Rect.height-30).Contains(mouse))return 0;
        if(new Rect(Rect.xMax-4,Rect.y+30,8,Rect.height-30).Contains(mouse))return 1;
        if(new Rect(Rect.x,Rect.yMax-4,Rect.width,8).Contains(mouse))return 2;
        return -1;
    }
    public int CursorDirection(Vector2 mouse)
    {
        EnsureInitialized();
        int edge=Dragging?gesture:ResizeEdge(mouse);
        return edge==2?2:edge==0||edge==1?1:0;
    }
    public void Draw(ref bool open,Font font,int fontSize,bool englishGuideUnlocked)
    {
        if(!open){CancelDrag();return;}
        EnsureInitialized();
        var ev=Event.current;
        control=GUIUtility.GetControlID("GuideBookWindowDrag".GetHashCode(),FocusType.Passive);
        int hit=ResizeEdge(ev.mousePosition);
        if(hit<0&&new Rect(Rect.x,Rect.y,Rect.width-36,30).Contains(ev.mousePosition))hit=3;
        if(ev.type==EventType.MouseDown&&ev.button==0&&hit>=0){
            gesture=hit;startMouse=ev.mousePosition;startRect=Rect;GUIUtility.hotControl=control;ev.Use();
        }
        if(gesture>=0&&ev.type==EventType.MouseDrag){
            Vector2 delta=ev.mousePosition-startMouse;Rect next=startRect;
            if(gesture==0)next.xMin=Mathf.Min(startRect.xMin+delta.x,startRect.xMax-280);
            else if(gesture==1)next.width=Mathf.Max(280,startRect.width+delta.x);
            else if(gesture==2)next.height=Mathf.Max(180,startRect.height+delta.y);
            else {next.position=startRect.position+delta;next.y=Mathf.Max(44,next.y);next.x=Mathf.Clamp(next.x,24-next.width,Mathf.Max(0,Screen.width-24));}
            Rect=next;ev.Use();
        }
        if(gesture>=0&&ev.rawType==EventType.MouseUp){CancelDrag();ev.Use();}
        ForgeGuideUI.DrawBook(Rect,ref open,font,fontSize,reader,englishGuideUnlocked);
        if(!open)CancelDrag();
        if((Dragging||Contains(ev.mousePosition))&&(ev.isMouse||ev.type==EventType.ScrollWheel))ev.Use();
    }
}
