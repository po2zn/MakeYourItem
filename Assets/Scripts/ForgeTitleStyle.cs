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
    public static void Logo(Rect rect,GUIStyle basis)
    {
        const string title="Forge(777)";
        var style=new GUIStyle(basis){wordWrap=false,fontStyle=FontStyle.Bold,fontSize=Mathf.Clamp(Mathf.RoundToInt(rect.width*.145f),24,96),padding=new RectOffset(),alignment=TextAnchor.MiddleCenter};
        while(style.fontSize>12&&style.CalcSize(new GUIContent(title)).x>rect.width-12)style.fontSize--;
        SetLogoColor(style,new Color(.025f,.035f,.05f,.95f));
        var shadow=rect;shadow.position+=new Vector2(3,5);GUI.Label(shadow,title,style);
        // A compact outline keeps the wordmark legible against the live workshop.
        foreach(var offset in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down}){
            var edge=rect;edge.position+=offset*2;GUI.Label(edge,title,style);
        }
        SetLogoColor(style,new Color(1f,.91f,.72f));GUI.Label(rect,title,style);
        float phase=Mathf.Repeat(Time.unscaledTime,6f)/1.3f;
        if(phase<1f){
            float center=Mathf.Lerp(-.15f,1.15f,phase)*rect.width;
            for(int band=0;band<7;band++){
                float width=rect.width*.022f;
                float left=center+(band-3)*width;
                float x=Mathf.Max(0,left),right=Mathf.Min(rect.width,left+width);
                if(right<=x)continue;
                SetLogoColor(style,Color.Lerp(new Color(1f,.91f,.72f),Color.white,(1f-Mathf.Abs(band-3)/4f)*.85f));
                GUI.BeginGroup(new Rect(rect.x+x,rect.y,right-x,rect.height));
                GUI.Label(new Rect(-x,0,rect.width,rect.height),title,style);GUI.EndGroup();
            }
        }
    }
    static void SetLogoColor(GUIStyle style,Color color)
    {
        // Labels also use hover states: keep every layer of the logo unchanged.
        foreach(var state in new[]{style.normal,style.hover,style.active,style.focused,
            style.onNormal,style.onHover,style.onActive,style.onFocused}){
            state.textColor=color;state.background=null;
#if UNITY_EDITOR
state.scaledBackgrounds=new Texture2D[0];
#endif

        }
    }
    public static void Window(Rect rect,bool header=true)
    {
        Fill(new Rect(rect.x+5,rect.y+7,rect.width,rect.height),new Color(0,0,0,.22f));
        Fill(rect,new Color(.35f,.43f,.53f));
        Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),Panel);
        if(header)Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,34),new Color(.21f,.28f,.36f));
    }
}
