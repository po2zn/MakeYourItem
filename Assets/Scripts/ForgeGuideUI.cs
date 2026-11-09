using UnityEngine;

// Code-drawn icons avoid depending on emoji glyphs in the user's installed fonts.
public static class ForgeGuideUI
{
    public enum Icon { Add, Shop, Book }
    static void Fill(Rect rect,Color color)
    {Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    static void Stroke(Rect rect,Color color,float width=1.5f)
    {
        Fill(new Rect(rect.x,rect.y,rect.width,width),color);
        Fill(new Rect(rect.x,rect.yMax-width,rect.width,width),color);
        Fill(new Rect(rect.x,rect.y,width,rect.height),color);
        Fill(new Rect(rect.xMax-width,rect.y,width,rect.height),color);
    }
    public static bool IconButton(Rect rect,Icon icon,string tooltip,bool selected=false)
    {
        Color old=GUI.backgroundColor;
        if(selected)GUI.backgroundColor=new Color(.60f,.74f,.85f);
        bool clicked=GUI.Button(rect,new GUIContent("",tooltip));
        GUI.backgroundColor=old;
        if(Event.current.type!=EventType.Repaint)return clicked;
        Matrix4x4 previousMatrix=GUI.matrix;
        float iconScale=Mathf.Min(rect.width-16,rect.height-16)/24f;
        GUI.matrix=previousMatrix*Matrix4x4.TRS(new Vector3(rect.center.x-12*iconScale,rect.center.y-12*iconScale,0),Quaternion.identity,new Vector3(iconScale,iconScale,1));
        Rect box=new Rect(0,0,24,24);
        Color ink=new Color(.93f,.94f,.96f);
        if(icon==Icon.Add){Fill(new Rect(box.x+11,box.y+4,2,16),ink);Fill(new Rect(box.x+4,box.y+11,16,2),ink);}
        else if(icon==Icon.Book){
            Stroke(new Rect(box.x+2,box.y+4,9,16),ink);
            Stroke(new Rect(box.x+13,box.y+4,9,16),ink);
            Fill(new Rect(box.x+11,box.y+5,2,17),ink);
            Fill(new Rect(box.x+4,box.y+8,5,1),ink);Fill(new Rect(box.x+15,box.y+8,5,1),ink);
        }else{
            Stroke(new Rect(box.x+4,box.y+10,16,12),ink);
            Fill(new Rect(box.x+4,box.y+3,16,2),ink);
            for(int i=0;i<5;i++)Fill(new Rect(box.x+2+i*4,box.y+6,3,5),ink);
            Stroke(new Rect(box.x+7,box.y+14,4,8),ink,1);
            Stroke(new Rect(box.x+14,box.y+14,4,4),ink,1);
        }
        GUI.matrix=previousMatrix;
        return clicked;
    }
    public static void DrawBook(Rect window,ref bool open,ref bool collapsed,Font font,int fontSize,ForgeGuideReader reader,bool englishGuideUnlocked)
    {
        if(!open)return;
        Rect book=new Rect(window.x,window.y+30,window.width,window.height-30);
        Fill(new Rect(window.x,window.y,window.width,30),new Color(.16f,.18f,.22f,.98f));
        var title=new GUIStyle(GUI.skin.label){font=font,fontSize=fontSize+4,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,richText=false};
        title.hover.textColor=title.normal.textColor;
        GUI.Label(new Rect(window.x+10,window.y+2,window.width-78,26),"도면 안내서",title);
        bool wasCollapsed=collapsed;
        if(GUI.Button(new Rect(window.xMax-60,window.y+3,26,24),new GUIContent("−",collapsed?"안내서 펼치기":"안내서 접기")))collapsed=!collapsed;
        if(GUI.Button(new Rect(window.xMax-30,window.y+3,26,24),new GUIContent("×","안내서 닫기")))open=false;
        if(collapsed||wasCollapsed||!open)return;
        Fill(new Rect(book.x+4,book.y+5,book.width,book.height),new Color(0,0,0,.25f));
        Fill(book,new Color(.25f,.16f,.11f));
        Stroke(book,new Color(.47f,.31f,.19f),2);
        float pageWidth=Mathf.Max(1,(book.width-26)/2);
        Rect left=new Rect(book.x+9,book.y+9,pageWidth,Mathf.Max(1,book.height-18));
        Rect right=new Rect(book.center.x+4,book.y+9,pageWidth,Mathf.Max(1,book.height-18));
        Fill(left,new Color(.93f,.89f,.78f));Fill(right,new Color(.97f,.94f,.85f));
        // Page decoration remains behind the scrollable reading panel.
        Fill(new Rect(left.xMax-4,left.y,4,left.height),new Color(.84f,.78f,.65f));
        Fill(new Rect(right.x,right.y,4,right.height),new Color(.88f,.82f,.70f));
        Fill(new Rect(book.center.x-2,book.y+8,4,book.height-16),new Color(.36f,.25f,.16f));
        reader.Draw(new Rect(book.x+10,book.y+10,book.width-20,book.height-20),font,fontSize,englishGuideUnlocked);
    }
}
