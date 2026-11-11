using System.Collections;
using UnityEngine;

public partial class ForgeMvp
{
    ForgeTutorialLesson lesson=new ForgeTutorialLesson();
    bool showTutorial,tutorialAnimating,tutorialHint,tutorialWasPaused;
    bool showTutorialWelcome;
    Vector2 tutorialWelcomeScroll;
    string tutorialInput="",tutorialFeedback="";
    Vector2 tutorialScroll;
    Vector2 tutorialEditorScroll;
    readonly CodePaintCache tutorialPaintCache=new CodePaintCache();
    int tutorialPendingCaret=-1;
    float tutorialClickedAt=-100;
    string tutorialColoredSource;
    System.Collections.Generic.List<ForgeSyntax.Token> tutorialColoredTokens;
    readonly ForgeWindowFrame tutorialWindowFrame=new ForgeWindowFrame(new ForgeWindowRules(false,ForgeWindowEdges.Horizontal,340,220));
    readonly ForgeWindowInteraction tutorialWindowInteraction=new ForgeWindowInteraction();
    float tutorialEditorWidth=-1;
    Vector2 tutorialWindowScreenSize;
    float tutorialSuccessAt=-100;
    Vector2 tutorialPreviousOffset;
    float tutorialPreviousZoom;
    float tutorialCameraBlend,tutorialFade,tutorialScreenFade;
    bool tutorialCameraActive;
    Vector3 tutorialCameraFocus;
    GUIStyle tutorialTitleStyle,tutorialBodyStyle,tutorialButtonStyle,tutorialPrimaryStyle,tutorialStockStyle;
    float TutorialPanelWidth => Mathf.Max(1,Mathf.Min(460,(Screen.width-36)*.48f));
    void OpenTutorial()
    {
        if(busy||executionSession!=null||showTutorial)return;
        CommitName();EndWorkspaceDrag();CancelHierarchyTreeDrag();guideBook.CancelDrag();
        showProgress=false;showTechTree=false;
        tutorialWasPaused=DayCycle.Paused;DayCycle.Paused=true;
        tutorialPreviousOffset=workspaceOffset;tutorialPreviousZoom=workspaceZoom;
        tutorialInput=lesson.Code;tutorialEditorScroll=Vector2.zero;tutorialPendingCaret=-1;tutorialSuccessAt=-100;
        tutorialFeedback="연습용 재료입니다. 실제 골드와 재고는 소비되지 않습니다.";
        tutorialHint=false;showTutorial=true;
        showTutorialWelcome=lesson.Stage==0;tutorialWelcomeScroll=Vector2.zero;
        tutorialCameraBlend=0;tutorialFade=0;tutorialScreenFade=0;tutorialCameraActive=false;
        tutorialCameraFocus=craftMotion.TutorialFocus;
        UpdateWorkspaceCamera();
        StartCoroutine(PlayTutorialIntro());
        ClearUIFocus();
    }
    IEnumerator PlayTutorialIntro()
    {
        tutorialAnimating=true;
        const float screenFadeSeconds=.4f,zoomSeconds=1.8f,fadeSeconds=.35f;
        // Hide the camera reset behind black, then reveal the incoming zoom.
        for(float elapsed=0;elapsed<screenFadeSeconds;elapsed+=Time.unscaledDeltaTime){
            tutorialScreenFade=Mathf.SmoothStep(0,1,elapsed/screenFadeSeconds);
            yield return null;
        }
        tutorialScreenFade=1;
        yield return null;
        craftMotion.BeginRun();tutorialCameraActive=true;
        UpdateWorkspaceCamera();
        for(float elapsed=0;elapsed<zoomSeconds;elapsed+=Time.unscaledDeltaTime){
            tutorialCameraBlend=Mathf.SmoothStep(0,1,elapsed/zoomSeconds);
            tutorialScreenFade=1-Mathf.SmoothStep(0,1,elapsed/screenFadeSeconds);
            yield return null;
        }
        tutorialCameraBlend=1;tutorialScreenFade=0;
        for(float elapsed=0;elapsed<fadeSeconds;elapsed+=Time.unscaledDeltaTime){
            tutorialFade=Mathf.SmoothStep(0,1,elapsed/fadeSeconds);
            yield return null;
        }
        tutorialFade=1;tutorialAnimating=false;
        if(returnToTitleRequested){FinishTitleReturn();yield break;}
        if(lesson.Evaluation!=null)yield return AnimateTutorial(lesson.Evaluation,0);
    }
    IEnumerator RevealTutorialLesson()
    {
        showTutorialWelcome=false;tutorialScroll=Vector2.zero;tutorialFade=0;tutorialAnimating=true;
        for(float elapsed=0;elapsed<.35f;elapsed+=Time.unscaledDeltaTime){tutorialFade=Mathf.SmoothStep(0,1,elapsed/.35f);yield return null;}
        tutorialFade=1;tutorialAnimating=false;
        SaveProgress();
    }
    void DrawTutorialWelcome()
    {
        SetResizeCursor(0);
        float width=Mathf.Max(1,Mathf.Min(520,Screen.width-24));
        Rect panel=new Rect(12,24,width,Mathf.Max(160,Screen.height-48));
        Color old=GUI.color;GUI.color=TutorialTint(new Color(.09f,.13f,.19f,.97f));GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.color=old;
        GUILayout.BeginArea(new Rect(panel.x+24,panel.y+24,width-48,Mathf.Max(1,panel.height-196)));
        tutorialWelcomeScroll=GUILayout.BeginScrollView(tutorialWelcomeScroll,false,false,GUIStyle.none,GUIStyle.none);
        ForgeTextLayout.LayoutLabel("나만의 대장간에 오신 것을 환영합니다",tutorialTitleStyle);
        GUILayout.Space(20);
        ForgeTextLayout.LayoutLabel("이곳에서는 여러분이 쓴 코드가 대장장이의 작업 지시가 됩니다. 재료를 고르고, 불로 달구고, 망치로 두드리며 나만의 검을 만들어 보세요.",tutorialBodyStyle);
        GUILayout.Space(28);
        ForgeTextLayout.LayoutLabel("코드가 도면이 됩니다",tutorialTitleStyle);
        GUILayout.Space(10);
        ForgeTextLayout.LayoutLabel("한 줄의 명령을 실행하면 대장장이가 직접 움직입니다. 작업물의 변화를 보면서 제작 순서를 익힐 수 있습니다. 코드를 처음 접해도 한 단계씩 따라오면 됩니다.",tutorialBodyStyle);
        GUILayout.Space(28);
        ForgeTextLayout.LayoutLabel("만들고, 팔고, 더 넓혀 보세요",tutorialTitleStyle);
        GUILayout.Space(10);
        ForgeTextLayout.LayoutLabel("완성품을 판매해 골드를 모으고, 테크 트리에서 새로운 재료와 기능을 구매하세요. 다양한 재료와 공정을 조합하며 더 가치 있는 작품을 만드는 것이 이 공방의 목표입니다.",tutorialBodyStyle);
        GUILayout.Space(28);
        ForgeTextLayout.LayoutLabel("첫 목표는 검 한 자루",tutorialTitleStyle);
        GUILayout.Space(10);
        ForgeTextLayout.LayoutLabel("튜토리얼에서는 재료와 변수부터 시작해 검날과 손잡이를 직접 만듭니다. 연습용 재료를 사용하므로 실제 골드나 재고가 줄지 않습니다. 틀려도 다시 시도하거나 이전 단계로 돌아갈 수 있습니다.",tutorialBodyStyle);
        GUILayout.Space(12);
        GUILayout.EndScrollView();GUILayout.EndArea();
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!tutorialAnimating;
        if(ForgeTextLayout.Button(new Rect(panel.x+24,panel.yMax-148,width-48,60),"튜토리얼 시작 · 첫 검 만들기",tutorialPrimaryStyle))StartCoroutine(RevealTutorialLesson());
        if(ForgeTextLayout.Button(new Rect(panel.x+24,panel.yMax-76,width-48,52),"나중에 시작하기",tutorialButtonStyle))CloseTutorial();
        GUI.enabled=enabled;
    }
    void UpdateTutorialCamera()
    {
        if(!tutorialCameraActive)return;
        float size=Mathf.Lerp(9f,3.8f,tutorialCameraBlend);
        workspaceCamera.orthographicSize=size;
        float panelWidth=Mathf.Min(520,Screen.width-24)+24;
        float destinationX=Mathf.Clamp((panelWidth+(Screen.width-panelWidth)*.5f)/Mathf.Max(1,Screen.width),.5f,.82f);
        float screenX=Mathf.Lerp(.5f,destinationX,tutorialCameraBlend);
        var cameraTransform=workspaceCamera.transform;
        float distance=Mathf.Max(1,Vector3.Dot(tutorialCameraFocus-cameraOrigin,cameraTransform.forward));
        cameraTransform.position=tutorialCameraFocus-cameraTransform.forward*distance
            -cameraTransform.right*((screenX-.5f)*2*size*workspaceCamera.aspect);
    }
    Color TutorialTint(Color color){color.a*=tutorialFade;return color;}
    void CloseTutorial()
    {
        if(tutorialAnimating)return;
        tutorialWindowInteraction.Cancel();SetResizeCursor(0);
        showTutorial=false;DayCycle.Paused=tutorialWasPaused;
        workspaceOffset=tutorialPreviousOffset;workspaceZoom=tutorialPreviousZoom;UpdateWorkspaceCamera();
        craftMotion.BeginRun();SaveProgress();ClearUIFocus();
    }
    IEnumerator AnimateTutorial(Evaluation evaluation,int first)
    {
        tutorialAnimating=true;
        for(int i=first;i<evaluation.steps.Count;i++)yield return craftMotion.Play(evaluation.steps[i]);
        tutorialAnimating=false;
        SaveProgress();
    }
    void SubmitTutorial()
    {
        int previous=lesson.Evaluation==null?0:lesson.Evaluation.steps.Count;
        tutorialSuccessAt=-100;
        if(!lesson.SubmitDocument(tutorialInput,out tutorialFeedback))return;
        tutorialInput=lesson.Code;tutorialHint=false;tutorialSuccessAt=Time.unscaledTime;
        tutorialScroll=Vector2.zero;
        ClearUIFocus();SaveProgress();
        StartCoroutine(AnimateTutorial(lesson.Evaluation,previous));
    }
    void PreviousTutorial()
    {
        if(tutorialAnimating||lesson.Stage==0)return;
        if(!lesson.Previous())return;
        tutorialInput=lesson.Code;tutorialEditorScroll=Vector2.zero;tutorialPendingCaret=-1;
        tutorialHint=false;tutorialScroll=Vector2.zero;tutorialSuccessAt=-100;
        tutorialFeedback="이전 단계로 돌아왔습니다. 코드를 확인하고 다시 실행해 보세요.";
        craftMotion.BeginRun();SaveProgress();ClearUIFocus();
        if(lesson.Evaluation!=null)StartCoroutine(AnimateTutorial(lesson.Evaluation,0));
    }
    float DrawTutorialMaterials()
    {
        float width=Mathf.Min(520,Screen.width-24);
        int columns=width>=440?4:2;
        float cellWidth=(width-(columns-1)*8)/columns;
        ForgeTextLayout.Label(new Rect(12,12,width,28),"연습용 재료 · 실제 재고와 별도",tutorialBodyStyle);
        for(int id=0;id<4;id++){
            Rect cell=new Rect(12+(id%columns)*(cellWidth+8),44+(id/columns)*72,cellWidth,64);
            materialHud.DrawIcon(new Rect(cell.x,cell.y+8,48,48),id);
            string amount=id==0?"무제한":(id==1?lesson.RemainingIron:0)+"개";
            ForgeTextLayout.Label(new Rect(cell.x+50,cell.y,cell.width-50,64),ForgeMaterialGuide.Find(id).Heading+"\n"+amount,tutorialStockStyle);
        }
        return 44+((4+columns-1)/columns)*72+12;
    }
    void DrawTutorial()
    {
        Color old=GUI.color;bool enabled=GUI.enabled;
        GUI.color=TutorialTint(old);GUI.enabled=enabled&&tutorialFade>=1;
        try{
            DrawTutorialContents();
            if(tutorialScreenFade>0){
                GUI.color=new Color(0,0,0,tutorialScreenFade);
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            }
        }
        finally{GUI.color=old;GUI.enabled=enabled;}
    }
    void DrawTutorialContents()
    {
        if(tutorialTitleStyle==null){
            tutorialTitleStyle=new GUIStyle(GUI.skin.label){font=codeStyle.font,fontSize=25,wordWrap=true};tutorialTitleStyle.normal.textColor=Color.white;
            tutorialBodyStyle=new GUIStyle(tutorialTitleStyle){fontSize=18};
            tutorialButtonStyle=new GUIStyle(GUI.skin.button){font=codeStyle.font,fontSize=18,wordWrap=true,
                padding=new RectOffset(18,18,14,14),margin=new RectOffset(0,0,0,0),alignment=TextAnchor.MiddleCenter};
            tutorialPrimaryStyle=new GUIStyle(tutorialButtonStyle){fontSize=20,fontStyle=FontStyle.Bold};
            tutorialStockStyle=new GUIStyle(tutorialBodyStyle){fontSize=16,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(0,0,0,0)};
        }
        if(showTutorialWelcome){DrawTutorialWelcome();return;}
        float width=TutorialPanelWidth;
        float panelTop=DrawTutorialMaterials();
        Rect panel=new Rect(12,panelTop,width,Mathf.Max(100,Screen.height-panelTop-12));
        Color old=GUI.color;GUI.color=TutorialTint(new Color(.09f,.13f,.19f,.97f));GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.color=old;
        const float inset=24;
        float contentWidth=Mathf.Max(1,panel.width-inset*2);
        const string footerNote="진행은 자동 저장됩니다. 편하게 쉬어 가세요.";
        float noteHeight=ForgeTextLayout.Height(footerNote,tutorialBodyStyle,contentWidth);
        float footerHeight=12+noteHeight+12+54+12+54+20;
        GUILayout.BeginArea(new Rect(panel.x+inset,panel.y+24,contentWidth,Mathf.Max(1,panel.height-48-footerHeight)));
        // Keep wheel scrolling without the oversized, always-visible drag bar.
        tutorialScroll=GUILayout.BeginScrollView(tutorialScroll,false,false,GUIStyle.none,GUIStyle.none);
        ForgeTextLayout.LayoutLabel("첫 검 만들기 · "+Mathf.Min(lesson.Stage+1,8)+" / 8",tutorialTitleStyle);
        GUILayout.Space(8);
        ForgeTextLayout.LayoutLabel("연습 공방 · 실제 재고 소비 없음",tutorialBodyStyle);
        GUILayout.Space(16);
        Rect bar=GUILayoutUtility.GetRect(1,6,GUILayout.ExpandWidth(true));
        GUI.color=TutorialTint(new Color(.25f,.8f,.65f));GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*lesson.Stage/8f,6),Texture2D.whiteTexture);GUI.color=old;
        GUILayout.Space(28);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!tutorialAnimating;
        if(lesson.Complete){
            ForgeTextLayout.LayoutLabel("첫 번째 검을 완성했습니다!",tutorialTitleStyle);
            GUILayout.Space(12);
            ForgeTextLayout.LayoutLabel("직접 작성한 도면입니다. 철 2개로 검날과 손잡이를 만들었습니다. 공방 기록에서 언제든 다시 연습할 수 있습니다.",tutorialBodyStyle);
            GUILayout.Space(20);
            GUILayout.Space(24);
            if(GUILayout.Button("도면 복사",tutorialPrimaryStyle,GUILayout.MinHeight(60)))GUIUtility.systemCopyBuffer=lesson.Code;
            GUILayout.Space(12);
            if(GUILayout.Button("처음부터 다시 연습",tutorialButtonStyle,GUILayout.MinHeight(54))){lesson=new ForgeTutorialLesson();tutorialInput="";tutorialSuccessAt=-100;showTutorialWelcome=true;tutorialWelcomeScroll=Vector2.zero;tutorialFeedback="재료부터 다시 골라 보세요.";craftMotion.BeginRun();SaveProgress();}
        }else{
            ForgeTextLayout.LayoutLabel(ForgeTutorialLesson.Titles[lesson.Stage],tutorialTitleStyle);
            GUILayout.Space(12);
            ForgeTextLayout.LayoutLabel(ForgeTutorialLesson.Instructions[lesson.Stage],tutorialBodyStyle);
            GUILayout.Space(24);
            if(lesson.Stage==0){
                ForgeTextLayout.LayoutLabel("코드 미리보기 · Material = 1\n철의 번호를 Material이라는 이름에 담습니다.",tutorialBodyStyle);
                GUILayout.Space(20);
                for(int row=0;row<5;row++){
                    GUILayout.BeginHorizontal();
                    for(int column=0;column<2;column++){
                        int id=row*2+column;
                        if(column>0)GUILayout.Space(12);
                        if(GUILayout.Button(ForgeMaterialGuide.Find(id).Heading,tutorialButtonStyle,GUILayout.Width(Mathf.Max(1,(contentWidth-32)/2)),GUILayout.MinHeight(64))){tutorialSuccessAt=-100;if(lesson.SelectMaterial(id,out tutorialFeedback)){tutorialSuccessAt=Time.unscaledTime;SaveProgress();}}
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(12);
                }
            }else{
                ForgeTextLayout.LayoutLabel("오른쪽 코드창에서 "+lesson.Stage+"번째 줄을 이어서 작성하세요.",tutorialBodyStyle);
                GUILayout.Space(12);
                if(GUILayout.Button(tutorialHint?"힌트 숨기기":"막히셨나요? 힌트 보기",tutorialButtonStyle,GUILayout.MinHeight(54)))tutorialHint=!tutorialHint;
                if(tutorialHint&&!lesson.Complete){GUILayout.Space(16);ForgeTextLayout.LayoutLabel(ForgeTutorialLesson.Hints[lesson.Stage],tutorialBodyStyle);}
            }
        }
        GUI.enabled=enabled;
        GUILayout.Space(24);
        if(tutorialSuccessAt>=0)GUI.color=TutorialTint(new Color(.5f,1f,.7f));
        ForgeTextLayout.LayoutLabel(tutorialAnimating?"작업물의 변화를 확인하세요…":tutorialFeedback,tutorialBodyStyle);GUI.color=old;
        GUILayout.Space(12);
        GUILayout.EndScrollView();GUILayout.EndArea();
        // Keep leaving/resuming separate from the exercise and reachable without scrolling.
        Rect footer=new Rect(panel.x+inset,panel.yMax-footerHeight,contentWidth,footerHeight-20);
        GUI.color=TutorialTint(new Color(.3f,.4f,.52f,.6f));GUI.DrawTexture(new Rect(footer.x,footer.y,footer.width,1),Texture2D.whiteTexture);GUI.color=old;
        ForgeTextLayout.Label(new Rect(footer.x,footer.y+12,footer.width,noteHeight),footerNote,tutorialBodyStyle);
        GUI.enabled=enabled&&!tutorialAnimating&&lesson.Stage>0;
        if(ForgeTextLayout.Button(new Rect(footer.x,footer.y+24+noteHeight,footer.width,54),"← 이전 단계 다시 배우기",tutorialButtonStyle))PreviousTutorial();
        GUI.enabled=enabled&&!tutorialAnimating;
        if(ForgeTextLayout.Button(new Rect(footer.x,footer.y+90+noteHeight,footer.width,54),lesson.Complete?"대장간으로 돌아가기":"여기까지 하고 나중에 계속",tutorialButtonStyle))CloseTutorial();
        GUI.enabled=enabled;
        DrawTutorialEditor(panelTop);
    }
    void DrawTutorialEditor(float top)
    {
        float left=TutorialPanelWidth+24;
        float availableWidth=Mathf.Max(1,Screen.width-left-12);
        float minimumWidth=Mathf.Min(340,availableWidth);
        if(tutorialEditorWidth<0)tutorialEditorWidth=Mathf.Min(600,availableWidth);
        float width=Mathf.Clamp(tutorialEditorWidth,minimumWidth,availableWidth);
        float height=Mathf.Max(220,Mathf.Min(400,Screen.height-top-12));
        Vector2 screenSize=new Vector2(Screen.width,Screen.height);
        if(screenSize!=tutorialWindowScreenSize){tutorialWindowInteraction.Cancel();tutorialWindowScreenSize=screenSize;}
        tutorialWindowFrame.Rules=new ForgeWindowRules(false,ForgeWindowEdges.Horizontal,minimumWidth,height){Maximum=new Vector2(availableWidth,height)};
        tutorialWindowFrame.Bounds=new Rect(left,top,width,height);
        bool resizing=tutorialWindowInteraction.Active;
        tutorialWindowInteraction.Handle(tutorialWindowFrame,"TutorialCodeWindow".GetHashCode(),new Rect(),GUI.enabled);
        Rect window=tutorialWindowFrame.Bounds;
        if(resizing||tutorialWindowInteraction.Active)tutorialEditorWidth=window.width;
        width=window.width;
        SetResizeCursor(GUI.enabled?tutorialWindowInteraction.Cursor(tutorialWindowFrame,Event.current.mousePosition):0);
        Color color=GUI.color;bool enabled=GUI.enabled;
        if(Event.current.type==EventType.MouseDown&&window.Contains(Event.current.mousePosition))tutorialClickedAt=Time.realtimeSinceStartup;
        DrawCodeSurface(window,tutorialClickedAt,tutorialFade);
        GUI.color=TutorialTint(new Color(.4f,.65f,.8f,.6f));
        GUI.DrawTexture(new Rect(window.x+1,window.center.y-18,2,36),Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(window.xMax-3,window.center.y-18,2,36),Texture2D.whiteTexture);GUI.color=color;
        ForgeTextLayout.Label(new Rect(window.x+16,window.y+12,width-32,38),"튜토리얼 코드창",tutorialTitleStyle);
        string caption=lesson.Stage==0?"왼쪽에서 철을 선택하면 코드를 작성할 수 있습니다.":lesson.Complete?"완성한 도면입니다.":"이전 코드 아래에 이번 단계의 한 줄을 추가하세요.";
        float captionHeight=ForgeTextLayout.Height(caption,tutorialBodyStyle,Mathf.Max(1,width-32));
        ForgeTextLayout.Label(new Rect(window.x+16,window.y+54,width-32,captionHeight),caption,tutorialBodyStyle);
        Rect view=new Rect(window.x+40,window.y+66+captionHeight,width-52,Mathf.Max(40,height-150-captionHeight));
        GUI.color=TutorialTint(new Color(.48f,.52f,.58f,.4f));
        GUI.DrawTexture(new Rect(view.x-4,view.y+2,1,view.height-4),Texture2D.whiteTexture);GUI.color=color;
        GUI.enabled=enabled&&!tutorialAnimating&&lesson.Stage>0&&!lesson.Complete;
        Vector2 textSize=MeasureCode(tutorialInput);
        Rect content=new Rect(0,0,Mathf.Max(view.width,textSize.x),Mathf.Max(view.height,textSize.y));
        if(tutorialPendingCaret>=0){FollowEditorCaret(content,view,tutorialInput,ref tutorialEditorScroll,tutorialPendingCaret);tutorialPendingCaret=-1;}
        var ev=Event.current;
        bool followCaret=GUI.GetNameOfFocusedControl()=="TutorialInput"&&ev.type==EventType.KeyDown;
        string previousInput=tutorialInput;
        tutorialEditorScroll=GUI.BeginScrollView(view,tutorialEditorScroll,content,false,false,GUIStyle.none,GUIStyle.none);
        GUI.SetNextControlName("TutorialInput");
        // Use the gameplay editor's native selection/caret and shared syntax layer.
        tutorialInput=GUI.TextArea(content,tutorialInput,4096,inputStyle);
        if(tutorialColoredSource!=tutorialInput||tutorialColoredTokens==null){tutorialColoredSource=tutorialInput;tutorialColoredTokens=ForgeSyntax.Scan(tutorialInput);}
        DrawSyntaxTokens(content,tutorialInput,tutorialColoredTokens,codeStyle,tokenStyle,new Rect(tutorialEditorScroll,view.size),tutorialPaintCache);
        GUI.EndScrollView();
        if(tutorialInput!=previousInput){
            textSize=MeasureCode(tutorialInput);
            content.width=Mathf.Max(view.width,textSize.x);content.height=Mathf.Max(view.height,textSize.y);
            followCaret=true;
        }
        if(followCaret&&GUI.GetNameOfFocusedControl()=="TutorialInput"){
            var editor=(TextEditor)GUIUtility.GetStateObject(typeof(TextEditor),GUIUtility.keyboardControl);
            tutorialPendingCaret=Mathf.Clamp(editor.cursorIndex,0,tutorialInput.Length);
            FollowEditorCaret(content,view,tutorialInput,ref tutorialEditorScroll,tutorialPendingCaret);
        }
        DrawEditorScrollbars(view,content,ref tutorialEditorScroll);
        Color background=GUI.backgroundColor;GUI.backgroundColor=new Color(.55f,.85f,1f);
        if(ForgeTextLayout.Button(new Rect(window.x+16,window.yMax-68,width-32,52),"이번 단계 실행",tutorialPrimaryStyle))SubmitTutorial();
        GUI.backgroundColor=background;GUI.enabled=enabled;
    }
}
