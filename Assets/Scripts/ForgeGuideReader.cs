using System.Collections.Generic;
using UnityEngine;

// Per-book browsing state survives moving, resizing and closing the reference window.
public sealed partial class ForgeGuideReader
{
    ForgeTechnology technology; ForgeBlacksmiths smiths;
    string accessSignature;
    readonly Dictionary<string,string> proseCache=new Dictionary<string,string>();
    string VisibleProse(string text){if(!proseCache.TryGetValue(text,out var result)){result=BlueprintResearchGuide.VisibleProse(text,technology,smiths);proseCache[text]=result;}return result;}
    readonly HashSet<int> visible=new HashSet<int>(),fresh=new HashSet<int>(),visibleMaterials=new HashSet<int>(),freshMaterials=new HashSet<int>();
    bool newOnly;
    float noticeUntil;int knownSpeed;bool knownEnglish;
    public int NewCount => fresh.Count+freshMaterials.Count;
    public bool HasNew => NewCount>0;
    public void Sync(ForgeTechnology tech,ForgeBlacksmiths team)
    {
        string signature=string.Join(",",tech.ExportNodes())+"|"+team.hireTier+"|"+team.favorUnlocked+team.positionUnlocked+team.demandUnlocked;
        bool reset=technology!=tech||smiths!=team;
        if(!reset&&signature==accessSignature)return;
        if(reset){fresh.Clear();freshMaterials.Clear();visible.Clear();visibleMaterials.Clear();section="게임 소개";newOnly=false;}
        bool notify=!reset&&accessSignature!=null;
        technology=tech;smiths=team;accessSignature=signature;
        var next=new HashSet<int>();
        for(int i=0;i<BlueprintGuide.Entries.Count;i++)if(BlueprintResearchGuide.EntryVisible(BlueprintGuide.Entries[i],tech,team))next.Add(i);
        foreach(int i in next)if(notify&&!visible.Contains(i)){fresh.Add(i);noticeUntil=Time.realtimeSinceStartup+8;}
        if(notify&&tech.MaxSpeedLevel>knownSpeed)foreach(int i in next)if(BlueprintGuide.Entries[i].Feature==BlueprintFeature.Speed){fresh.Add(i);noticeUntil=Time.realtimeSinceStartup+8;}
        if(notify&&tech.Owns(ForgeTechnology.EnglishResearch)&&!knownEnglish)foreach(int i in next)if(BlueprintGuide.Entries[i].Id=="notation"||BlueprintGuide.Entries[i].Feature==BlueprintFeature.Match||System.Array.IndexOf(ForgeTechnology.ControlFeatures,BlueprintGuide.Entries[i].Feature)>=0){fresh.Add(i);noticeUntil=Time.realtimeSinceStartup+8;}
        knownEnglish=tech.Owns(ForgeTechnology.EnglishResearch);knownSpeed=tech.MaxSpeedLevel;
        fresh.IntersectWith(next);visible.Clear();visible.UnionWith(next);
        var materials=new HashSet<int>();for(int i=0;i<ForgeMaterialGuide.Entries.Count;i++)if(tech.MaterialAvailable(ForgeMaterialGuide.Entries[i].Number))materials.Add(i);
        foreach(int i in materials)if(notify&&!visibleMaterials.Contains(i)){freshMaterials.Add(i);noticeUntil=Time.realtimeSinceStartup+8;}
        freshMaterials.IntersectWith(materials);visibleMaterials.Clear();visibleMaterials.UnionWith(materials);
        proseCache.Clear();
        if(material>=0&&!visibleMaterials.Contains(material))material=-1;
        if(!SectionVisible(section)){section="게임 소개";newOnly=false;}
    }
    bool SectionVisible(string value)
    {
        if(BlueprintResearchGuide.BookmarkSection(value)!=value)return false;
        if(value==""||value=="게임 소개"||value=="재료")return true;
        foreach(int i in visible)if(BlueprintGuide.Entries[i].Section==value)return true;
        return false;
    }
    bool SectionNew(string value)
    {if(value=="재료")return freshMaterials.Count>0;foreach(int i in fresh)if(BlueprintGuide.Entries[i].Section==value)return true;return false;}
    public void DrawNotice(Font font)
    {
        if(!HasNew||Time.realtimeSinceStartup>noticeUntil)return;
        float width=Mathf.Min(420,Screen.width-24);
        var style=new GUIStyle(GUI.skin.box){font=font,fontSize=22,fontStyle=FontStyle.Bold,wordWrap=true};
        string text="도면 안내서에 새 내용 "+NewCount+"개 추가!";
        float height=ForgeTextLayout.Height(text,style,width);
        Color old=GUI.color;GUI.color=Color.Lerp(Color.white,new Color(1,.8f,.3f),(.5f+.5f*Mathf.Sin(Time.realtimeSinceStartup*6)));
        ForgeTextLayout.Label(new Rect((Screen.width-width)/2,Screen.height-height-12,width,height),text,style);GUI.color=old;
    }
    string query="";
    int selected;
    const float BookmarkWidth=104, BookmarkStep=44, BookmarkHeight=40;
    public static Rect BookmarkRail(Rect area) => new Rect(area.x-BookmarkWidth-2,area.y,BookmarkWidth,area.height);
    Texture2D bookmarkTexture;
    public bool ShowBookmarks { get; set; } = true;
    bool showContents,copied;
    bool englishUnlocked;
    Vector2 indexScroll,detailScroll,codeScroll,leftTabScroll,materialScroll;
    int material=-1;
    string section="게임 소개";
    // Serialized GuideSection indices remain stable; merged bookmarks are hidden.
    public static readonly string[] Sections={"게임 소개","반복","재료","시작하기","대장간 공정","금속 가공","행운","마력 가공","공방 운영","시간 조회","기술 연구","대장장이와 다이아몬드","조건","사용자 함수","출력","자료 묶음","기본 문법","완성 예제","문제가 생겼을 때"};
    // Header and page insets surround the full bookmark rail.
    public static float DefaultWindowHeight => Mathf.Min(660,ForgeGuideUI.HeaderHeight+20+Sections.Length*BookmarkStep);
    static readonly string[] Tabs={"소개","반복","재료","시작","공정","금속","행운","마력","운영","시간","연구","대장장이","조건","함수","출력","자료","기본","예제","오류"};
    static readonly int[] BookmarkOrder={0,2,3,4,5,6,7,8,9,10,11,12,1,13,14,15,16,17,18};
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
        tabStyle=new GUIStyle(GUIStyle.none){wordWrap=true,font=font,fontSize=fontSize,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,richText=false,padding=new RectOffset(0,0,0,0)};
        foreach(var state in new[]{tabStyle.normal,tabStyle.hover,tabStyle.active,tabStyle.focused,tabStyle.onNormal,tabStyle.onHover,tabStyle.onActive,tabStyle.onFocused}){state.background=null;
#if UNITY_EDITOR
state.scaledBackgrounds=new Texture2D[0];
#endif
state.textColor=new Color(.98f,.94f,.83f);}
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
    {fresh.Remove(index);material=-1;selected=index;detailScroll=Vector2.zero;codeScroll=Vector2.zero;showContents=false;copied=false;}
    public void Draw(Rect area,Font font,int fontSize,bool englishGuideUnlocked)
    {
        if(englishUnlocked!=englishGuideUnlocked){englishUnlocked=englishGuideUnlocked;copied=false;detailScroll=Vector2.zero;codeScroll=Vector2.zero;}
        Styles(font,fontSize);
        Color old=GUI.color;GUI.color=new Color(.96f,.92f,.82f);GUI.DrawTexture(area,Texture2D.whiteTexture);GUI.color=old;
        // Ribbons extend beyond the cover; the reading page keeps its full width.
        if(ShowBookmarks)DrawBookmarks(BookmarkRail(area));
        const float contentMargin=24;
        Rect page=new Rect(area.x+contentMargin,area.y+4,Mathf.Max(1,area.width-contentMargin*2),Mathf.Max(1,area.height-8));
        if(HasNew){
            float h=Mathf.Max(36,Height("새 내용 "+NewCount+"개 · 확인하기",buttonStyle,page.width));
            Color previous=GUI.backgroundColor;GUI.backgroundColor=Color.Lerp(new Color(1,.8f,.3f),Color.white,.5f+.5f*Mathf.Sin(Time.realtimeSinceStartup*6));
            if(ForgeTextLayout.Button(new Rect(page.x,page.y,page.width,h),"새 내용 "+NewCount+"개 · 확인하기",buttonStyle)){
                if(fresh.Count>0){section="";query="";newOnly=true;showContents=true;indexScroll=Vector2.zero;}
                else{section="재료";material=-1;materialScroll=Vector2.zero;indexScroll=Vector2.zero;}
            }
            GUI.backgroundColor=previous;page.y+=h+8;page.height=Mathf.Max(1,page.height-h-8);
        }
        if(section=="재료"){DrawMaterials(page);return;}
        if(section=="게임 소개"){DrawIntroduction(page);return;}
        if(ForgeTextLayout.Button(new Rect(page.x,page.y,58,28),new GUIContent(showContents?"본문":"<-",showContents?"본문 보기":"목록으로 돌아가기"),buttonStyle))showContents=!showContents;
        Rect searchRect=new Rect(page.x+62,page.y,Mathf.Max(1,page.width-62),28);
        string next=GUI.TextField(searchRect,query,80,searchStyle);
        if(string.IsNullOrEmpty(next)){
            var placeholderStyle=new GUIStyle(GUIStyle.none){font=font,fontSize=Mathf.Max(10,fontSize-3),alignment=TextAnchor.MiddleLeft,clipping=TextClipping.Clip};
            placeholderStyle.normal.textColor=new Color(.55f,.55f,.55f);
            ForgeTextLayout.Label(new Rect(searchRect.x+6,searchRect.y,Mathf.Max(1,searchRect.width-12),searchRect.height),"Search...",placeholderStyle);
        }
        if(next!=query){query=next;section="";newOnly=false;indexScroll=Vector2.zero;showContents=true;}
        var matches=new List<int>();
        for(int i=0;i<BlueprintGuide.Entries.Count;i++)if(visible.Contains(i)&&(!newOnly||!showContents||fresh.Contains(i))&&(section.Length==0||BlueprintGuide.Entries[i].Section==section)&&BlueprintGuide.Entries[i].Matches(query,englishUnlocked))matches.Add(i);
        if(matches.Count>0&&!matches.Contains(selected)){selected=matches[0];detailScroll=Vector2.zero;codeScroll=Vector2.zero;copied=false;}
        Rect view=new Rect(page.x,page.y+34,page.width,Mathf.Max(1,page.height-68));
        if(showContents)DrawIndex(view,matches);else DrawDetail(view,matches.Count>0);
        float bottom=page.yMax-28;int position=matches.IndexOf(selected);bool wasEnabled=GUI.enabled;
        GUI.enabled=wasEnabled&&position>0;
        if(ForgeTextLayout.Button(new Rect(page.x,bottom,32,28),"<",buttonStyle))Select(matches[position-1]);
        GUI.enabled=wasEnabled&&position>=0&&position<matches.Count-1;
        if(ForgeTextLayout.Button(new Rect(page.xMax-32,bottom,32,28),">",buttonStyle))Select(matches[position+1]);
        GUI.enabled=wasEnabled;
        var pageStyle=new GUIStyle(sectionStyle){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Normal};
        ForgeTextLayout.Label(new Rect(page.x+34,bottom,Mathf.Max(1,page.width-68),28),matches.Count==0?"0 / 0":(position+1)+" / "+matches.Count,pageStyle);
    }
    int VisibleSectionCount {get{int count=0;foreach(string value in Sections)if(SectionVisible(value))count++;return count;}}
    public bool BookmarkContains(Rect rail,Vector2 point)
    {
        if(!rail.Contains(point))return false;
        float contentHeight=Mathf.Max(0,VisibleSectionCount*BookmarkStep-(BookmarkStep-BookmarkHeight));
        float scroll=Mathf.Clamp(leftTabScroll.y,0,Mathf.Max(0,contentHeight-rail.height));
        float y=point.y-rail.y+scroll;
        return y>=0&&y<contentHeight&&y%BookmarkStep<BookmarkHeight;
    }
    void DrawBookmarks(Rect rail)
    {
        if(bookmarkTexture==null)bookmarkTexture=Resources.Load<Texture2D>("GuideBookmark");
        float width=rail.width;
        float contentHeight=Mathf.Max(0,VisibleSectionCount*BookmarkStep-(BookmarkStep-BookmarkHeight));
        rail.height=Mathf.Min(rail.height,contentHeight);
        leftTabScroll.y=Mathf.Clamp(leftTabScroll.y,0,Mathf.Max(0,contentHeight-rail.height));
        bool hitBookmark=BookmarkContains(rail,Event.current.mousePosition);
        GUIStyle scrollBackground=GUI.skin.scrollView;
        GUI.skin.scrollView=GUIStyle.none;
        leftTabScroll=GUI.BeginScrollView(rail,leftTabScroll,new Rect(0,0,width,contentHeight),false,false,GUIStyle.none,GUIStyle.none);
        GUI.skin.scrollView=scrollBackground;
        int row=0;
        foreach(int i in BookmarkOrder){
            if(!SectionVisible(Sections[i]))continue;
            bool active=section==Sections[i];
            Rect hit=new Rect(0,row++*BookmarkStep,width,BookmarkHeight);
            if(ForgeTextLayout.Button(hit,new GUIContent("",Sections[i]),GUIStyle.none)){
                GUI.FocusControl(null);
                material=-1;newOnly=false;section=Sections[i];query="";showContents=true;
                indexScroll=Vector2.zero;materialScroll=Vector2.zero;detailScroll=Vector2.zero;codeScroll=Vector2.zero;
                active=true;
            }
            Rect tab=hit;
            Color old=GUI.color;
            GUI.color=old*(SectionNew(Sections[i])?Color.Lerp(Color.white,new Color(1.5f,1.2f,.5f),.5f+.5f*Mathf.Sin(Time.realtimeSinceStartup*6)):active?new Color(1.35f,1.35f,1.35f,1):new Color(.78f,.78f,.78f,1));
            // Trim the transparent source margins in UV space; preserve the original sprite file.
            if(bookmarkTexture!=null)GUI.DrawTextureWithTexCoords(tab,bookmarkTexture,new Rect(80f/1920f,139f/819f,1750f/1920f,540f/819f),true);

            GUI.color=old;
            ForgeTextLayout.Label(new Rect(tab.x+10,tab.y,tab.width-14,tab.height),Tabs[i],tabStyle);
        }
        GUI.EndScrollView(hitBookmark);
    }
    void DrawMaterials(Rect page)
    {
        if(material>=0){
            if(ForgeTextLayout.Button(new Rect(page.x,page.y,58,30),new GUIContent("<-","재료 목록으로 돌아가기"),buttonStyle)){material=-1;materialScroll=Vector2.zero;}
            if(material>=0)DrawMaterial(new Rect(page.x,page.y+38,page.width,Mathf.Max(1,page.height-38)));
            return;
        }
        float titleH=TitleHeight("재료",page.width);
        ForgeTextLayout.Label(new Rect(page.x,page.y,page.width,titleH),"재료",titleStyle);
        Rect view=new Rect(page.x,page.y+titleH+8,page.width,Mathf.Max(1,page.height-titleH-8));
        float width=Mathf.Max(1,view.width-22),total=0;
        foreach(int i in visibleMaterials){var entry=ForgeMaterialGuide.Entries[i];total+=Mathf.Max(44,Height((freshMaterials.Contains(i)?"새 항목 · ":"")+entry.Heading,entryStyle,width))+4;}
        indexScroll=BeginScroll(view,indexScroll,new Rect(0,0,width,Mathf.Max(view.height,total)));
        float y=0;
        for(int i=0;i<ForgeMaterialGuide.Entries.Count;i++){
            if(!visibleMaterials.Contains(i))continue;
            var entry=ForgeMaterialGuide.Entries[i];
            string heading=(freshMaterials.Contains(i)?"새 항목 · ":"")+entry.Heading;
            float height=Mathf.Max(44,Height(heading,entryStyle,width));
            if(ForgeTextLayout.Button(new Rect(0,y,width,height),heading,entryStyle)){freshMaterials.Remove(i);material=i;materialScroll=Vector2.zero;}
            y+=height+4;
        }
        GUI.EndScrollView();
    }
    const string IntroductionText="아무리 숫자 언어라고 하지만, Python을 바탕으로 둔 언어입니다.\n\n기본 공정으로 검을 만들고 판매해 연구 자금을 모으세요. 철·나무·물은 재료 상점에서 구매할 수 있습니다.\n\n구매하지 않은 기술의 설명과 예제는 숨겨져 있습니다. 테크 트리에서 연구를 구매하면 사용할 수 있는 내용이 안내서에 추가되며, 반짝이는 알림과 책갈피로 알려드립니다.\n\n책갈피는 마우스 휠로 넘깁니다. 각 페이지의 준비물을 확인하고 실행하세요. 예제를 복사해도 재료는 지급되지 않습니다.";
    void DrawIntroduction(Rect page)
    {
        float width=Mathf.Max(1,page.width-22);
        float titleH=TitleHeight(BlueprintGuide.IntroductionTitle,width);
        float bodyH=Height(IntroductionText,bodyStyle,width);
        detailScroll=BeginScroll(page,detailScroll,new Rect(0,0,width,Mathf.Max(page.height,titleH+bodyH+24)));
        ForgeTextLayout.Label(new Rect(0,0,width,titleH),BlueprintGuide.IntroductionTitle,titleStyle);
        ForgeTextLayout.Label(new Rect(0,titleH+12,width,bodyH),IntroductionText,bodyStyle);
        GUI.EndScrollView();
    }
    void DrawMaterial(Rect page)
    {
        var entry=ForgeMaterialGuide.Entries[material];float width=Mathf.Max(1,page.width-22);
        string status=BlueprintResearchGuide.MaterialInfo(entry.Number,englishUnlocked);
        string description=entry.Number==7||entry.Number==8||entry.Number==9?BlueprintResearchGuide.FormatGold(entry.Description):VisibleProse(entry.Description);
        float titleH=TitleHeight(entry.Heading,width),statusH=Height(status,sectionStyle,width),bodyH=Height(description,bodyStyle,width);
        materialScroll=BeginScroll(page,materialScroll,new Rect(0,0,width,Mathf.Max(page.height,titleH+statusH+bodyH+36)));
        ForgeTextLayout.Label(new Rect(0,0,width,titleH),entry.Heading,titleStyle);
        ForgeTextLayout.Label(new Rect(0,titleH+8,width,statusH),status,sectionStyle);
        ForgeTextLayout.Label(new Rect(0,titleH+statusH+20,width,bodyH),description,bodyStyle);
        GUI.EndScrollView();
    }
    // Visible scrollbars expose overflow, including long code lines. Callers reserve the gutter.
    static Vector2 BeginScroll(Rect viewport,Vector2 scroll,Rect content,bool horizontalCode=false)
    {
        var ev=Event.current;
        if(horizontalCode&&ev.type==EventType.ScrollWheel&&viewport.Contains(ev.mousePosition)
            &&(ev.shift||Mathf.Abs(ev.delta.x)>0)&&content.width>viewport.width){
            float delta=ev.shift?ev.delta.y:ev.delta.x;
            scroll.x=Mathf.Clamp(scroll.x+delta*20f,0,content.width-viewport.width);
            ev.Use();
        }
        return GUI.BeginScrollView(viewport,scroll,content,horizontalCode&&content.width>viewport.width-20,false);
    }
    void DrawIndex(Rect view,List<int> matches)
    {
        float width=Mathf.Max(1,view.width-22),height=0;
        foreach(int index in matches)height+=Mathf.Max(44,Height((fresh.Contains(index)?"새 항목 · ":"")+BlueprintGuide.Entries[index].Section+"\n"+BlueprintGuide.Entries[index].HeadingFor(englishUnlocked),entryStyle,width))+4;
        indexScroll=BeginScroll(view,indexScroll,new Rect(0,0,width,Mathf.Max(view.height,height)));
        float y=0;
        foreach(int index in matches){
            var entry=BlueprintGuide.Entries[index];string label=(fresh.Contains(index)?"새 항목 · ":"")+entry.Section+"\n"+entry.HeadingFor(englishUnlocked);
            float h=Mathf.Max(44,Height(label,entryStyle,width));
            Color old=GUI.backgroundColor;if(index==selected)GUI.backgroundColor=new Color(.70f,.81f,.86f);
            if(ForgeTextLayout.Button(new Rect(0,y,width,h),label,entryStyle))Select(index);
            GUI.backgroundColor=old;y+=h+4;
        }
        if(matches.Count==0)ForgeTextLayout.Label(new Rect(0,0,width,Height("함수 이름이나 숫자로 찾아보세요.",bodyStyle,width)),"함수 이름이나 숫자로 찾아보세요.",bodyStyle);
        GUI.EndScrollView();
    }
    float Height(string text,GUIStyle style,float width){return ForgeTextLayout.Height(text,style,width);}
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
        ForgeTextLayout.Label(new Rect(0,y,width,headingH),heading,sectionStyle);
        ForgeTextLayout.Label(new Rect(0,y+headingH+4,width,Height(body,bodyStyle,width)),body,bodyStyle);
    }
    void DrawReadOnlyCode(Rect rect,string text,string entryId)
    {
        string controlName="GuideExample_"+entryId;
        var ev=Event.current;
        if(GUI.GetNameOfFocusedControl()==controlName){
            if(ev.type==EventType.KeyDown){
                bool shortcut=ev.control||ev.command;
                bool copyOrSelect=shortcut&&!ev.alt&&(ev.keyCode==KeyCode.C||ev.keyCode==KeyCode.A);
                bool navigation=ev.keyCode==KeyCode.LeftArrow||ev.keyCode==KeyCode.RightArrow
                    ||ev.keyCode==KeyCode.UpArrow||ev.keyCode==KeyCode.DownArrow
                    ||ev.keyCode==KeyCode.Home||ev.keyCode==KeyCode.End
                    ||ev.keyCode==KeyCode.PageUp||ev.keyCode==KeyCode.PageDown;
                if(!copyOrSelect&&!navigation)ev.Use();
            }
            if((ev.type==EventType.ValidateCommand||ev.type==EventType.ExecuteCommand)
                &&ev.commandName!="Copy"&&ev.commandName!="SelectAll")ev.Use();
        }
        GUI.SetNextControlName(controlName);
        // Keep native selection and Copy, but never commit an edit to the example.
        GUI.TextArea(rect,text,codeStyle);
        if(GUI.GetNameOfFocusedControl()==controlName){
            var editor=(TextEditor)GUIUtility.GetStateObject(typeof(TextEditor),GUIUtility.keyboardControl);
            if(editor.text!=text){
                int cursor=editor.cursorIndex,selection=editor.selectIndex;
                editor.text=text;
                editor.cursorIndex=Mathf.Clamp(cursor,0,text.Length);
                editor.selectIndex=Mathf.Clamp(selection,0,text.Length);
            }
        }
    }
    void DrawDetail(Rect view,bool hasMatch)
    {
        if(!hasMatch){ForgeTextLayout.Label(view,"검색 결과가 없습니다.",bodyStyle);return;}
        var entry=BlueprintGuide.Entries[selected];float width=Mathf.Max(1,view.width-22);
        float sectionH=Height(entry.Section,sectionStyle,width),titleH=TitleHeight(entry.HeadingFor(englishUnlocked),width);
        string description=VisibleProse(entry.DescriptionFor(englishUnlocked));
        string example=entry.ExampleFor(englishUnlocked);
        if(entry.Feature==BlueprintFeature.Speed&&technology.MaxSpeedLevel==1)example=example.Replace("222(2)","222(1)").Replace("speed(2)","speed(1)");
        float bodyH=Height(description,bodyStyle,width);
        float codeHeight=example.Length==0?0:Mathf.Max(codeStyle.CalcSize(new GUIContent(example+"\n ")).y,codeStyle.padding.vertical+(BlueprintViewport.Row(example,example.Length)+2)*Mathf.Max(1,codeStyle.lineHeight));
        string expected=entry.IsFunctionReference?"":BlueprintResearchGuide.FormatGold(entry.Expected);
        float expectedH=expected.Length==0?0:SubsectionHeight("실행 결과",expected,width);
        string note=VisibleProse(entry.NoteFor(englishUnlocked));
        float noteH=note.Length==0?0:SubsectionHeight("주의할 점",note,width);
        float total=sectionH+titleH+bodyH+codeHeight+expectedH+noteH+170;
        detailScroll=BeginScroll(view,detailScroll,new Rect(0,0,width,Mathf.Max(view.height,total)));
        float y=0;ForgeTextLayout.Label(new Rect(0,y,width,sectionH),entry.Section,sectionStyle);y+=sectionH+4;
        ForgeTextLayout.Label(new Rect(0,y,width,titleH),entry.HeadingFor(englishUnlocked),titleStyle);y+=titleH+10;
        ForgeTextLayout.Label(new Rect(0,y,width,bodyH),description,bodyStyle);y+=bodyH+12;
        if(example.Length>0){
        if(ForgeTextLayout.Button(new Rect(0,y,Mathf.Min(width,150),28),copied?"복사했습니다":"예제 복사",buttonStyle)){GUIUtility.systemCopyBuffer=example;copied=true;}
        y+=34;
        Rect codeRect=new Rect(0,y,width,codeHeight);
        Color old=GUI.color;GUI.color=new Color(.13f,.15f,.18f);GUI.DrawTexture(codeRect,Texture2D.whiteTexture);GUI.color=old;
        float codeWidth=Mathf.Max(width,codeStyle.CalcSize(new GUIContent(example+" ")).x+12);
        codeRect.height+=20;
        codeScroll=BeginScroll(codeRect,codeScroll,new Rect(0,0,codeWidth,codeHeight),true);
        DrawReadOnlyCode(new Rect(0,0,codeWidth,codeHeight),example,entry.Id);
        GUI.EndScrollView();y+=codeRect.height+12;
        }
        if(expectedH>0)DrawSubsection(y,width,"실행 결과",expected);y+=expectedH+12;
        if(noteH>0)DrawSubsection(y,width,"주의할 점",note);
        GUI.EndScrollView();
    }
}
