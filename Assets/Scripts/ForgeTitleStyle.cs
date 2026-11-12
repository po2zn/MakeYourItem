using UnityEngine;

// Shared title palette follows the workshop's blue-grey windows and controls.
public static class ForgeTitleStyle
{
    static Texture2D normal,hover,pressed;
    public static readonly Color Background=new Color(.075f,.105f,.15f);
    public static readonly Color Panel=new Color(.15f,.20f,.27f);
    public static readonly Color Ink=new Color(.93f,.94f,.96f);
    static Texture2D Solid(Color color)
    {
        var texture=new Texture2D(1,1){name="Title UI fill",hideFlags=HideFlags.HideAndDontSave};
        texture.SetPixel(0,0,color);texture.Apply();return texture;
    }
    public static void Button(GUIStyle style)
    {
        if(!normal)normal=Solid(new Color(.24f,.32f,.41f));
        if(!hover)hover=Solid(new Color(.31f,.41f,.52f));
        if(!pressed)pressed=Solid(new Color(.18f,.25f,.33f));
        style.normal.background=normal;style.hover.background=hover;
        style.active.background=pressed;style.focused.background=hover;
        style.normal.textColor=style.hover.textColor=style.active.textColor=style.focused.textColor=Ink;
        style.border=new RectOffset();style.margin=new RectOffset(0,0,4,6);
    }
    public static void Fill(Rect rect,Color color)
    {
        var prior=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=prior;
    }
    public static void Window(Rect rect,bool header=true)
    {
        Fill(new Rect(rect.x+5,rect.y+7,rect.width,rect.height),new Color(0,0,0,.22f));
        Fill(rect,new Color(.35f,.43f,.53f));
        Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),Panel);
        if(header)Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,34),new Color(.21f,.28f,.36f));
    }
}
