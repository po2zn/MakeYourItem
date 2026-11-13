using System;
using System.Collections.Generic;
using UnityEngine;

public partial class ForgeMvp
{
    const string LanguageSetting="MakeYourItem.Settings.Language";
    const string WidthSetting="MakeYourItem.Settings.Width",HeightSetting="MakeYourItem.Settings.Height";
    // Selection metadata only. No translation tables or locale switching are applied yet.
    public static readonly System.Collections.ObjectModel.ReadOnlyCollection<string> LanguageCodes=Array.AsReadOnly(new[]{
        "en","ru","it","ar","sk","cs","es","tr","sl","pt-BR","he","hi","ro","pl","ko","zh-Hans","ja"});
    static readonly string[] languageLabels={
        "English","Русский","Italiano","العربية","Slovenčina","Čeština","Español",
        "Türkçe","Slovenščina","Português (Brasil)","עברית","हिन्दी","Română","Polski",
        "한국어","简体中文","日本語"};
    readonly List<Vector2Int> displaySizes=new List<Vector2Int>();
    int selectedLanguage,selectedDisplaySize;
    string settingsMessage="";
    GUIStyle settingsOption,settingsRowLabel,settingsTitle,settingsText,settingsButton,settingsSlider,settingsSliderThumb;
    GUIStyle SettingsButton => settingsButton ?? (settingsButton=SettingsWithoutHover(menuButton));
    static GUIStyle SettingsWithoutHover(GUIStyle source)
    {
        var style=new GUIStyle(source);
        style.hover=style.normal;
        style.onHover=style.onNormal;
        return style;
    }
    Font settingsLanguageFont;
    Vector2 settingsDropdownScroll,settingsPageScroll;
    int settingsDropdown;
    Rect settingsDropdownAnchor;
    void AddDisplaySize(int width,int height)
    {
        var size=new Vector2Int(width,height);
        if(width>0&&height>0&&!displaySizes.Contains(size))displaySizes.Add(size);
    }
    void RefreshDisplaySizes()
    {
        displaySizes.Clear();
        foreach(var resolution in Screen.resolutions)AddDisplaySize(resolution.width,resolution.height);
        var desktop=Screen.currentResolution;
        // Windowed players and the Editor may report no display mode list.
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(3840,2160)})
            if(size.x<=desktop.width&&size.y<=desktop.height)AddDisplaySize(size.x,size.y);
        AddDisplaySize(Screen.width,Screen.height);
        displaySizes.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
        selectedDisplaySize=Math.Max(0,displaySizes.IndexOf(new Vector2Int(Screen.width,Screen.height)));
    }
    void InitializeDisplaySettings()
    {
        selectedLanguage=LanguageCodes.IndexOf(PlayerPrefs.GetString(LanguageSetting,"en"));
        if(selectedLanguage<0)selectedLanguage=0;
        RefreshDisplaySizes();
        var saved=new Vector2Int(PlayerPrefs.GetInt(WidthSetting,Screen.width),PlayerPrefs.GetInt(HeightSetting,Screen.height));
        int index=displaySizes.IndexOf(saved);
        if(index>=0){selectedDisplaySize=index;if(saved.x!=Screen.width||saved.y!=Screen.height)Screen.SetResolution(saved.x,saved.y,Screen.fullScreen);}
    }
    void DrawSettingsScreen()
    {
        if(settingsOption==null){
            settingsLanguageFont=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Malgun Gothic","Microsoft YaHei","Yu Gothic","Nirmala UI","Arial"},20);
            settingsOption=new GUIStyle(SettingsButton){font=settingsLanguageFont,fontSize=20,alignment=TextAnchor.MiddleLeft,wordWrap=false};
            settingsTitle=SettingsWithoutHover(menuTitle);
            settingsText=SettingsWithoutHover(menuText);
            settingsRowLabel=new GUIStyle(settingsText){alignment=TextAnchor.MiddleLeft};
            settingsSlider=SettingsWithoutHover(GUI.skin.horizontalSlider);
            settingsSliderThumb=SettingsWithoutHover(GUI.skin.horizontalSliderThumb);
        }
        if(displaySizes.Count==0)RefreshDisplaySizes();
        var ev=Event.current;
        if(ev.type==EventType.KeyDown&&ev.keyCode==KeyCode.Escape){
            if(settingsDropdown!=0)settingsDropdown=0;else {ForgeAudioSettings.Save();titlePage=0;}
            ev.Use();
        }
        EventType savedEvent=ev.type;
        bool popupOpen=settingsDropdown!=0;
        if(popupOpen&&(ev.isMouse||ev.isKey||ev.type==EventType.ScrollWheel))ev.type=EventType.Ignore;
        ForgeTitleStyle.Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.04f,.07f,.11f,.35f));
        float width=Mathf.Min(840,Mathf.Max(1,Screen.width-68));
        float x=(Screen.width-20-width)/2;
        float labelWidth=width*.28f,controlX=x+labelWidth,controlWidth=width-labelWidth;
        const string audioHelp="변경 즉시 적용 · 자동 저장 · 0%는 음소거\n전체 음량은 BGM과 효과음 모두에 적용됩니다.";
        float audioHelpHeight=ForgeTextLayout.Height(audioHelp,settingsRowLabel,controlWidth);
        const string typingHelp="키 입력 소리 · 효과음(SFX) 음량 적용";
        const string forgeHelp="입력한 코드가 붉게 달아올랐다가 금속빛으로 식으며, 화면이 미세하게 흔들립니다.";
        float typingHeight=ForgeTextLayout.Height(typingHelp,settingsRowLabel,controlWidth),forgeHeight=ForgeTextLayout.Height(forgeHelp,settingsRowLabel,controlWidth);
        float panelHeight=952+Mathf.Max(44,audioHelpHeight)+144+typingHeight+forgeHeight;
        float y=Mathf.Max(16,(Screen.height-panelHeight)/2);
        settingsPageScroll=GUI.BeginScrollView(new Rect(0,0,Screen.width,Screen.height),settingsPageScroll,new Rect(0,0,Screen.width-20,Mathf.Max(Screen.height,y+panelHeight+12)));
        ForgeTitleStyle.Window(new Rect(x-16,y-8,width+32,panelHeight),false);
        ForgeTextLayout.Label(new Rect(x,y,width,58),"Settings · 설정",settingsTitle);
        ForgeTextLayout.Label(new Rect(x,y+64,width,30),"게임 환경을 원하는 방식으로 설정하세요.",settingsText);
        y+=120;
        ForgeTextLayout.Label(new Rect(x,y,labelWidth-16,48),"언어",settingsRowLabel);
        if(ForgeTextLayout.Button(new Rect(controlX,y,controlWidth,48),languageLabels[selectedLanguage]+"  ▾",settingsOption))OpenSettingsDropdown(1,new Rect(controlX,y,controlWidth,48),selectedLanguage);
        ForgeTextLayout.Label(new Rect(controlX,y+52,controlWidth,28),"선택값만 저장됩니다. 번역 기능은 추후 지원됩니다.",settingsRowLabel);
        y+=94;
        ForgeTextLayout.Label(new Rect(x,y,labelWidth-16,48),"해상도",settingsRowLabel);
        var size=displaySizes[selectedDisplaySize];
        if(ForgeTextLayout.Button(new Rect(controlX,y,controlWidth,48),size.x+" × "+size.y+"  ▾",settingsOption))OpenSettingsDropdown(2,new Rect(controlX,y,controlWidth,48),selectedDisplaySize);
        y+=72;
        ForgeTextLayout.Label(new Rect(x,y,labelWidth-16,48),"화면 모드",settingsRowLabel);
        if(ForgeTextLayout.Button(new Rect(controlX,y,controlWidth,48),(Screen.fullScreen?"전체 화면":"창 모드")+"  ▾",settingsOption))OpenSettingsDropdown(3,new Rect(controlX,y,controlWidth,48),Screen.fullScreen?0:1);
        y+=72;
        float master=DrawVolumeRow(x,y,labelWidth,controlWidth,"전체 음량",ForgeAudioSettings.Master);
        y+=64;
        float music=DrawVolumeRow(x,y,labelWidth,controlWidth,"배경음 (BGM)",ForgeAudioSettings.Music);
        y+=64;
        float sfx=DrawVolumeRow(x,y,labelWidth,controlWidth,"효과음 (SFX)",ForgeAudioSettings.Sfx);
        if(!Mathf.Approximately(master,ForgeAudioSettings.Master)||!Mathf.Approximately(music,ForgeAudioSettings.Music)||!Mathf.Approximately(sfx,ForgeAudioSettings.Sfx))
            ForgeAudioSettings.SetVolumes(master,music,sfx);
        y+=56;
        if(ForgeTextLayout.Button(new Rect(controlX,y,Mathf.Min(180,controlWidth),40),"효과음 미리 듣기",SettingsButton))ForgeSoundEffects.Preview();
        y+=48;
        ForgeTextLayout.Label(new Rect(controlX,y,controlWidth,audioHelpHeight),audioHelp,settingsRowLabel);
        y+=Mathf.Max(44,audioHelpHeight)+16;
        ForgeTextLayout.Label(new Rect(x,y,labelWidth-16,48),"코드 색상",settingsRowLabel);
        if(ForgeTextLayout.Button(new Rect(controlX,y,controlWidth,48),ForgeCodeColors.PaletteNames[ForgeCodeColors.Palette]+"  ▾",settingsOption))OpenSettingsDropdown(4,new Rect(controlX,y,controlWidth,48),ForgeCodeColors.Palette);
        var kinds=new[]{ForgeSyntax.Kind.Material,ForgeSyntax.Kind.Function,ForgeSyntax.Kind.Reserved,ForgeSyntax.Kind.Comment};
        var samples=new[]{"재료 · 1","함수 · Heat()","예약어 · if","주석 · # 설명"};
        for(int i=0;i<4;i++){
            var preview=new GUIStyle(settingsRowLabel){fontSize=16};preview.normal.textColor=ForgeCodeColors.TokenColor(kinds[i]);
            ForgeTextLayout.Label(new Rect(controlX+(i%2)*controlWidth/2,y+52+(i/2)*30,controlWidth/2-8,28),samples[i],preview);
        }
        y+=156;
        ForgeTextLayout.Label(new Rect(x,y,labelWidth-16,48),"코드 입력 소리",settingsRowLabel);
        if(ForgeTextLayout.Button(new Rect(controlX,y,controlWidth,48),ForgeCodePreferences.TypingSound?"ON · 켜짐":"OFF · 꺼짐",SettingsButton))ForgeCodePreferences.SetTypingSound(!ForgeCodePreferences.TypingSound);
        ForgeTextLayout.Label(new Rect(controlX,y+52,controlWidth,typingHeight),typingHelp,settingsRowLabel);y+=72+typingHeight;
        ForgeTextLayout.Label(new Rect(x,y,labelWidth-16,48),"Forge Code",settingsRowLabel);
        if(ForgeTextLayout.Button(new Rect(controlX,y,controlWidth,48),ForgeCodePreferences.ForgeCode?"ON · 켜짐":"OFF · 꺼짐",SettingsButton))ForgeCodePreferences.SetForgeCode(!ForgeCodePreferences.ForgeCode);
        ForgeTextLayout.Label(new Rect(controlX,y+52,controlWidth,forgeHeight),forgeHelp,settingsRowLabel);y+=72+forgeHeight;
        if(ForgeTextLayout.Button(new Rect(x,y,140,48),"뒤로",SettingsButton)){ForgeAudioSettings.Save();titlePage=0;settingsDropdown=0;}
        if(ForgeTextLayout.Button(new Rect(x+width-220,y,220,48),"해상도 적용",SettingsButton)){
            size=displaySizes[selectedDisplaySize];Screen.SetResolution(size.x,size.y,Screen.fullScreen);
            PlayerPrefs.SetInt(WidthSetting,size.x);PlayerPrefs.SetInt(HeightSetting,size.y);PlayerPrefs.Save();
            settingsMessage=size.x+" × "+size.y+" 설정을 저장했습니다.";
        }
        if(settingsMessage.Length>0)ForgeTextLayout.Label(new Rect(x,y+58,width,48),settingsMessage,settingsText);
        GUI.EndScrollView();
        if(popupOpen)ev.type=savedEvent;
        if(settingsDropdown!=0)DrawSettingsDropdown();
    }
    float DrawVolumeRow(float x,float y,float labelWidth,float controlWidth,string label,float volume)
    {
        float controlX=x+labelWidth;
        ForgeTextLayout.Label(new Rect(x,y,labelWidth-16,48),label,settingsRowLabel);
        ForgeTextLayout.Label(new Rect(controlX+controlWidth-64,y,64,48),Mathf.RoundToInt(volume*100)+"%",settingsRowLabel);
        float next=GUI.HorizontalSlider(new Rect(controlX,y+16,Mathf.Max(1,controlWidth-80),24),volume,0,1,settingsSlider,settingsSliderThumb);
        return Mathf.Round(next*100)/100f;
    }
    void OpenSettingsDropdown(int kind,Rect anchor,int selected)
    {anchor.y-=settingsPageScroll.y;settingsDropdown=kind;settingsDropdownAnchor=anchor;settingsDropdownScroll=new Vector2(0,Mathf.Max(0,selected-2)*44);ClearUIFocus();}
    void DrawSettingsDropdown()
    {
        int count=settingsDropdown==1?languageLabels.Length:settingsDropdown==2?displaySizes.Count:settingsDropdown==4?ForgeCodeColors.PaletteNames.Length:2;
        float wanted=Mathf.Min(264,count*44+8);
        float below=Screen.height-settingsDropdownAnchor.yMax-12;
        bool upward=below<wanted&&settingsDropdownAnchor.y>below;
        float height=Mathf.Max(1,Mathf.Min(wanted,upward?settingsDropdownAnchor.y-12:below));
        Rect popup=new Rect(settingsDropdownAnchor.x,upward?settingsDropdownAnchor.y-height-4:settingsDropdownAnchor.yMax+4,settingsDropdownAnchor.width,height);
        var ev=Event.current;
        if(ev.type==EventType.MouseDown&&!popup.Contains(ev.mousePosition)){settingsDropdown=0;ev.Use();return;}
        Color old=GUI.color;GUI.color=new Color(.16f,.19f,.24f);GUI.DrawTexture(popup,Texture2D.whiteTexture);GUI.color=old;
        Rect viewport=new Rect(popup.x+4,popup.y+4,popup.width-8,Mathf.Max(1,popup.height-8));
        settingsDropdownScroll=GUI.BeginScrollView(viewport,settingsDropdownScroll,new Rect(0,0,Mathf.Max(1,viewport.width-20),count*44));
        for(int i=0;i<count;i++){
            string value=settingsDropdown==1?languageLabels[i]:settingsDropdown==2?displaySizes[i].x+" × "+displaySizes[i].y:settingsDropdown==4?ForgeCodeColors.PaletteNames[i]:i==0?"전체 화면":"창 모드";
            if(ForgeTextLayout.Button(new Rect(0,i*44,Mathf.Max(1,viewport.width-20),42),value,settingsOption)){
                if(settingsDropdown==1){selectedLanguage=i;PlayerPrefs.SetString(LanguageSetting,LanguageCodes[i]);PlayerPrefs.Save();settingsMessage="언어 선택을 저장했습니다. 번역은 아직 적용되지 않습니다.";}
                else if(settingsDropdown==2){selectedDisplaySize=i;settingsMessage="해상도 적용 버튼을 눌러 변경하세요.";}
                else if(settingsDropdown==4){ForgeCodeColors.SetPalette(i);settingsMessage="코드 색상을 적용하고 저장했습니다.";}
                else {Screen.fullScreen=i==0;PlayerPrefs.SetInt("MakeYourItem.Settings.Fullscreen",i==0?1:0);PlayerPrefs.Save();}
                settingsDropdown=0;break;
            }
        }
        GUI.EndScrollView();
        if(popup.Contains(ev.mousePosition)&&(ev.isMouse||ev.type==EventType.ScrollWheel))ev.Use();
    }
}
