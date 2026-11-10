using System;
using UnityEngine;

public sealed class ForgeTechTreeScreen
{
    Vector2 offset,dragStart,offsetStart;
    float zoom=1;
    int dragButton=-1,control;
    GUIStyle label,button,body,header;
    const float CardWidth=350,TextWidth=CardWidth-24;
    readonly Rect[] cards=new Rect[ForgeTechnology.Offers.Count];
    readonly float[] titleHeights=new float[ForgeTechnology.Offers.Count],detailHeights=new float[ForgeTechnology.Offers.Count];
    readonly string[] details=new string[ForgeTechnology.Offers.Count];
    const string NavigationHint="빈 곳 좌클릭 / 우클릭 드래그: 이동 · 휠: 확대/축소";
    public void CancelDrag(){if(dragButton>=0&&GUIUtility.hotControl==control)GUIUtility.hotControl=0;dragButton=-1;}
    static void Fill(Rect rect,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    float TextHeight(string text,float width) => Mathf.Ceil(body.CalcHeight(new GUIContent(text),width))+4;
    void LayoutCards(ForgeTechnology technology,int iron,ForgeMaterialStock[] stocks,float startY)
    {
        float materialY=startY,functionY=startY;
        for(int i=0;i<cards.Length;i++){
            var offer=ForgeTechnology.Offers[i];
            bool material=offer.Kind==ForgePurchaseKind.Material;
            bool owned=!material&&technology.Owns(offer.Feature);
            details[i]=material?"보유 "+technology.Stock(offer,iron,stocks)+"개 · "+(offer.Material==1?"검날 제작 재료":"전용 가공은 도면 안내서를 확인하세요"):(owned?"해금 완료 · 숫자/영문 이름 모두 사용 가능":"미구매 · 구매 후 도면에서 사용 가능");
            titleHeights[i]=TextHeight(offer.Title,TextWidth);
            detailHeights[i]=TextHeight(details[i],TextWidth);
            float height=12+titleHeights[i]+8+detailHeights[i]+12+36+12;
            cards[i]=new Rect(material?24:406,material?materialY:functionY,CardWidth,height);
            if(material)materialY+=height+16;else functionY+=height+16;
        }
    }
    Rect Project(Rect rect) => new Rect(offset.x+rect.x*zoom,offset.y+rect.y*zoom,rect.width*zoom,rect.height*zoom);
    GUIStyle ScaledStyle(GUIStyle source)
    {
        return new GUIStyle(source){fontSize=Mathf.Max(1,Mathf.FloorToInt(source.fontSize*zoom)),
            padding=new RectOffset(Mathf.RoundToInt(source.padding.left*zoom),Mathf.RoundToInt(source.padding.right*zoom),Mathf.RoundToInt(source.padding.top*zoom),Mathf.RoundToInt(source.padding.bottom*zoom))};
    }
    bool OverCard(Vector2 point)
    {
        foreach(var card in cards)if(card.Contains(point))return true;
        return false;
    }
    public bool Draw(Font font,ForgeTechnology technology,int gold,int iron,ForgeMaterialStock[] stocks,bool busy,string message,Action<ForgeTechnology.Offer> purchase)
    {
        if(label==null){
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=20,alignment=TextAnchor.UpperLeft,wordWrap=true,padding=new RectOffset(0,0,0,0)};
            label.normal.textColor=Color.white;
            header=new GUIStyle(label){fontSize=30,alignment=TextAnchor.MiddleLeft};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};
            body=new GUIStyle(label){fontSize=15,wordWrap=true};
        }
        var ev=Event.current;
        control=GUIUtility.GetControlID("TechTreePan".GetHashCode(),FocusType.Passive);
        float footerWidth=Mathf.Max(1,Screen.width-32);
        float messageHeight=TextHeight(message,footerWidth),hintHeight=TextHeight(NavigationHint,footerWidth);
        float footerHeight=messageHeight+hintHeight+16;
        float headingHeight=Mathf.Ceil(Mathf.Max(label.CalcHeight(new GUIContent("재료 구매 · 1개씩 반복 구매"),350),label.CalcHeight(new GUIContent("기능 구매 · 한 번 구매해 해금"),350)))+4;
        float descriptionY=12+headingHeight+8;
        float descriptionHeight=Mathf.Max(TextHeight("구매한 재료는 보유 재고에 추가됩니다.",350),TextHeight("기본 공정과 자료구조 조작은 무료이며, 특수 공정은 구매해 해금합니다.",350));
        LayoutCards(technology,iron,stocks,descriptionY+descriptionHeight+16);
        string heading="테크 트리 · "+gold+"G";
        float headerWidth=Mathf.Max(1,Screen.width-204);
        float headerHeight=Mathf.Max(64,Mathf.Ceil(header.CalcHeight(new GUIContent(heading),headerWidth))+24);
        Rect canvas=new Rect(0,headerHeight,Screen.width,Mathf.Max(1,Screen.height-headerHeight-footerHeight));
        if(ev.type==EventType.ScrollWheel&&canvas.Contains(ev.mousePosition)&&dragButton<0){
            Vector2 anchor=(ev.mousePosition-canvas.position-offset)/zoom;
            zoom=Mathf.Clamp(zoom*Mathf.Pow(1.05f,-ev.delta.y),.4f,2f);
            offset=ev.mousePosition-canvas.position-anchor*zoom;
            ev.Use();
        }
        bool overCard=OverCard((ev.mousePosition-canvas.position-offset)/zoom);
        if(ev.type==EventType.MouseDown&&canvas.Contains(ev.mousePosition)&&(ev.button==1||ev.button==0&&!overCard)&&GUIUtility.hotControl==0){
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
        // Project into the fixed viewport before IMGUI clips controls. Changing
        // GUI.matrix inside BeginGroup also transforms its clipping space.
        var canvasLabel=ScaledStyle(label);
        var canvasBody=ScaledStyle(body);
        var canvasButton=ScaledStyle(button);
        GUI.Label(Project(new Rect(24,12,350,headingHeight)),"재료 구매 · 1개씩 반복 구매",canvasLabel);
        GUI.Label(Project(new Rect(406,12,350,headingHeight)),"기능 구매 · 한 번 구매해 해금",canvasLabel);
        GUI.Label(Project(new Rect(24,descriptionY,350,descriptionHeight)),"구매한 재료는 보유 재고에 추가됩니다.",canvasBody);
        GUI.Label(Project(new Rect(406,descriptionY,350,descriptionHeight)),"기본 공정과 자료구조 조작은 무료이며, 특수 공정은 구매해 해금합니다.",canvasBody);
        ForgeTechnology.Offer selected=null;
        for(int i=0;i<cards.Length;i++){
            var offer=ForgeTechnology.Offers[i];
            Rect card=cards[i];
            bool owned=offer.Kind==ForgePurchaseKind.Function&&technology.Owns(offer.Feature);
            Fill(Project(card),owned?new Color(.12f,.26f,.23f):new Color(.18f,.21f,.27f));
            GUI.Label(Project(new Rect(card.x+12,card.y+12,TextWidth,titleHeights[i])),offer.Title,canvasBody);
            GUI.Label(Project(new Rect(card.x+12,card.y+12+titleHeights[i]+8,TextWidth,detailHeights[i])),details[i],canvasBody);
            string reason=technology.BlockReason(offer,gold,iron,stocks,busy);
            bool enabled=GUI.enabled;
            GUI.enabled=enabled&&reason==null;
            string caption=owned?"구매 완료":busy?"제작 중":gold<offer.Price?offer.Price+"G · 골드 부족":offer.Price+"G · "+(offer.Kind==ForgePurchaseKind.Material?"1개 구매":"기능 구매");
            if(GUI.Button(Project(new Rect(card.x+12,card.yMax-48,TextWidth,36)),new GUIContent(caption,reason??""),canvasButton))selected=offer;
            GUI.enabled=enabled;
        }
        GUI.EndGroup();
        Fill(new Rect(0,0,Screen.width,headerHeight),new Color(.14f,.16f,.2f));
        GUI.Label(new Rect(16,12,headerWidth,headerHeight-24),heading,header);
        bool back=GUI.Button(new Rect(Mathf.Max(0,Screen.width-172),(headerHeight-36)/2,156,36),"← 대장간",button);
        GUI.Label(new Rect(16,Screen.height-footerHeight+8,footerWidth,messageHeight),message,body);
        GUI.Label(new Rect(16,Screen.height-footerHeight+8+messageHeight,footerWidth,hintHeight),NavigationHint,body);
        if(selected!=null)purchase(selected);
        if(back)CancelDrag();return back;
    }
}
