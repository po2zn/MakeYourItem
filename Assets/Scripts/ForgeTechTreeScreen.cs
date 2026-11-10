using System;
using System.Linq;
using UnityEngine;

public sealed class ForgeTechTreeScreen
{
    Vector2 offset=new Vector2(16,20),dragStart,offsetStart;
    Vector2 viewportSize;
    float zoom=1,cachedZoom=-1;
    int dragButton=-1,control,tab;
    bool focusRoot=true,cardsLaidOut;
    string selectedId=ForgeTechnology.AutomationRoot;
    GUIStyle label,button,body,header,quantityStyle,mapLabel,mapBody,mapButton,mapSectionLabel;
    readonly string[] quantities=new string[ForgeTechnology.Offers.Count];
    readonly Rect[] cards=new Rect[ForgeTechnology.Offers.Count];
    const float CardWidth=300,CardHeight=140,ColumnSpacing=340,RowSpacing=164;
    static Rect SectionRect(string nodeId)=>new Rect(24,24+ForgeTechnology.Find(nodeId).Lane*RowSpacing,CardWidth,CardHeight);
    static readonly Rect Root=SectionRect(ForgeTechnology.AutomationRoot);
    const string NavigationHint="연결된 선행 항목을 모두 구매해야 해금됩니다. · 빈 곳/우클릭 드래그: 이동 · 휠: 확대/축소";
    public void CancelDrag(){if(dragButton>=0&&GUIUtility.hotControl==control)GUIUtility.hotControl=0;dragButton=-1;}
    static void Fill(Rect rect,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    Rect Project(Rect r)=>new Rect(offset.x+r.x*zoom,offset.y+r.y*zoom,r.width*zoom,r.height*zoom);
    float TextHeight(string text,float width)=>Mathf.Ceil(body.CalcHeight(new GUIContent(text),Mathf.Max(1,width)))+4;
    void LayoutCards()
    {
        if(ForgeRenderDiagnostics.Optimized&&cardsLaidOut)return;
        cardsLaidOut=true;
        int material=0;
        for(int i=0;i<cards.Length;i++){
            var o=ForgeTechnology.Offers[i];
            cards[i]=o.Kind==ForgePurchaseKind.Material?new Rect(24+(material%3)*ColumnSpacing,24+(material++/3)*RowSpacing,CardWidth,CardHeight):new Rect(24+o.Tier*ColumnSpacing,24+o.Lane*RowSpacing,CardWidth,CardHeight);
        }
    }
    bool Visible(ForgeTechnology.Offer o)=>tab==1?o.Kind==ForgePurchaseKind.Material:o.Kind!=ForgePurchaseKind.Material;
    int Index(string id){for(int i=0;i<ForgeTechnology.Offers.Count;i++)if(ForgeTechnology.Offers[i].Id==id)return i;return -1;}
    bool OverCard(Vector2 point)
    {for(int i=0;i<cards.Length;i++)if(Visible(ForgeTechnology.Offers[i])&&cards[i].Contains(point))return true;return tab==0&&Root.Contains(point);}
    void ChangeTab(int next)
    {tab=next;CancelDrag();offset=new Vector2(16,20);zoom=1;focusRoot=tab==0;selectedId=tab==0?ForgeTechnology.AutomationRoot:"stock-1";}
    void Fit(Rect viewport)
    {
        float w=0,h=0;
        for(int i=0;i<cards.Length;i++)if(Visible(ForgeTechnology.Offers[i])){w=Mathf.Max(w,cards[i].xMax+24);h=Mathf.Max(h,cards[i].yMax+24);}
        zoom=Mathf.Clamp(Mathf.Min((viewport.width-24)/w,(viewport.height-24)/h),.25f,1);
        offset=new Vector2((viewport.width-w*zoom)/2,12);
    }
    GUIStyle Scaled(GUIStyle s)=>new GUIStyle(s){fontSize=Mathf.Max(7,Mathf.RoundToInt(s.fontSize*zoom)),padding=new RectOffset(4,4,4,4)};
    Color CardColor(ForgeTechnology.Offer o,ForgeTechnology technology)=>o.Id==selectedId?new Color(.24f,.38f,.48f):technology.Owns(o)?new Color(.12f,.3f,.23f):technology.MissingPrerequisites(o)!=null?new Color(.16f,.18f,.22f):new Color(.22f,.27f,.34f);
    void DrawEdge(Rect from,Rect to,Color color)
    {
        if(Event.current.type!=EventType.Repaint||ForgeRenderDiagnostics.Hide(ForgeRenderLayer.TreeEdges))return;
        var a=Project(from);var b=Project(to);
        Rect bounds=Rect.MinMaxRect(Mathf.Min(a.xMax,b.x)-4,Mathf.Min(a.center.y,b.center.y)-4,Mathf.Max(a.xMax,b.x)+6,Mathf.Max(a.center.y,b.center.y)+8);
        if(ForgeRenderDiagnostics.Optimized&&!bounds.Overlaps(new Rect(Vector2.zero,viewportSize)))return;
        float mid=(a.xMax+b.x)/2;
        Fill(new Rect(a.xMax,a.center.y,Mathf.Max(1,mid-a.xMax),2),color);
        Fill(new Rect(mid,Mathf.Min(a.center.y,b.center.y),2,Mathf.Abs(a.center.y-b.center.y)+2),color);
        Fill(new Rect(mid,b.center.y,Mathf.Max(1,b.x-mid),2),color);
        Fill(new Rect(b.x-4,b.center.y-3,6,8),color);
    }
    public bool Draw(Font font,ForgeTechnology technology,int gold,int iron,ForgeMaterialStock[] stocks,bool busy,string message,Action<ForgeTechnology.Offer,int> purchase,bool purchaseSucceeded=false,string goal="",Action openSmiths=null)
    {
        if(label==null){
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=24,wordWrap=true,padding=new RectOffset(4,4,4,4)};
            label.normal.textColor=Color.white;label.hover=label.normal;
            header=new GUIStyle(label){fontSize=38};
            body=new GUIStyle(label){fontSize=22};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=24,wordWrap=true};
            quantityStyle=new GUIStyle(GUI.skin.textField){font=font,fontSize=24,padding=new RectOffset(8,8,4,4)};
        }
        var ev=Event.current;LayoutCards();
        var selected=ForgeTechnology.Find(selectedId);
        if(selected==null||!Visible(selected)){selected=ForgeTechnology.Offers.First(Visible);selectedId=selected.Id;}
        int selectedIndex=Index(selected.Id);
        bool material=selected.Kind==ForgePurchaseKind.Material;
        int quantity=1;
        if(material&&!int.TryParse(quantities[selectedIndex]??"1",System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out quantity))quantity=0;
        float width=Mathf.Max(100,Screen.width-32);
        string info=selected.Description+"\n"+technology.PrerequisiteText(selected);
        float detailHeight=TextHeight(selected.Title+" · "+(material?"개당 ":"")+selected.Price+"G",width)+TextHeight(info,width)+TextHeight(message,width)+102;
        detailHeight=Mathf.Min(detailHeight,Mathf.Max(140,Screen.height*.48f));
        string goalText=goal.Length>0?goal:NavigationHint;
        float titleHeight=Mathf.Max(44,header.CalcHeight(new GUIContent("테크 트리 · "+gold+"G"),Mathf.Max(1,Screen.width-180)));
        float tabsY=titleHeight+8,goalY=tabsY+48;
        float headerHeight=goalY+TextHeight(goalText,width)+8;
        Rect canvas=new Rect(0,headerHeight,Screen.width,Mathf.Max(40,Screen.height-headerHeight-detailHeight));
        viewportSize=canvas.size;
        if(focusRoot){offset=new Vector2(16,Mathf.Max(12,(canvas.height-CardHeight)/2)-Root.y);focusRoot=false;}
        control=GUIUtility.GetControlID("TechTreePan".GetHashCode(),FocusType.Passive);
        // Lost capture must not leave dragButton blocking all subsequent wheel input.
        if(dragButton>=0&&GUIUtility.hotControl!=control)dragButton=-1;
        if(ev.type==EventType.ScrollWheel&&canvas.Contains(ev.mousePosition)&&dragButton<0){
            Vector2 anchor=(ev.mousePosition-canvas.position-offset)/zoom;
            zoom=Mathf.Clamp(zoom*Mathf.Pow(1.08f,-ev.delta.y),.25f,1.8f);offset=ev.mousePosition-canvas.position-anchor*zoom;ev.Use();
        }
        bool overCard=OverCard((ev.mousePosition-canvas.position-offset)/zoom);
        if(ev.type==EventType.MouseDown&&canvas.Contains(ev.mousePosition)&&(ev.button==1||ev.button==0&&!overCard)&&GUIUtility.hotControl==0){
            dragButton=ev.button;dragStart=ev.mousePosition;offsetStart=offset;GUIUtility.hotControl=control;GUI.FocusControl(null);ev.Use();
        }
        if(dragButton>=0&&GUIUtility.hotControl==control){
            if(ev.type==EventType.MouseDrag&&ev.button==dragButton){offset=offsetStart+ev.mousePosition-dragStart;ev.Use();}
            if(ev.rawType==EventType.MouseUp&&ev.button==dragButton){CancelDrag();ev.Use();}
        }
        if(!Mathf.Approximately(cachedZoom,zoom)){cachedZoom=zoom;mapLabel=Scaled(label);mapBody=Scaled(body);mapButton=Scaled(button);mapSectionLabel=new GUIStyle(mapLabel){alignment=TextAnchor.MiddleLeft};}
        Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.07f,.10f,.14f));
        GUI.BeginGroup(canvas);
        if(tab==0){
            GUI.Label(Project(SectionRect("function-Heat")),"금속 가공",mapSectionLabel);
            GUI.Label(Project(SectionRect("function-Lucky")),"행운",mapSectionLabel);
            GUI.Label(Project(SectionRect("function-Inspect")),"마력 가공",mapSectionLabel);
            GUI.Label(Project(SectionRect("function-Fuel")),"공방 운영",mapSectionLabel);
            GUI.Label(Project(SectionRect("function-IsDay")),"시간",mapSectionLabel);
            GUI.Label(Project(SectionRect("function-Abs")),"기술 연구",mapSectionLabel);
            for(int i=0;i<cards.Length;i++){
                var o=ForgeTechnology.Offers[i];if(!Visible(o))continue;
                if(o.Parents.Count==0)DrawEdge(Root,cards[i],new Color(.3f,.4f,.5f));
                foreach(var parent in o.Parents){int pi=Index(parent);if(pi>=0)DrawEdge(cards[pi],cards[i],technology.Owns(parent)?new Color(.25f,.75f,.5f):new Color(.3f,.35f,.42f));}
            }
            Fill(Project(Root),new Color(.18f,.3f,.36f));
            GUI.Label(Project(new Rect(Root.x+8,Root.y+6,Root.width-16,Root.height-12)),"기본 공방 · 제공\n철 / 나무 / 물\n1~5 공정",mapLabel);
        }
        if(ForgeRenderDiagnostics.Optimized&&!ForgeRenderDiagnostics.SkipPaint(ForgeRenderLayer.TreeCards)){
            for(int i=0;i<cards.Length;i++){
                var o=ForgeTechnology.Offers[i];if(!Visible(o)||!Project(cards[i]).Overlaps(new Rect(Vector2.zero,viewportSize)))continue;
                Fill(Project(cards[i]),CardColor(o,technology));
            }
        }
        for(int i=0;i<cards.Length;i++){
            var o=ForgeTechnology.Offers[i];if(!Visible(o))continue;
            if(ForgeRenderDiagnostics.SkipPaint(ForgeRenderLayer.TreeCards))continue;
            if(ForgeRenderDiagnostics.Optimized&&Event.current.type==EventType.Repaint&&!Project(cards[i]).Overlaps(new Rect(Vector2.zero,viewportSize)))continue;
            bool owned=technology.Owns(o),locked=technology.MissingPrerequisites(o)!=null;
            Rect r=cards[i];
            if(!ForgeRenderDiagnostics.Optimized)Fill(Project(r),CardColor(o,technology));
            string state=o.Kind==ForgePurchaseKind.Material?"재고 "+technology.Stock(o,iron,stocks)+" · "+o.Price+"G/개":owned?"구매 완료":locked?"선행 미충족 · "+o.Price+"G":gold<o.Price?"골드 부족 · "+o.Price+"G":"구매 가능 · "+o.Price+"G";
            if(GUI.Button(Project(new Rect(r.x+6,r.y+5,r.width-12,72)),o.Title,mapButton)){selectedId=o.Id;GUI.FocusControl(null);}
            GUI.Label(Project(new Rect(r.x+6,r.y+81,r.width-12,53)),state,mapBody);
        }
        GUI.EndGroup();
        Fill(new Rect(0,0,Screen.width,headerHeight),new Color(.12f,.16f,.22f));
        GUI.Label(new Rect(12,4,Mathf.Max(1,Screen.width-180),titleHeight),"테크 트리 · "+gold+"G",header);
        bool back=GUI.Button(new Rect(Screen.width-164,10,150,44),"← 대장간",button);
        float tabUnit=Mathf.Min(160,Mathf.Max(50,(Screen.width-64)/5));
        if(GUI.Button(new Rect(16,tabsY,tabUnit,44),tab==0?"● 성장 트리":"성장 트리",button))ChangeTab(0);
        if(GUI.Button(new Rect(24+tabUnit,tabsY,tabUnit,44),tab==1?"● 재료 상점":"재료 상점",button))ChangeTab(1);
        if(GUI.Button(new Rect(32+tabUnit*2,tabsY,tabUnit,44),"전체 보기",button))Fit(canvas);
        if(GUI.Button(new Rect(40+tabUnit*3,tabsY,tabUnit,44),"100%",button)){zoom=1;offset=new Vector2(16,20);}
        if(openSmiths!=null&&GUI.Button(new Rect(48+tabUnit*4,tabsY,tabUnit,44),"대장장이",button))openSmiths();
        GUI.Label(new Rect(16,goalY,width,headerHeight-goalY),goalText,body);
        // A scrollable inspector keeps long descriptions and prerequisites readable at low resolutions.
        DrawInspector(new Rect(0,canvas.yMax,Screen.width,Mathf.Max(1,Screen.height-canvas.yMax)),selected,selectedIndex,technology,gold,iron,stocks,busy,message,purchaseSucceeded,purchase);
        if(back)CancelDrag();return back;
    }
    Vector2 inspectorScroll;
    void DrawInspector(Rect rect,ForgeTechnology.Offer offer,int index,ForgeTechnology technology,int gold,int iron,ForgeMaterialStock[] stocks,bool busy,string message,bool success,Action<ForgeTechnology.Offer,int> purchase)
    {
        // Slightly lighter blue-gray keeps the inspector within the tree palette.
        Fill(rect,new Color(.17f,.21f,.27f));
        Fill(new Rect(rect.x,rect.y,rect.width,2),new Color(.30f,.36f,.43f));
        GUILayout.BeginArea(new Rect(rect.x+16,rect.y+6,Mathf.Max(1,rect.width-32),Mathf.Max(1,rect.height-12)));
        inspectorScroll=GUILayout.BeginScrollView(inspectorScroll);
        bool material=offer.Kind==ForgePurchaseKind.Material;
        GUILayout.Label(offer.Title+" · "+(material?"개당 ":"")+offer.Price+"G",label);
        GUILayout.Label(offer.Description+"\n"+technology.PrerequisiteText(offer),body);
        int quantity=1;
        GUILayout.BeginHorizontal();
        if(material){
            GUILayout.Label("수량",body,GUILayout.Width(64));
            quantities[index]=GUILayout.TextField(quantities[index]??"1",10,quantityStyle,GUILayout.Width(130),GUILayout.Height(44));
            if(!int.TryParse(quantities[index],System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out quantity)||quantity<1)quantity=0;
            GUILayout.Label(quantity>0?"예상 "+ForgeTechnology.TotalPrice(offer,quantity).ToString("N0")+"G":"1 이상 입력",body);
        }
        string reason=technology.BlockReason(offer,gold,iron,stocks,busy,quantity);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&reason==null;
        string caption=technology.Owns(offer)?"구매 완료":material?quantity+"개 구매":offer.Kind==ForgePurchaseKind.Commission?"최종 의뢰 수락":"해금 구매";
        bool buy=GUILayout.Button(caption,button,GUILayout.MinWidth(170),GUILayout.Height(46));GUI.enabled=enabled;
        GUILayout.EndHorizontal();
        if(reason!=null&&!technology.Owns(offer))GUILayout.Label(reason,body);
        if(technology.MissingPrerequisites(offer)!=null&&GUILayout.Button("필요한 선행 노드 보기",button,GUILayout.Height(44))){
            string parent=offer.Parents.First(id=>!technology.Owns(id));
            ChangeTab(0);selectedId=parent;zoom=1;
            focusRoot=false;
            offset=viewportSize/2-cards[Index(parent)].center;
        }
        Color old=GUI.color;if(success)GUI.color=new Color(.5f,1f,.7f);GUILayout.Label(message,body);GUI.color=old;
        GUILayout.Label(NavigationHint,body);
        GUILayout.EndScrollView();GUILayout.EndArea();
        if(buy)purchase(offer,quantity);
    }
}
