using UnityEngine;

// A non-modal, workspace-space reference window. Only its own pointer input is captured.
public sealed class ForgeBookWindow : IForgeWindow
{
    public Rect Rect { get; private set; }
    public bool Dragging => interaction.Active;
    Rect IForgeWindow.Bounds {get=>Rect;set=>Rect=value;}
    public ForgeWindowRules Rules => new ForgeWindowRules(true,collapsed?ForgeWindowEdges.None:ForgeWindowEdges.All,280,180,ForgeGuideUI.HeaderHeight);
    readonly ForgeWindowInteraction interaction=new ForgeWindowInteraction();
    readonly ForgeGuideReader reader=new ForgeGuideReader();
    bool initialized,collapsed;
    float expandedHeight=ForgeGuideReader.DefaultWindowHeight;
    public void EnsureInitialized(Vector2 position)
    {
        if(initialized)return;
        initialized=true;
        position.y-=ForgeGuideUI.HeaderHeight-30;
        Rect=new Rect(position,new Vector2(420,ForgeGuideReader.DefaultWindowHeight));
    }
    public void CenterAt(Vector2 center)
    {
        EnsureInitialized(Vector2.zero);
        CancelDrag();
        Rect bounds=Rect;
        bounds.center=center;
        Rect=bounds;
    }
    public bool Contains(Vector2 mouse)
    {
        return new Rect(Rect.x-4,Rect.y,Rect.width+8,Rect.height+4).Contains(mouse)
            || !collapsed && reader.ShowBookmarks && BookmarkRail.Contains(mouse);
    }
    Rect BookmarkRail => ForgeGuideReader.BookmarkRail(new Rect(Rect.x+10,Rect.y+ForgeGuideUI.HeaderHeight+10,Rect.width-20,Mathf.Max(1,Rect.height-ForgeGuideUI.HeaderHeight-20)));
    public void CancelDrag()
    {
        interaction.Cancel();
    }
    int ResizeEdge(Vector2 mouse)
    {
        if(collapsed||reader.ShowBookmarks&&BookmarkRail.Contains(mouse))return -1;
        return ForgeWindowGeometry.HitResize(this,Rect,mouse);
    }
    public int CursorDirection(Vector2 mouse)
    {
        return Dragging?interaction.Cursor(this,mouse):ForgeWindowGeometry.CursorDirection(ResizeEdge(mouse));
    }
    public void Draw(ref bool open,Font font,int fontSize,bool englishGuideUnlocked)
    {
        if(!open){CancelDrag();return;}
        var ev=Event.current;
        interaction.Handle(this,"GuideBookWindowDrag".GetHashCode(),new Rect(Rect.x,Rect.y,Rect.width-ForgeGuideUI.HeaderControlsWidth,ForgeGuideUI.HeaderHeight),Dragging||!reader.ShowBookmarks||!BookmarkRail.Contains(ev.mousePosition));
        bool wasCollapsed=collapsed;
        ForgeGuideUI.DrawBook(Rect,ref open,ref collapsed,font,fontSize,reader,englishGuideUnlocked);
        if(wasCollapsed!=collapsed){
            Rect resized=Rect;
            if(collapsed){expandedHeight=resized.height;resized.height=ForgeGuideUI.HeaderHeight;}
            else resized.height=expandedHeight;
            Rect=resized;CancelDrag();
        }
        if(!open)CancelDrag();
        if((Dragging||Contains(ev.mousePosition))&&(ev.isMouse||ev.type==EventType.ScrollWheel))ev.Use();
    }
}
