using UnityEngine;

// Workspace/HUD tooltip. Research trees use the serialized tooltip in ForgeTreePanel.
public static class ForgeTreeTooltip
{
    static GUIStyle titleStyle,bodyStyle;
    static void Fill(Rect rect,Color color)
    {
        Color previous=GUI.color;GUI.color=color;
        GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;
    }
    public static void Draw(Vector2 mouse,string title,string text,Font font)
    {
        if(Event.current.type!=EventType.Repaint)return;
        if(bodyStyle==null||bodyStyle.font!=font){
            bodyStyle=new GUIStyle(GUI.skin.label){font=font,fontSize=22,fontStyle=FontStyle.Normal,alignment=TextAnchor.UpperLeft,wordWrap=true,richText=false,padding=new RectOffset(4,4,4,4)};
            bodyStyle.normal.textColor=Color.white;bodyStyle.hover=bodyStyle.normal;
            titleStyle=new GUIStyle(bodyStyle){fontSize=28,fontStyle=FontStyle.Bold};
        }
        bodyStyle.fontSize=22;titleStyle.fontSize=28;
        float width=Mathf.Min(560,Mathf.Max(1,Screen.width-24));
        float inner=Mathf.Max(1,width-24);
        var heading=new GUIContent(title);var content=new GUIContent(text);
        float titleHeight=ForgeTextLayout.Height(title,titleStyle,inner);
        float bodyHeight=ForgeTextLayout.Height(text,bodyStyle,inner);
        float height=titleHeight+bodyHeight+34;
        // Keep the title larger and bold even when fitting a small screen.
        while(height>Screen.height-16&&bodyStyle.fontSize>8){
            bodyStyle.fontSize--;titleStyle.fontSize=bodyStyle.fontSize+6;
            titleHeight=ForgeTextLayout.Height(title,titleStyle,inner);bodyHeight=ForgeTextLayout.Height(text,bodyStyle,inner);
            height=titleHeight+bodyHeight+34;
        }
        float x=mouse.x+18,y=mouse.y+20;
        if(x+width>Screen.width-8)x=mouse.x-width-18;
        if(y+height>Screen.height-8)y=mouse.y-height-16;
        x=Mathf.Clamp(x,8,Mathf.Max(8,Screen.width-width-8));
        y=Mathf.Clamp(y,8,Mathf.Max(8,Screen.height-height-8));
        Fill(new Rect(x+3,y+3,width,height),new Color(0,0,0,.5f));
        Fill(new Rect(x,y,width,height),new Color(.30f,.40f,.50f));
        Fill(new Rect(x+1,y+1,width-2,height-2),new Color(.10f,.14f,.20f));
        ForgeTextLayout.Label(new Rect(x+12,y+12,inner,titleHeight),heading,titleStyle);
        ForgeTextLayout.Label(new Rect(x+12,y+22+titleHeight,inner,bodyHeight),content,bodyStyle);
    }
}
