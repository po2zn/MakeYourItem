using UnityEngine;

public partial class ForgeMvp
{
    bool showGameMenu,returnToTitleRequested;
    GUIStyle gameMenuTitle,gameMenuBody,gameMenuButton;
    void ToggleGameMenu()
    {
        if(returnToTitleRequested)return;
        showGameMenu=!showGameMenu;
        CommitName();EndWorkspaceDrag();techTree.CancelDrag();guideBook.CancelDrag();
        debugResizeEdge=-1;debugInteracting=false;edge=-1;
        GUIUtility.hotControl=0;GUI.FocusControl(null);SetResizeCursor(0);
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
        float textHeight=gameMenuBody.CalcHeight(new GUIContent(description),width-48);
        float height=210+textHeight;
        Rect panel=new Rect((Screen.width-width)/2,Mathf.Max(12,(Screen.height-height)/2),width,height);
        GUI.color=new Color(.12f,.17f,.24f);GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.color=old;
        GUI.Label(new Rect(panel.x+24,panel.y+20,width-48,42),"게임 메뉴",gameMenuTitle);
        GUI.Label(new Rect(panel.x+24,panel.y+72,width-48,textHeight),description,gameMenuBody);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!returnToTitleRequested;
        if(GUI.Button(new Rect(panel.x+24,panel.y+92+textHeight,width-48,48),"계속하기 · ESC",gameMenuButton))ToggleGameMenu();
        if(GUI.Button(new Rect(panel.x+24,panel.y+152+textHeight,width-48,48),"저장하고 타이틀로",gameMenuButton))RequestTitleReturn();
        GUI.enabled=enabled;
    }
    void RestartTutorialFromDebug()
    {
        if(busy||executionSession!=null||tutorialAnimating)return;
        lesson=new ForgeTutorialLesson();tutorialScroll=Vector2.zero;tutorialSuccessAt=-100;
        showDebugWindow=false;debugInteracting=false;
        OpenTutorial();SaveProgress();
    }
}
