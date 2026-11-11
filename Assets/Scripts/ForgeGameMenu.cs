using UnityEngine;

public partial class ForgeMvp
{
    bool showGameMenu,returnToTitleRequested;
    GUIStyle gameMenuTitle,gameMenuBody,gameMenuButton;
    Vector2 gameMenuScroll;
    void ToggleGameMenu()
    {
        if(returnToTitleRequested)return;
        showGameMenu=!showGameMenu;
        CommitName();EndWorkspaceDrag();CancelHierarchyTreeDrag();guideBook.CancelDrag();
        debugResizeEdge=-1;debugInteracting=false;edge=-1;
        GUIUtility.hotControl=0;ClearUIFocus();SetResizeCursor(0);
    }
    void RequestTitleReturn()
    {
        returnToTitleRequested=true;
        if(!busy&&!tutorialAnimating)FinishTitleReturn();
    }
    void FinishTitleReturn()
    {
        returnToTitleRequested=false;
        StopExecution(false);
        StopSmithJobs();
        if(showTutorial)CloseTutorial();
        ReturnToTitle();
        if(showTitle)showGameMenu=false;
    }
    void DrawGameMenu()
    {
        if(gameMenuTitle==null){
            gameMenuTitle=new GUIStyle(GUI.skin.label){font=codeStyle.font,fontSize=28,wordWrap=true};
            gameMenuTitle.normal.textColor=Color.white;
            gameMenuBody=new GUIStyle(gameMenuTitle){fontSize=18};
            gameMenuButton=new GUIStyle(GUI.skin.button){font=codeStyle.font,fontSize=20,wordWrap=true,padding=new RectOffset(16,16,14,14)};
        }
        Color old=GUI.color;GUI.color=new Color(0,0,0,.72f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
        float width=Mathf.Min(460,Screen.width-24);
        string description=returnToTitleRequested?"진행 중인 작업을 마무리하고 저장하는 중입니다…":"진행 상황을 저장하고 타이틀로 이동할 수 있습니다. 실행 중인 도면은 현재 작업이 끝난 뒤 중단됩니다.";
        if(saveMessage.StartsWith("저장 실패"))description=saveMessage;
        float textHeight=ForgeTextLayout.Height(description,gameMenuBody,width-48);
        float height=Mathf.Min(Screen.height-24,250+textHeight);
        Rect panel=new Rect((Screen.width-width)/2,Mathf.Max(12,(Screen.height-height)/2),width,height);
        GUI.color=new Color(.12f,.17f,.24f);GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.color=old;
        GUILayout.BeginArea(new Rect(panel.x+24,panel.y+16,width-48,Mathf.Max(1,height-32)));
        gameMenuScroll=GUILayout.BeginScrollView(gameMenuScroll);
        ForgeTextLayout.LayoutLabel("게임 메뉴",gameMenuTitle);GUILayout.Space(12);
        ForgeTextLayout.LayoutLabel(description,gameMenuBody);GUILayout.Space(16);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!returnToTitleRequested;
        if(GUILayout.Button("계속하기 · ESC",gameMenuButton,GUILayout.MinHeight(48)))ToggleGameMenu();
        GUILayout.Space(10);
        if(GUILayout.Button("저장하고 타이틀로",gameMenuButton,GUILayout.MinHeight(48)))RequestTitleReturn();
        GUILayout.EndScrollView();GUILayout.EndArea();
        GUI.enabled=enabled;
    }
    void RestartTutorialFromDebug()
    {
        if(busy||executionSession!=null||tutorialAnimating)return;
        lesson=new ForgeTutorialLesson();tutorialScroll=Vector2.zero;tutorialSuccessAt=-100;
        debugInteracting=false;
        OpenTutorial();SaveProgress();
    }
}
