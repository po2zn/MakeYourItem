using System.Collections.Generic;
using UnityEngine;

// Per-book browsing state survives moving, resizing and closing the reference window.
public sealed class ForgeGuideReader
{
    string query="";
    int selected;
    bool showContents,copied;
    bool englishUnlocked;
    Vector2 indexScroll,detailScroll,codeScroll,leftTabScroll,rightTabScroll,materialScroll;
    int material=-1;
    string section="";
    static readonly string[] Sections={"게임 소개","시작하기","대장간 공정","조건과 반복","사용자 함수","조회와 계산","자료 묶음","기본 문법","완성 예제","문제가 생겼을 때"};
    static readonly string[] Tabs={"소개","시작","공정","조건","함수","계산","자료","기본","예제","오류"};
    GUIStyle bodyStyle,titleStyle,sectionStyle,entryStyle,codeStyle,buttonStyle,searchStyle,tabStyle;
    void Styles(Font font,int fontSize)
    {
        if(bodyStyle!=null&&bodyStyle.font==font&&bodyStyle.fontSize==fontSize)return;
        bodyStyle=new GUIStyle(GUI.skin.label){font=font,fontSize=fontSize,wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};
        bodyStyle.normal.textColor=new Color(.18f,.15f,.12f);
        titleStyle=new GUIStyle(bodyStyle){fontSize=fontSize+4,fontStyle=FontStyle.Bold,alignment=TextAnchor.UpperLeft,fixedHeight=0,contentOffset=Vector2.zero};
        sectionStyle=new GUIStyle(bodyStyle){fontSize=fontSize,fontStyle=FontStyle.Bold};
        sectionStyle.normal.textColor=new Color(.42f,.33f,.23f);
        entryStyle=new GUIStyle(GUI.skin.button){font=font,fontSize=fontSize+2,fontStyle=FontStyle.Bold,wordWrap=true,richText=false,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(6,6,4,4)};
        buttonStyle=new GUIStyle(GUI.skin.button){font=font,fontSize=fontSize,richText=false};
        searchStyle=new GUIStyle(GUI.skin.textField){font=font,fontSize=fontSize,richText=false};
        codeStyle=new GUIStyle(bodyStyle){fontSize=fontSize,wordWrap=false,padding=new RectOffset(6,6,6,6)};
        codeStyle.normal.textColor=new Color(.94f,.94f,.94f);
        tabStyle=new GUIStyle(buttonStyle){fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(0,0,0,0)};
        foreach(var state in new[]{tabStyle.normal,tabStyle.hover,tabStyle.active,tabStyle.focused,tabStyle.onNormal,tabStyle.onHover,tabStyle.onActive,tabStyle.onFocused}){state.background=null;state.scaledBackgrounds=new Texture2D[0];state.textColor=new Color(.98f,.94f,.83f);}
        foreach(var style in new[]{bodyStyle,titleStyle,sectionStyle,entryStyle,codeStyle,buttonStyle,searchStyle,tabStyle})FixTextColor(style);
    }
    static void FixTextColor(GUIStyle style)
    {
        Color color=style.normal.textColor;
        style.hover.textColor=color;style.active.textColor=color;style.focused.textColor=color;
        style.onNormal.textColor=color;style.onHover.textColor=color;
        style.onActive.textColor=color;style.onFocused.textColor=color;
    }
    void Select(int index)
    {material=-1;selected=index;detailScroll=Vector2.zero;codeScroll=Vector2.zero;showContents=false;copied=false;}
    public void Draw(Rect area,Font font,int fontSize,bool englishGuideUnlocked)
    {
        if(englishUnlocked!=englishGuideUnlocked){englishUnlocked=englishGuideUnlocked;copied=false;detailScroll=Vector2.zero;codeScroll=Vector2.zero;}
        Styles(font,fontSize);
        Color old=GUI.color;GUI.color=new Color(.96f,.92f,.82f);GUI.DrawTexture(area,Texture2D.whiteTexture);GUI.color=old;
        // Ribbons stay inside the book hit region, clear of its resize handles.
        DrawBookmarks(new Rect(area.x,area.y,62,area.height),false);
        DrawBookmarks(new Rect(area.xMax-44,area.y,44,area.height),true);
        Rect page=new Rect(area.x+68,area.y+4,Mathf.Max(1,area.width-118),Mathf.Max(1,area.height-8));
        if(material>=0){DrawMaterial(page);return;}
        if(section=="게임 소개"){DrawIntroduction(page);return;}
        if(GUI.Button(new Rect(page.x,page.y,58,28),showContents?"본문":"목록",buttonStyle))showContents=!showContents;
        string next=GUI.TextField(new Rect(page.x+62,page.y,Mathf.Max(1,page.width-62),28),query,80,searchStyle);
        if(next!=query){query=next;section="";indexScroll=Vector2.zero;showContents=true;}
        var matches=new List<int>();
        for(int i=0;i<BlueprintGuide.Entries.Count;i++)if((section.Length==0||BlueprintGuide.Entries[i].Section==section)&&BlueprintGuide.Entries[i].Matches(query,englishUnlocked))matches.Add(i);
        if(matches.Count>0&&!matches.Contains(selected)){selected=matches[0];detailScroll=Vector2.zero;codeScroll=Vector2.zero;copied=false;}
        Rect view=new Rect(page.x,page.y+34,page.width,Mathf.Max(1,page.height-68));
        if(showContents)DrawIndex(view,matches);else DrawDetail(view,matches.Count>0);
        float bottom=page.yMax-28;int position=matches.IndexOf(selected);bool wasEnabled=GUI.enabled;
        GUI.enabled=wasEnabled&&position>0;
        if(GUI.Button(new Rect(page.x,bottom,32,28),"<",buttonStyle))Select(matches[position-1]);
        GUI.enabled=wasEnabled&&position>=0&&position<matches.Count-1;
        if(GUI.Button(new Rect(page.xMax-32,bottom,32,28),">",buttonStyle))Select(matches[position+1]);
        GUI.enabled=wasEnabled;
        var pageStyle=new GUIStyle(sectionStyle){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Normal};
        GUI.Label(new Rect(page.x+34,bottom,Mathf.Max(1,page.width-68),28),matches.Count==0?"0 / 0":(position+1)+" / "+matches.Count,pageStyle);
    }
    void DrawBookmarks(Rect rail,bool materials)
    {
        int count=materials?ForgeMaterialGuide.Entries.Count:Sections.Length;
        float width=rail.width-4;
        Vector2 scroll=materials?rightTabScroll:leftTabScroll;
        scroll=BeginScroll(rail,scroll,new Rect(0,0,width,count*38));
        for(int i=0;i<count;i++){
            bool active=materials?material==i:material<0&&section==Sections[i];
            Rect tab=new Rect(0,i*38,active?width:width-4,34);
            Color old=GUI.color;GUI.color=active?new Color(.73f,.39f,.20f):materials?new Color(.33f,.46f,.48f):new Color(.48f,.37f,.28f);
            GUI.DrawTexture(tab,Texture2D.whiteTexture);GUI.color=old;
            // An inset stitch line and projecting end make the navigation read as a bookmark.
            GUI.color=new Color(.92f,.79f,.53f);GUI.DrawTexture(new Rect(materials?tab.xMax-3:tab.x+2,tab.y+3,1,tab.height-6),Texture2D.whiteTexture);GUI.color=old;
            if(GUI.Button(tab,new GUIContent(materials?i.ToString():Tabs[i],materials?ForgeMaterialGuide.Entries[i].Name:Sections[i]),tabStyle)){
                GUI.FocusControl(null);
                if(materials){material=i;materialScroll=Vector2.zero;}
                else{material=-1;section=Sections[i];query="";showContents=true;indexScroll=Vector2.zero;detailScroll=Vector2.zero;codeScroll=Vector2.zero;}
            }
        }
        GUI.EndScrollView();if(materials)rightTabScroll=scroll;else leftTabScroll=scroll;
    }
    void DrawIntroduction(Rect page)
    {
        float width=Mathf.Max(1,page.width-4);
        float titleH=TitleHeight(BlueprintGuide.IntroductionTitle,width);
        float bodyH=Height(BlueprintGuide.Introduction,bodyStyle,width);
        detailScroll=BeginScroll(page,detailScroll,new Rect(0,0,width,Mathf.Max(page.height,titleH+bodyH+24)));
        GUI.Label(new Rect(0,0,width,titleH),BlueprintGuide.IntroductionTitle,titleStyle);
        GUI.Label(new Rect(0,titleH+12,width,bodyH),BlueprintGuide.Introduction,bodyStyle);
        GUI.EndScrollView();
    }
    void DrawMaterial(Rect page)
    {
        var entry=ForgeMaterialGuide.Entries[material];float width=Mathf.Max(1,page.width-4);
        float titleH=TitleHeight(entry.Heading,width),statusH=Height(entry.Status,sectionStyle,width),bodyH=Height(entry.Description,bodyStyle,width);
        materialScroll=BeginScroll(page,materialScroll,new Rect(0,0,width,Mathf.Max(page.height,titleH+statusH+bodyH+36)));
        GUI.Label(new Rect(0,0,width,titleH),entry.Heading,titleStyle);
        GUI.Label(new Rect(0,titleH+8,width,statusH),entry.Status,sectionStyle);
        GUI.Label(new Rect(0,titleH+statusH+20,width,bodyH),entry.Description,bodyStyle);
        GUI.EndScrollView();
    }
    // Hide scrollbar graphics and their layout gutters; native wheel scrolling remains available.
    static Vector2 BeginScroll(Rect viewport,Vector2 scroll,Rect content,bool horizontalCode=false)
    {
        var ev=Event.current;
        if(horizontalCode&&ev.type==EventType.ScrollWheel&&viewport.Contains(ev.mousePosition)
            &&(ev.shift||Mathf.Abs(ev.delta.x)>0)&&content.width>viewport.width){
            float delta=ev.shift?ev.delta.y:ev.delta.x;
            scroll.x=Mathf.Clamp(scroll.x+delta*20f,0,content.width-viewport.width);
            ev.Use();
        }
        return GUI.BeginScrollView(viewport,scroll,content,false,false,GUIStyle.none,GUIStyle.none);
    }
    void DrawIndex(Rect view,List<int> matches)
    {
        float width=Mathf.Max(1,view.width-4),height=0;
        foreach(int index in matches)height+=Mathf.Max(44,entryStyle.CalcHeight(new GUIContent(BlueprintGuide.Entries[index].Section+"\n"+BlueprintGuide.Entries[index].HeadingFor(englishUnlocked)),width))+4;
        indexScroll=BeginScroll(view,indexScroll,new Rect(0,0,width,Mathf.Max(view.height,height)));
        float y=0;
        foreach(int index in matches){
            var entry=BlueprintGuide.Entries[index];string label=entry.Section+"\n"+entry.HeadingFor(englishUnlocked);
            float h=Mathf.Max(44,entryStyle.CalcHeight(new GUIContent(label),width));
            Color old=GUI.backgroundColor;if(index==selected)GUI.backgroundColor=new Color(.70f,.81f,.86f);
            if(GUI.Button(new Rect(0,y,width,h),label,entryStyle))Select(index);
            GUI.backgroundColor=old;y+=h+4;
        }
        if(matches.Count==0)GUI.Label(new Rect(0,0,width,48),"함수 이름이나 숫자로 찾아보세요.",bodyStyle);
        GUI.EndScrollView();
    }
    float Height(string text,GUIStyle style,float width){return style.CalcHeight(new GUIContent(text),Mathf.Max(1,width));}
    float TitleHeight(string text,float width)
    {
        // Mixed Latin/Korean bold glyphs can extend below CalcHeight's line box.
        // Grow the label itself, not only the gap after it (which remains clipped).
        float measured=Height(text,titleStyle,width);
        float glyphMargin=Mathf.Ceil(titleStyle.fontSize*.5f);
        return Mathf.Ceil(Mathf.Max(measured,titleStyle.lineHeight)+glyphMargin);
    }
    float SubsectionHeight(string heading,string body,float width)
    {return Mathf.Ceil(Height(heading,sectionStyle,width)+sectionStyle.fontSize*.5f)+4+Height(body,bodyStyle,width);}
    void DrawSubsection(float y,float width,string heading,string body)
    {
        float headingH=Mathf.Ceil(Height(heading,sectionStyle,width)+sectionStyle.fontSize*.5f);
        GUI.Label(new Rect(0,y,width,headingH),heading,sectionStyle);
        GUI.Label(new Rect(0,y+headingH+4,width,Height(body,bodyStyle,width)),body,bodyStyle);
    }
    void DrawDetail(Rect view,bool hasMatch)
    {
        if(!hasMatch){GUI.Label(view,"검색 결과가 없습니다.",bodyStyle);return;}
        var entry=BlueprintGuide.Entries[selected];float width=Mathf.Max(1,view.width-4);
        float sectionH=Height(entry.Section,sectionStyle,width),titleH=TitleHeight(entry.HeadingFor(englishUnlocked),width);
        float bodyH=Height(entry.DescriptionFor(englishUnlocked),bodyStyle,width);
        float codeHeight=Mathf.Max(codeStyle.CalcSize(new GUIContent(entry.ExampleFor(englishUnlocked)+"\n ")).y,codeStyle.padding.vertical+(BlueprintViewport.Row(entry.ExampleFor(englishUnlocked),entry.ExampleFor(englishUnlocked).Length)+2)*Mathf.Max(1,codeStyle.lineHeight));
        string expected=entry.IsFunctionReference?"":entry.Expected;
        float expectedH=expected.Length==0?0:SubsectionHeight("실행 결과",expected,width);
        string note=entry.IsFunctionReference?"":entry.NoteFor(englishUnlocked);
        float noteH=note.Length==0?0:SubsectionHeight("주의할 점",note,width);
        float total=sectionH+titleH+bodyH+codeHeight+expectedH+noteH+114;
        detailScroll=BeginScroll(view,detailScroll,new Rect(0,0,width,Mathf.Max(view.height,total)));
        float y=0;GUI.Label(new Rect(0,y,width,sectionH),entry.Section,sectionStyle);y+=sectionH+4;
        GUI.Label(new Rect(0,y,width,titleH),entry.HeadingFor(englishUnlocked),titleStyle);y+=titleH+10;
        GUI.Label(new Rect(0,y,width,bodyH),entry.DescriptionFor(englishUnlocked),bodyStyle);y+=bodyH+12;
        if(GUI.Button(new Rect(0,y,Mathf.Min(width,150),28),copied?"복사했습니다":"예제 복사",buttonStyle)){GUIUtility.systemCopyBuffer=entry.ExampleFor(englishUnlocked);copied=true;}
        y+=34;
        Rect codeRect=new Rect(0,y,width,codeHeight);
        Color old=GUI.color;GUI.color=new Color(.13f,.15f,.18f);GUI.DrawTexture(codeRect,Texture2D.whiteTexture);GUI.color=old;
        float codeWidth=Mathf.Max(width,codeStyle.CalcSize(new GUIContent(entry.ExampleFor(englishUnlocked)+" ")).x+12);
        codeScroll=BeginScroll(codeRect,codeScroll,new Rect(0,0,codeWidth,codeHeight),true);
        GUI.Label(new Rect(0,0,codeWidth,codeHeight),entry.ExampleFor(englishUnlocked),codeStyle);
        GUI.EndScrollView();y+=codeHeight+12;
        if(expectedH>0)DrawSubsection(y,width,"실행 결과",expected);y+=expectedH+12;
        if(noteH>0)DrawSubsection(y,width,"주의할 점",note);
        GUI.EndScrollView();
    }
}
