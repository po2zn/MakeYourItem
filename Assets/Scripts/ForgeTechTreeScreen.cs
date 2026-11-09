using UnityEngine;

// Independent canvas navigation; workshop state is never changed by this screen.
public sealed class ForgeTechTreeScreen
{
    Vector2 offset,dragStart,offsetStart;
    float zoom=1;
    int dragButton=-1,control;
    public void CancelDrag(){if(dragButton>=0&&GUIUtility.hotControl==control)GUIUtility.hotControl=0;dragButton=-1;}
    static void Fill(Rect rect,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    public bool Draw(Font font)
    {
        var ev=Event.current;
        control=GUIUtility.GetControlID("TechTreePan".GetHashCode(),FocusType.Passive);
        Rect canvas=new Rect(0,56,Screen.width,Mathf.Max(1,Screen.height-56));
        if(ev.type==EventType.ScrollWheel&&canvas.Contains(ev.mousePosition)&&dragButton<0){
            Vector2 anchor=(ev.mousePosition-canvas.position-offset)/zoom;
            zoom=Mathf.Clamp(zoom*Mathf.Pow(1.05f,-ev.delta.y),.4f,2f);
            offset=ev.mousePosition-canvas.position-anchor*zoom;ev.Use();
        }
        if(ev.type==EventType.MouseDown&&canvas.Contains(ev.mousePosition)&&(ev.button==0||ev.button==1)&&GUIUtility.hotControl==0){
            dragButton=ev.button;dragStart=ev.mousePosition;offsetStart=offset;GUIUtility.hotControl=control;GUI.FocusControl(null);ev.Use();
        }
        if(dragButton>=0&&GUIUtility.hotControl==control){
            if(ev.type==EventType.MouseDrag&&ev.button==dragButton){offset=offsetStart+ev.mousePosition-dragStart;ev.Use();}
            if(ev.rawType==EventType.MouseUp&&ev.button==dragButton){CancelDrag();ev.Use();}
        }
        Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.09f,.11f,.14f));
        GUI.BeginGroup(canvas);
        float spacing=64*zoom;
        float xStart=((offset.x%spacing)+spacing)%spacing,yStart=((offset.y%spacing)+spacing)%spacing;
        for(float x=xStart;x<canvas.width;x+=spacing)Fill(new Rect(x,0,1,canvas.height),new Color(.15f,.18f,.22f));
        for(float y=yStart;y<canvas.height;y+=spacing)Fill(new Rect(0,y,canvas.width,1),new Color(.15f,.18f,.22f));
        GUI.EndGroup();
        Fill(new Rect(0,0,Screen.width,56),new Color(.14f,.16f,.2f));
        var label=new GUIStyle(GUI.skin.label){font=font,fontSize=20,alignment=TextAnchor.MiddleLeft};
        GUI.Label(new Rect(16,8,Mathf.Max(1,Screen.width-200),40),"테크 트리 · 확대 "+Mathf.RoundToInt(zoom*100)+"%",label);
        var button=new GUIStyle(GUI.skin.button){font=font,fontSize=18};
        bool back=GUI.Button(new Rect(Mathf.Max(0,Screen.width-172),10,156,36),"← 대장간",button);
        var hint=new GUIStyle(label){fontSize=16};
        GUI.Label(new Rect(16,Screen.height-36,Mathf.Max(1,Screen.width-32),28),"좌클릭 / 우클릭 드래그: 이동 · 휠: 확대/축소",hint);
        if(back)CancelDrag();return back;
    }
}
