using System.Collections;
using UnityEngine;

public partial class ForgeMvp
{
    ForgeTutorialLesson lesson=new ForgeTutorialLesson();
    bool showTutorial,tutorialAnimating,tutorialHint,tutorialWasPaused;
    string tutorialInput="",tutorialFeedback="";
    Vector2 tutorialScroll;
    float tutorialSuccessAt=-100;
    Vector2 tutorialPreviousOffset;
    float tutorialPreviousZoom;
    float tutorialCameraBlend,tutorialFade,tutorialScreenFade;
    bool tutorialCameraActive;
    Vector3 tutorialCameraFocus;
    GUIStyle tutorialTitleStyle,tutorialBodyStyle,tutorialInputStyle,tutorialButtonStyle,tutorialPrimaryStyle,tutorialCodeStyle,tutorialStockStyle;
    void OpenTutorial()
    {
        if(busy||executionSession!=null||showTutorial)return;
        CommitName();EndWorkspaceDrag();techTree.CancelDrag();guideBook.CancelDrag();
        showProgress=false;showTechTree=false;showGuideBook=false;
        tutorialWasPaused=DayCycle.Paused;DayCycle.Paused=true;
        tutorialPreviousOffset=workspaceOffset;tutorialPreviousZoom=workspaceZoom;
        tutorialInput="";tutorialFeedback="연습용 재료입니다. 실제 골드와 재고는 소비되지 않습니다.";
        tutorialHint=false;showTutorial=true;
        tutorialCameraBlend=0;tutorialFade=0;tutorialScreenFade=0;tutorialCameraActive=false;
        tutorialCameraFocus=craftMotion.TutorialFocus;
        UpdateWorkspaceCamera();
        StartCoroutine(PlayTutorialIntro());
        GUI.FocusControl(null);
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
        showTutorial=false;DayCycle.Paused=tutorialWasPaused;
        workspaceOffset=tutorialPreviousOffset;workspaceZoom=tutorialPreviousZoom;UpdateWorkspaceCamera();
        craftMotion.BeginRun();SaveProgress();GUI.FocusControl(null);
    }
    IEnumerator AnimateTutorial(Evaluation evaluation,int first)
    {
        tutorialAnimating=true;
        for(int i=first;i<evaluation.steps.Count;i++)yield return craftMotion.Play(evaluation.steps[i]);
        tutorialAnimating=false;tutorialSuccessAt=Time.unscaledTime;
        SaveProgress();
    }
    void SubmitTutorial()
    {
        int previous=lesson.Evaluation==null?0:lesson.Evaluation.steps.Count;
        if(!lesson.Submit(tutorialInput,out tutorialFeedback))return;
        tutorialInput="";tutorialHint=false;tutorialSuccessAt=Time.unscaledTime;
        tutorialScroll=Vector2.zero;
        GUI.FocusControl(null);SaveProgress();
        StartCoroutine(AnimateTutorial(lesson.Evaluation,previous));
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
            tutorialInputStyle=new GUIStyle(GUI.skin.textField){font=codeStyle.font,fontSize=20,padding=new RectOffset(16,16,16,16)};
            tutorialButtonStyle=new GUIStyle(GUI.skin.button){font=codeStyle.font,fontSize=18,wordWrap=true,
                padding=new RectOffset(18,18,14,14),margin=new RectOffset(0,0,0,0),alignment=TextAnchor.MiddleCenter};
            tutorialPrimaryStyle=new GUIStyle(tutorialButtonStyle){fontSize=20,fontStyle=FontStyle.Bold};
            tutorialCodeStyle=new GUIStyle(tutorialBodyStyle){richText=false,padding=new RectOffset(12,12,12,12)};
            tutorialStockStyle=new GUIStyle(tutorialBodyStyle){fontSize=16,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(6,6,6,6)};
        }
        float width=Mathf.Min(520,Screen.width-24);
        Rect panel=new Rect(12,12,width,Mathf.Max(100,Screen.height-24));
        Color old=GUI.color;GUI.color=TutorialTint(new Color(.09f,.13f,.19f,.97f));GUI.DrawTexture(panel,Texture2D.whiteTexture);GUI.color=old;
        const float inset=24;
        float contentWidth=Mathf.Max(1,panel.width-inset*2);
        const string footerNote="진행은 자동 저장됩니다. 편하게 쉬어 가세요.";
        float noteHeight=Mathf.Ceil(tutorialBodyStyle.CalcHeight(new GUIContent(footerNote),contentWidth))+4;
        float footerHeight=12+noteHeight+12+54+20;
        // Practice inventory stays visible while the instructions/code scroll.
        const float inventoryHeight=100;
        GUI.Label(new Rect(panel.x+inset,panel.y+16,contentWidth,28),"연습용 재료 · 실제 재고와 별도",tutorialBodyStyle);
        float cellWidth=Mathf.Max(1,(contentWidth-24)/4);
        for(int material=0;material<=3;material++){
            Rect cell=new Rect(panel.x+inset+material*(cellWidth+8),panel.y+48,cellWidth,56);
            GUI.color=TutorialTint(new Color(.17f,.24f,.32f));GUI.DrawTexture(cell,Texture2D.whiteTexture);GUI.color=old;
            string amount=material==0?"무제한":(material==1?lesson.RemainingIron:0)+"개";
            GUI.Label(cell,ForgeMaterialGuide.Find(material).Heading+"\n"+amount,tutorialStockStyle);
        }
        GUILayout.BeginArea(new Rect(panel.x+inset,panel.y+24+inventoryHeight,contentWidth,Mathf.Max(1,panel.height-48-footerHeight-inventoryHeight)));
        tutorialScroll=GUILayout.BeginScrollView(tutorialScroll,false,true);
        GUILayout.Label("첫 검 만들기 · "+Mathf.Min(lesson.Stage+1,8)+" / 8",tutorialTitleStyle);
        GUILayout.Space(8);
        GUILayout.Label("연습 공방 · 실제 재고 소비 없음",tutorialBodyStyle);
        GUILayout.Space(16);
        Rect bar=GUILayoutUtility.GetRect(1,6,GUILayout.ExpandWidth(true));
        GUI.color=TutorialTint(new Color(.25f,.8f,.65f));GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*lesson.Stage/8f,6),Texture2D.whiteTexture);GUI.color=old;
        GUILayout.Space(28);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!tutorialAnimating;
        if(lesson.Complete){
            GUILayout.Label("첫 번째 검을 완성했습니다!",tutorialTitleStyle);
            GUILayout.Space(12);
            GUILayout.Label("직접 작성한 도면입니다. 철 2개로 검날과 손잡이를 만들었습니다. 공방 기록에서 언제든 다시 연습할 수 있습니다.",tutorialBodyStyle);
            GUILayout.Space(20);
            DrawTutorialCode();
            GUILayout.Space(24);
            if(GUILayout.Button("도면 복사",tutorialPrimaryStyle,GUILayout.MinHeight(60)))GUIUtility.systemCopyBuffer=lesson.Code;
            GUILayout.Space(12);
            if(GUILayout.Button("처음부터 다시 연습",tutorialButtonStyle,GUILayout.MinHeight(54))){lesson=new ForgeTutorialLesson();tutorialInput="";tutorialFeedback="재료부터 다시 골라 보세요.";craftMotion.BeginRun();SaveProgress();}
        }else{
            GUILayout.Label(ForgeTutorialLesson.Titles[lesson.Stage],tutorialTitleStyle);
            GUILayout.Space(12);
            GUILayout.Label(ForgeTutorialLesson.Instructions[lesson.Stage],tutorialBodyStyle);
            GUILayout.Space(24);
            if(lesson.Stage==0){
                GUILayout.Label("코드 미리보기 · Material = 1\n철의 번호를 Material이라는 이름에 담습니다.",tutorialBodyStyle);
                GUILayout.Space(20);
                for(int row=0;row<5;row++){
                    GUILayout.BeginHorizontal();
                    for(int column=0;column<2;column++){
                        int id=row*2+column;
                        if(column>0)GUILayout.Space(12);
                        if(GUILayout.Button(ForgeMaterialGuide.Find(id).Heading,tutorialButtonStyle,GUILayout.Width(Mathf.Max(1,(contentWidth-32)/2)),GUILayout.MinHeight(64))&&lesson.SelectMaterial(id,out tutorialFeedback)){tutorialSuccessAt=Time.unscaledTime;SaveProgress();}
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(12);
                }
            }else{
                if(lesson.Code.Length>0){
                    GUILayout.Label("지금까지 작성한 도면",tutorialBodyStyle);
                    GUILayout.Space(8);
                    DrawTutorialCode();
                    GUILayout.Space(16);
                }
                GUILayout.Label(lesson.Stage+"번째 줄을 아래에 이어서 작성하세요",tutorialBodyStyle);
                GUILayout.Space(12);
                Color background=GUI.backgroundColor;
                GUI.backgroundColor=Color.Lerp(Color.white,new Color(.5f,.85f,1f),.25f+.15f*Mathf.Sin(Time.unscaledTime*3));
                GUI.SetNextControlName("TutorialInput");
                tutorialInput=GUILayout.TextField(tutorialInput,512,tutorialInputStyle,GUILayout.MinHeight(64));
                GUI.backgroundColor=background;
                GUILayout.Space(20);
                GUI.backgroundColor=new Color(.55f,.85f,1f);
                if(GUILayout.Button("이 줄 실행하기",tutorialPrimaryStyle,GUILayout.MinHeight(60)))SubmitTutorial();
                GUI.backgroundColor=background;
                GUILayout.Space(12);
                if(GUILayout.Button(tutorialHint?"힌트 숨기기":"막히셨나요? 힌트 보기",tutorialButtonStyle,GUILayout.MinHeight(54)))tutorialHint=!tutorialHint;
                if(tutorialHint&&!lesson.Complete){GUILayout.Space(16);GUILayout.Label(ForgeTutorialLesson.Hints[lesson.Stage],tutorialBodyStyle);}
            }
        }
        GUI.enabled=enabled;
        GUILayout.Space(24);
        if(Time.unscaledTime-tutorialSuccessAt<2)GUI.color=TutorialTint(new Color(.5f,1f,.7f));
        GUILayout.Label(tutorialAnimating?"작업물의 변화를 확인하세요…":tutorialFeedback,tutorialBodyStyle);GUI.color=old;
        GUILayout.Space(12);
        GUILayout.EndScrollView();GUILayout.EndArea();
        // Keep leaving/resuming separate from the exercise and reachable without scrolling.
        Rect footer=new Rect(panel.x+inset,panel.yMax-footerHeight,contentWidth,footerHeight-20);
        GUI.color=TutorialTint(new Color(.3f,.4f,.52f,.6f));GUI.DrawTexture(new Rect(footer.x,footer.y,footer.width,1),Texture2D.whiteTexture);GUI.color=old;
        GUI.Label(new Rect(footer.x,footer.y+12,footer.width,noteHeight),footerNote,tutorialBodyStyle);
        GUI.enabled=enabled&&!tutorialAnimating;
        if(GUI.Button(new Rect(footer.x,footer.y+24+noteHeight,footer.width,54),lesson.Complete?"대장간으로 돌아가기":"여기까지 하고 나중에 계속",tutorialButtonStyle))CloseTutorial();
        GUI.enabled=enabled;
    }
    void DrawTutorialCode()
    {
        string[] lines=lesson.Code.TrimEnd('\r','\n').Split('\n');
        var numbered=new System.Text.StringBuilder();
        for(int i=0;i<lines.Length;i++){if(i>0)numbered.Append('\n');numbered.Append(i+1).Append("  ").Append(lines[i]);}
        GUIContent content=new GUIContent(numbered.ToString());
        Rect rect=GUILayoutUtility.GetRect(content,tutorialCodeStyle,GUILayout.ExpandWidth(true));
        Color previous=GUI.color;GUI.color=TutorialTint(new Color(.06f,.09f,.14f));GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;
        GUI.Label(rect,content,tutorialCodeStyle);
    }
}
