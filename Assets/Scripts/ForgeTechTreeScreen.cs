using System;
using UnityEngine;

public sealed class ForgeTechTreeScreen
{
    Vector2 offset,dragStart,offsetStart;
    float zoom=1;
    int dragButton=-1,control;
    GUIStyle label,button,body;
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
            details[i]=material?"보유 "+technology.Stock(offer,iron,stocks)+"개 · "+(offer.Material==1?"검날 제작 재료":"작업대 배치용 · 전용 제작법 미지원"):(owned?"해금 완료 · 숫자/영문 이름 모두 사용 가능":"미구매 · 구매 후 도면에서 사용 가능");
            titleHeights[i]=TextHeight(offer.Title,TextWidth);
            detailHeights[i]=TextHeight(details[i],TextWidth);
            float height=12+titleHeights[i]+8+detailHeights[i]+12+36+12;
            cards[i]=new Rect(material?24:406,material?materialY:functionY,CardWidth,height);
            if(material)materialY+=height+16;else functionY+=height+16;
        }
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
        float descriptionHeight=Mathf.Max(TextHeight("구매한 재료는 보유 재고에 추가됩니다.",350),TextHeight("기본 공정 1~4와 print는 무료입니다.",350));
        LayoutCards(technology,iron,stocks,descriptionY+descriptionHeight+16);
        Rect canvas=new Rect(0,56,Screen.width,Mathf.Max(1,Screen.height-56-footerHeight));
        if(ev.type==EventType.ScrollWheel&&canvas.Contains(ev.mousePosition)&&dragButton<0){
            Vector2 anchor=(ev.mousePosition-canvas.position-offset)/zoom;
            zoom=Mathf.Clamp(zoom*Mathf.Pow(1.05f,-ev.delta.y),.4f,2f);
            offset=ev.mousePosition-canvas.position-anchor*zoom;ev.Use();
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
        Matrix4x4 previous=GUI.matrix;
        GUI.matrix=previous*Matrix4x4.TRS(new Vector3(offset.x,offset.y,0),Quaternion.identity,new Vector3(zoom,zoom,1));
        GUI.Label(new Rect(24,12,350,headingHeight),"재료 구매 · 1개씩 반복 구매",label);
        GUI.Label(new Rect(406,12,350,headingHeight),"기능 구매 · 한 번 구매해 해금",label);
        GUI.Label(new Rect(24,descriptionY,350,descriptionHeight),"구매한 재료는 보유 재고에 추가됩니다.",body);
        GUI.Label(new Rect(406,descriptionY,350,descriptionHeight),"기본 공정 1~4와 print는 무료입니다.",body);
        ForgeTechnology.Offer selected=null;
        for(int i=0;i<cards.Length;i++){
            var offer=ForgeTechnology.Offers[i];
            Rect card=cards[i];
            bool owned=offer.Kind==ForgePurchaseKind.Function&&technology.Owns(offer.Feature);
            Fill(card,owned?new Color(.12f,.26f,.23f):new Color(.18f,.21f,.27f));
            GUI.Label(new Rect(card.x+12,card.y+12,TextWidth,titleHeights[i]),offer.Title,body);
            GUI.Label(new Rect(card.x+12,card.y+12+titleHeights[i]+8,TextWidth,detailHeights[i]),details[i],body);
            string reason=technology.BlockReason(offer,gold,iron,stocks,busy);
            bool enabled=GUI.enabled;
            GUI.enabled=enabled&&reason==null;
            string caption=owned?"구매 완료":busy?"제작 중":gold<offer.Price?offer.Price+"G · 골드 부족":offer.Price+"G · "+(offer.Kind==ForgePurchaseKind.Material?"1개 구매":"기능 구매");
            if(GUI.Button(new Rect(card.x+12,card.yMax-48,TextWidth,36),new GUIContent(caption,reason??""),button))selected=offer;
            GUI.enabled=enabled;
        }
        GUI.matrix=previous;
        GUI.EndGroup();
        Fill(new Rect(0,0,Screen.width,56),new Color(.14f,.16f,.2f));
        GUI.Label(new Rect(16,8,Mathf.Max(1,Screen.width-200),40),"테크 트리 · "+gold+"G",label);
        bool back=GUI.Button(new Rect(Mathf.Max(0,Screen.width-172),10,156,36),"← 대장간",button);
        GUI.Label(new Rect(16,Screen.height-footerHeight+8,footerWidth,messageHeight),message,body);
        GUI.Label(new Rect(16,Screen.height-footerHeight+8+messageHeight,footerWidth,hintHeight),NavigationHint,body);
        if(selected!=null)purchase(selected);
        if(back)CancelDrag();return back;
    }
}
