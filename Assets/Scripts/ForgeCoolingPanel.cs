using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Reuses the authored right-hand Debug window, scroll layout, font, fold and drag handles.
public sealed class ForgeCoolingPanel : MonoBehaviour {
    ForgeMvp game;ForgeGameUI ui;GameObject window;Button entry,template,runControl,stepControl;
    RectTransform content,diagramRect;Text templateText,status,latest,inspection,bindings,log,practiceLocation;
    ForgeCoolingDiagram diagram;InputField lineInput,practiceCode;
    GameObject choices,practiceTools;string choiceMode,selectedVariable,distance,previous,visited;
    bool open,showLog,observing,paused;int screenWidth,screenHeight;
    ForgeCoolingPractice practice;readonly ForgeCoolingViewState practiceView=new ForgeCoolingViewState();Coroutine runner;
    string message="";string namesStamp="";float nextRefresh;float zoom=1;
    BlueprintObservation Observer=>practice?.Observation??game.CoolingObservation;
    ForgeCoolingViewState View=>practice!=null?practiceView:game.CoolingView;
    public static ForgeCoolingPanel Create(ForgeMvp game,ForgeGameUI ui){var existing=ui.GetComponent<ForgeCoolingPanel>();if(existing)return existing;var p=ui.gameObject.AddComponent<ForgeCoolingPanel>();p.game=game;p.ui=ui;p.Build();return p;}
    Transform Named(string name)=>window.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    Button Button(string label,UnityEngine.Events.UnityAction action,Transform parent=null){var b=Instantiate(template,parent?parent:content);b.name=label;b.gameObject.SetActive(true);b.onClick=new Button.ButtonClickedEvent();b.onClick.AddListener(action);foreach(var a in b.GetComponents<ForgeUIAction>())Destroy(a);var t=b.GetComponentInChildren<Text>();t.text=label;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;var l=b.GetComponent<LayoutElement>()??b.gameObject.AddComponent<LayoutElement>();l.minHeight=36;l.preferredHeight=42;var hint=b.GetComponent<ForgeUIHover>();if(hint){hint.description=label;hint.Bind(ui);}return b;}
    Text Text(string name,string value,Transform parent=null){var t=Instantiate(templateText,parent?parent:content);t.name=name;t.text=value;t.gameObject.SetActive(true);t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.fontSize=16;return t;}
    static RectTransform Rect(string name,Transform parent){var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);return (RectTransform)o.transform;}
    InputField Input(string name,string value,int height,Transform parent=null){var r=Rect(name,parent?parent:content);r.gameObject.AddComponent<Image>().color=new Color(.96f,.97f,1);r.gameObject.AddComponent<LayoutElement>().preferredHeight=height;var field=r.gameObject.AddComponent<InputField>();var t=Text(name+" 글씨",value,r);var fit=t.GetComponent<ContentSizeFitter>();if(fit)Destroy(fit);t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=new Vector2(8,8);t.rectTransform.offsetMax=new Vector2(-8,-8);t.supportRichText=false;t.verticalOverflow=VerticalWrapMode.Truncate;field.textComponent=t;field.text=value;field.characterLimit=32768;return field;}
    GameObject Group(string name){var r=Rect(name,content);var layout=r.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=6;layout.childControlHeight=layout.childControlWidth=true;layout.childForceExpandHeight=false;return r.gameObject;}
    void Build(){
        template=ui.Node("ClearDebug").GetComponent<Button>();templateText=ui.Node("DebugText").GetComponent<Text>();
        window=Instantiate(ui.Node("Debug").gameObject,ui.Node("Debug").parent);window.name="CoolingBlueprint";
        foreach(var a in window.GetComponentsInChildren<ForgeUIAction>(true))Destroy(a);
        Named("DebugTitle").GetComponent<Text>().text="냉각·용뼈 도면";
        var close=Named("DebugClose").GetComponent<Button>();close.onClick=new Button.ButtonClickedEvent();close.onClick.AddListener(Close);
        content=(RectTransform)Named("DebugScrollContent");foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        status=Text("CoolingStatus","");latest=Text("CoolingLatest","");
        var graphViewport=Rect("냉각 도면 이동 영역",content);graphViewport.gameObject.AddComponent<LayoutElement>().preferredHeight=400;graphViewport.gameObject.AddComponent<Image>().color=new Color(.9f,.94f,.98f);graphViewport.gameObject.AddComponent<RectMask2D>();var pan=graphViewport.gameObject.AddComponent<ScrollRect>();pan.viewport=graphViewport;pan.horizontal=pan.vertical=true;pan.movementType=ScrollRect.MovementType.Clamped;
        diagramRect=Rect("냉각 그래프",graphViewport);diagramRect.anchorMin=diagramRect.anchorMax=new Vector2(.5f,.5f);diagramRect.pivot=new Vector2(.5f,.5f);diagramRect.sizeDelta=new Vector2(660,680);diagram=diagramRect.gameObject.AddComponent<ForgeCoolingDiagram>();diagram.raycastTarget=false;pan.content=diagramRect;
        Text("CoolingLegend","원을 둘러싼 굵은 테두리 = 조회 지점 · 굵은 선 = 조회 통로\n화살표 = 제출 경로 · 푸른 빛 = 실제 냉각 진행\n서리수 N개 = 실제 통로 소모량. 누적/이전/방문은 코드가 기록한 값이며 자동 정답이 아닙니다.\n용뼈: 굵은 구간 = 이번 절단 조각 · 세로선 = 고정 마디 · 완료 표시는 절단 후 갱신됩니다.\n도면을 드래그하여 이동하세요.");
        Button("도면 확대",()=>Zoom(.2f));Button("도면 축소",()=>Zoom(-.2f));
        runControl=Button("실행 / 계속",()=>Run(false));Button("일시 정지 · 현재 문장/공정 후",()=>{paused=true;if(practice==null)game.CoolingPause();});stepControl=Button("한 단계 실행",()=>Run(true));Button("실행 중단",Stop);
        lineInput=Input("중단점 줄 번호","1",42);lineInput.contentType=InputField.ContentType.IntegerNumber;Button("입력한 줄 중단점 설정 / 해제",()=>{if(!int.TryParse(lineInput.text,out int line)||line<1){message="양의 줄 번호를 입력하세요.";return;}message=practice!=null?(practice.Breakpoints.Toggle(null,line)?"연습 중단점 설정: ":"연습 중단점 해제: ")+line:game.CoolingBreakpoint(line);});
        Button("선택 재료 복제로 연습",StartPractice);Button("연습 초기화 · 같은 도면",ResetPractice);Button("실제 제작 보기로 돌아가기",()=>{EndPractice();Refresh();});
        practiceTools=Group("냉각 연습 도면");practiceLocation=Text("연습 실행 위치","",practiceTools.transform);practiceCode=Input("연습 코드","",240,practiceTools.transform);practiceCode.lineType=InputField.LineType.MultiLineNewline;practiceCode.onValueChanged.AddListener(_=>{if(practice!=null&&practice.Session!=null){CancelRunner();practice.Reset();practiceView.Select(practice.Blade);message="코드 변경으로 연습을 초기화했습니다.";}});practiceTools.SetActive(false);
        Button("코드 관찰 켜기 / 끄기",()=>{observing=!observing;Observer.Enabled=observing;});Button("현재 범위의 재료 변수 선택",()=>Pick("blade"));Button("관찰 변수 선택",()=>Pick("variable"));inspection=Text("변수 값","");
        Text("데이터 연결 안내","코드 데이터 연결 · 배열 인덱스 = 지점 번호\n방문 값은 ‘코드의 방문 표시’이며 최적 비용을 보증하지 않습니다.");
        Button("누적 비용 배열 선택",()=>Pick("distance"));Button("이전 지점 배열 선택",()=>Pick("previous"));Button("코드의 방문 표시 배열 선택",()=>Pick("visited"));choices=Group("실제 변수 목록");choices.SetActive(false);bindings=Text("배열 연결 상태","");
        Button("실행 기록 펼치기 / 접기 · 최근 80개",()=>{showLog=!showLog;});log=Text("냉각 실행 기록","");
        ForgeLegacyUIFont.Apply(window.transform);
        entry=Instantiate(ui.Node("DebugToggle").GetComponent<Button>(),ui.Node("DebugToggle").parent);entry.name="CoolingBlueprintToggle";entry.onClick=new Button.ButtonClickedEvent();entry.onClick.AddListener(()=>{if(open)Close();else Open();});foreach(var a in entry.GetComponents<ForgeUIAction>())Destroy(a);entry.GetComponentInChildren<Text>().text="가공 도면";var hint=entry.GetComponent<ForgeUIHover>();if(hint){hint.description="칼날의 냉각 경로 · 코드 관찰 · 복제 연습";hint.Bind(ui);}
        var layout=ui.GetComponentInChildren<ForgeLegacyLayout>(true);if(layout){layout.cooling=(RectTransform)entry.transform;layout.Invalidate();}
        window.SetActive(false);entry.gameObject.SetActive(false);Place();
    }
    void Zoom(float change){zoom=Mathf.Clamp(zoom+change,.8f,1.8f);diagramRect.localScale=Vector3.one*zoom;}
    void Place(){screenWidth=Screen.width;screenHeight=Screen.height;var r=(RectTransform)window.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(Mathf.Min(480,Screen.width-24),Mathf.Min(760,Screen.height-36));r.anchoredPosition=new Vector2(Mathf.Max(12,Screen.width-r.rect.width-12),-18);}
    public void SyncVisibility(bool visible){entry.gameObject.SetActive(visible);if(!visible&&open)Close();}
    public void Open(){if(!game.CoolingVisible)return;open=true;window.SetActive(true);window.transform.SetAsLastSibling();Observer.Enabled=observing;Refresh();}
    public void Close(){open=false;CancelRunner();EndPractice();window.SetActive(false);game.CoolingPanelClosed();}
    void EndPractice(){CancelRunner();practice?.Dispose();practice=null;practiceTools.SetActive(false);practiceView.Clear();Observer.Enabled=observing;}
    public void TargetChanged(){if(practice!=null){EndPractice();message="대상이 변경되어 이전 연습을 중단했습니다.";}choices.SetActive(false);Refresh();}
    void StartPractice(){if(!game.ProcessingUnlocked)return;if(game.CoolingView.Blade!=null&&(game.CoolingView.Blade.material==9?!game.DragonUnlocked:!game.CoolingUnlocked)){message="해당 재료를 먼저 해금하세요.";return;}if(game.CoolingView.Blade==null){message="칼날을 선택하거나 작업대에 올려주세요";return;}EndPractice();practice=new ForgeCoolingPractice(game.CoolingView.Blade,game.CoolingWater,true,game.ProcessingStocks());practice.Observation.Enabled=observing;practiceView.Select(practice.Blade);practiceTools.SetActive(true);practiceCode.SetTextWithoutNotify(practice.Blade.material==9?ForgeDragonGuide.Practice:"2(Blade)\n3(Blade)\n"+ForgeCoolingGuide.Algorithm);message="실제 재료·돈·판매에는 영향이 없습니다. 서리수 "+game.CoolingWater+"개를 복제했습니다.";Refresh();}
    void ResetPractice(){if(practice==null){message="먼저 복제 연습을 시작하세요.";return;}CancelRunner();practice.Reset();practiceView.Select(practice.Blade);message="같은 도면으로 연습 시작 상태 복원 · 변수/강조/대기 작업 초기화";Refresh();}
    void CancelRunner(){if(runner!=null){StopCoroutine(runner);runner=null;}practice?.Session?.Dispose();}
    void Stop(){if(practice==null)game.CoolingStop();else{CancelRunner();practiceView.Clear();practice.Observation.Clear();message="연습 중단 · 다시 하려면 연습 초기화";}}
    void Run(bool single){if(!game.ProcessingUnlocked)return;if(practice==null){game.CoolingRun(single);return;}if(runner!=null)return;if(practice.Session!=null&&(practice.Session.Completed||practice.Session.Cancelled)){message="연습 초기화 후 다시 실행하세요.";return;}paused=false;var s=practice.Start(practiceCode.text);runner=StartCoroutine(Replay(s,practice.Generation,single));}
    IEnumerator Replay(BlueprintRuntime.Session session,uint generation,bool single){
        yield return null;
        int cursor=session.Evaluation.steps.Count;
        do {
            if(practice==null||generation!=practice.Generation||session.Cancelled)yield break;
            if(paused||practice.Breakpoints.StopBefore(session.NextModule,session.NextLine,single)){runner=null;yield break;}
            if(!session.Completed){yield return BlueprintExecutionTiming.WaitForTick(()=>Time.unscaledTimeAsDouble);if(practice==null||generation!=practice.Generation)yield break;var task=session.Advance();while(!task.IsCompleted)yield return null;if(task.IsFaulted){message=task.Exception.GetBaseException().Message;runner=null;yield break;}}
            while(cursor<session.Evaluation.steps.Count){var step=session.Evaluation.steps[cursor++];uint viewGeneration=practiceView.Receipt(step);float elapsed=0;try{while(elapsed<step.duration){yield return null;if(practice==null||generation!=practice.Generation)yield break;elapsed+=Time.unscaledDeltaTime;practiceView.Animate(viewGeneration,step.duration<=0?1:elapsed/step.duration);}step.CompleteWork();practiceView.Receipt(step);}finally{step.CancelWork();step.ReleaseCooling();}practiceView.Animate(viewGeneration,1);}
            if(session.Completed){message=session.Evaluation.error??"연습 실행 완료 · 예상 서리수 소모 "+practice.Spent+"개";if(session.Evaluation.error!=null)ForgeSoundEffects.CodeError();break;}
        }while(!single);
        runner=null;Refresh();
    }
    void Pick(string mode){choiceMode=mode;choices.SetActive(true);namesStamp="";RefreshChoices();}
    void RefreshChoices(){if(!choices.activeSelf)return;var names=Observer.Names.ToArray();string stamp=choiceMode+string.Join("|",names);if(stamp==namesStamp)return;namesStamp=stamp;foreach(Transform child in choices.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}Button("선택/연결 해제",()=>Select(null),choices.transform);foreach(string name in names){string value=name;Button(name,()=>Select(value),choices.transform);}if(names.Length==0)Text("변수 없음","한 단계 실행하여 변수를 만든 뒤 선택하세요.",choices.transform);}
    void Select(string name){switch(choiceMode){case "distance":distance=name;break;case "previous":previous=name;break;case "visited":visited=name;break;case "variable":selectedVariable=name;break;case "blade":if(practice!=null)message="연습 대상은 고정되어 있습니다. 실제 제작 보기로 돌아가 선택하세요.";else message=game.CoolingSelectVariable(name);break;}choices.SetActive(false);Refresh();}
    void Refresh(){
        if(!open)return;runControl.interactable=stepControl.interactable=practice!=null?runner==null:!game.CoolingRunning;var view=View;var graph=view.Blade?.data.cooling;status.text=graph==null?"칼날을 선택하거나 작업대에 올려주세요":(practice!=null?"복제 연습":"실제 제작")+" · 칼날 #"+view.Blade.id+" · "+graph.Count+"개 지점\n입구 "+graph.Start+" → 출구 "+graph.End+" · 서리수 "+(practice?.Remaining??game.CoolingWater)+"개"+(practice!=null?" · 연습 소모 "+practice.Spent+"개":"");
        if(view.Blade?.material==9){var bone=view.Blade.data.dragonBone;status.text=(practice!=null?"복제 연습":"실제 제작")+" · 용뼈 #"+view.Blade.id+" · 전체 길이 "+bone.Length+" · 마디 "+bone.Count+"개\n"+bone.State+"\n누적 비용 "+bone.Cost+" · 절단 시간 "+bone.Seconds.ToString("0.00")+"초 · 현재 온도 "+view.Blade.heat+"°C"+(view.BonePosition<0?"":"\n이번 절단 "+view.BoneDuration.ToString("0.00")+"초 · 진행 "+(view.Progress*100).ToString("0")+"%");}
        latest.text=view.Latest+"\n"+Observer.Latest+(message.Length>0?"\n"+message:"");
        inspection.text="관찰 "+(Observer.Enabled?"켜짐":"꺼짐")+" · 현재/다음 실행 위치 "+(practice?.Session?.NextLine??game.CoolingNextLine)+"줄\n"+(!Observer.Enabled?"관찰을 켜면 선택한 변수의 현재 값을 표시합니다.":selectedVariable==null?"관찰할 변수를 선택하세요.":Observer.Inspect(selectedVariable));
        var notes=new List<string>();foreach(var pair in new[]{new KeyValuePair<string,string>("누적",distance),new KeyValuePair<string,string>("이전",previous),new KeyValuePair<string,string>("코드 방문",visited)}){Observer.BindArray(pair.Value,graph?.Count??0,out _,out string reason);notes.Add(pair.Key+" ← "+(pair.Value??"미선택")+": "+reason);}bindings.text=string.Join("\n",notes);
        log.gameObject.SetActive(showLog);if(showLog)log.text=Observer.History;practiceLocation.text=practice==null?"":"연습 도면 · 다음 문장 "+(practice.Session?.NextLine??1)+"줄 · 현재 공정 후 일시 정지";diagram.Show(view,Observer,distance,previous,visited,templateText.font);RefreshChoices();
    }
    void Update(){if(!game||!window)return;if(Screen.width!=screenWidth||Screen.height!=screenHeight)Place();if(!open)return;if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.1f;Refresh();}if(View.Path!=null)diagram.SetVerticesDirty();}
    void LateUpdate(){if(!open)return;foreach(var t in content.GetComponentsInChildren<Text>()){if(t.transform.IsChildOf(diagramRect)||t.GetComponentInParent<InputField>())continue;var button=t.transform.parent.GetComponent<Button>();if(button){var l=button.GetComponent<LayoutElement>();if(l)l.preferredHeight=Mathf.Max(36,t.preferredHeight+14);}else{var l=t.GetComponent<LayoutElement>();if(l)l.preferredHeight=Mathf.Max(24,t.preferredHeight+4);}}}
    void OnDisable(){if(window)Close();}void OnDestroy(){CancelRunner();practice?.Dispose();if(window)Destroy(window);if(entry)Destroy(entry.gameObject);}
}
