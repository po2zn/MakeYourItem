using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// One receipt-driven view for both production and an isolated practice session.
public sealed class ForgeStarlightPanel : MonoBehaviour
{
    static ForgeStarlightPanel instance;
    ForgeMvp owner;
    Canvas canvas;
    RectTransform panel,content;
    Text heading,state,history;
    InputField code;
    GameObject practiceControls;
    readonly Queue<string> lines=new Queue<string>();
    ForgeStarlight shown,lastProduction;
    ForgeStarlightPractice practice;
    Coroutine running;
    bool practiceEvaluated;
    string caption="별빛 가공";
    int selectedA=-1,selectedB=-1;
    public static ForgeStarlightPanel Get(ForgeMvp game=null){
        if(!instance){var root=new GameObject("별철 가공 및 연습");instance=root.AddComponent<ForgeStarlightPanel>();instance.owner=game?game:FindAnyObjectByType<ForgeMvp>();instance.Build();}
        if(game)instance.owner=game;return instance;
    }
    static RectTransform Rect(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);return (RectTransform)g.transform;}
    static void Fill(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    Text Label(string name,Transform parent,int size=17){var r=Rect(name,parent);var t=r.gameObject.AddComponent<Text>();t.text="";t.color=new Color(.12f,.15f,.22f);t.fontSize=size;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;r.gameObject.AddComponent<LayoutElement>();return t;}
    Button Button(string label,Transform parent,UnityEngine.Events.UnityAction action){var r=Rect(label,parent);r.gameObject.AddComponent<Image>().color=new Color(.8f,.85f,.95f);var b=r.gameObject.AddComponent<Button>();var t=Label(label+" 글씨",r);t.text=label;Fill(t.rectTransform);t.alignment=TextAnchor.MiddleCenter;r.gameObject.AddComponent<LayoutElement>().preferredHeight=42;b.onClick.AddListener(action);return b;}
    void Build(){
        canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=150;
        gameObject.AddComponent<GraphicRaycaster>();var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(800,600);scaler.matchWidthOrHeight=.5f;
        panel=Rect("별빛 창",transform);panel.gameObject.AddComponent<Image>().color=new Color(.95f,.96f,1,.98f);
        var viewport=Rect("스크롤 영역",panel);Fill(viewport);viewport.offsetMin=new Vector2(12,12);viewport.offsetMax=new Vector2(-28,-12);viewport.gameObject.AddComponent<Image>().color=Color.white;viewport.gameObject.AddComponent<RectMask2D>();
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
        var barRect=Rect("스크롤 막대",panel);barRect.anchorMin=new Vector2(1,0);barRect.anchorMax=Vector2.one;barRect.offsetMin=new Vector2(-24,12);barRect.offsetMax=new Vector2(-8,-12);barRect.gameObject.AddComponent<Image>().color=new Color(.85f,.87f,.91f);var bar=barRect.gameObject.AddComponent<Scrollbar>();var handle=Rect("스크롤 손잡이",barRect);Fill(handle);var handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=new Color(.3f,.4f,.6f);bar.handleRect=handle;bar.targetGraphic=handleImage;bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
        content=Rect("내용",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.offsetMin=content.offsetMax=Vector2.zero;scroll.content=content;
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=8;layout.padding=new RectOffset(10,10,10,10);layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        heading=Label("제목",content,20);state=Label("별빛 상태",content);history=Label("실행 기록",content,15);
        var enter=Button("연습 시작 · 현재 별철 복제",content,()=>OpenPractice(lastProduction??shown));enter.gameObject.name="연습 진입";
        practiceControls=Rect("연습 도구",content).gameObject;var group=practiceControls.AddComponent<VerticalLayoutGroup>();group.spacing=8;group.childControlHeight=group.childControlWidth=true;group.childForceExpandHeight=false;
        var input=Rect("숫자 도면",practiceControls.transform);input.gameObject.AddComponent<Image>().color=new Color(.93f,.94f,.97f);code=input.gameObject.AddComponent<InputField>();var inputText=Label("코드",input);Fill(inputText.rectTransform);inputText.rectTransform.offsetMin=new Vector2(8,8);inputText.rectTransform.offsetMax=new Vector2(-8,-8);inputText.supportRichText=false;inputText.verticalOverflow=VerticalWrapMode.Truncate;code.textComponent=inputText;code.lineType=InputField.LineType.MultiLineNewline;code.characterLimit=16384;input.gameObject.AddComponent<LayoutElement>().preferredHeight=160;
        code.text="i = 0\n888(i < 70(StarIron)):\n    1?(71(StarIron, i) == 0):\n        73(StarIron, i)\n    0?:\n        i = i + 1";
        Button("연습 코드 실행",practiceControls.transform,RunPractice);
        Button("초기 상태로 되돌리기",practiceControls.transform,ResetPractice);
        var close=Button("닫기 · 연습 실행 중단",content,Close);close.transform.SetSiblingIndex(1);history.transform.SetAsLastSibling();
        ForgeLegacyUIFont.Apply(transform);practiceControls.SetActive(false);canvas.enabled=false;
    }
    void LayoutPanel(bool large){panel.anchorMin=large?new Vector2(.05f,.05f):new Vector2(.52f,.04f);panel.anchorMax=large?new Vector2(.95f,.95f):new Vector2(.98f,.43f);panel.offsetMin=panel.offsetMax=Vector2.zero;}
    void LateUpdate(){
        if(owner&&!owner.StarPanelAllowed){Close();return;}
        if(!canvas.enabled)return;
        // All prose uses preferred height, including after resize or a dynamic font rebuild.
        foreach(var t in content.GetComponentsInChildren<Text>())if(t!=code.textComponent&&t.transform.parent.GetComponent<Button>()==null){var l=t.GetComponent<LayoutElement>();if(l)l.preferredHeight=Mathf.Max(t.fontSize+8,t.preferredHeight+4);}
        foreach(var button in content.GetComponentsInChildren<Button>()){var text=button.GetComponentInChildren<Text>();button.GetComponent<LayoutElement>().preferredHeight=Mathf.Max(42,text.preferredHeight+16);}
    }
    static string ArrayText(ForgeStarlight data,int a=-1,int b=-1){var text=new StringBuilder();for(int i=0;i<data.Count;i++){if(i>0)text.Append("   ");bool selected=i==a||i==b;text.Append(selected?"【":"[").Append(i).Append(": ").Append(data.Revealed(i)?data.Values[i].ToString():"?").Append(selected?"】":"]");}return data.Count==0?"(빈 배열)":text.ToString();}
    void Refresh(){heading.text=practice!=null?"별철 연습 · 실제 재료/골드에 영향 없음":caption;if(shown==null)return;state.text="위치: 값 · 【 】는 동작 대상\n"+ArrayText(shown,selectedA,selectedB)+"\n현재 "+shown.Count+"개 · 교환 "+shown.Swaps+"회 · 제거 "+shown.Removals+"회\n"+(practice!=null?"예상":"누적")+" 추가 가공 시간 "+shown.Seconds.ToString("0.00")+"초 · 정제 보너스 +"+(practice!=null&&!practiceEvaluated?0:shown.Bonus)+"G";history.text=string.Join("\n",lines);}
    void Log(string message){lines.Enqueue(message);while(lines.Count>30)lines.Dequeue();}
    public static void Receipt(ForgeMvp.Step step,bool before,string station){
        if(step.data?.starlight==null)return;
        // Production never creates or opens the standalone practice canvas.
        if(instance)instance.lastProduction=step.data.starlight.Clone();
        if(!before){var game=FindAnyObjectByType<ForgeMvp>();if(game)game.AppendStarlightReceipt(step,station);}
    }
    public static string DebugReceipt(ForgeMvp.Step step,string station){
        var data=step.data?.starlight;if(data==null)return "";
        int a=step.process==73?-1:step.starA,b=step.process==73?-1:step.starB;
        return station+" · "+step.line+"줄 · 별철 #"+step.pieceId+" · "+step.text+"\n"+
            "위치: 값 (【 】 동작 대상) "+ArrayText(data,a,b)+"\n"+
            "별빛 "+data.Count+"개 · 교환 "+data.Swaps+"회 · 제거 "+data.Removals+"회 · 추가 가공 "+data.Seconds.ToString("0.00")+"초 · 정제 보너스 +"+data.Bonus+"G";
    }
    void ShowReceipt(ForgeMvp.Step step,bool before){if(practice!=null)practiceEvaluated=true;shown=(before?step.starlightBefore:step.data.starlight)?.Clone()??step.data.starlight.Clone();selectedA=step.starA;selectedB=step.starB;if(!before){Log(step.line+"줄 · "+step.text+" → "+ArrayText(shown));if(step.process==73)selectedA=selectedB=-1;}Refresh();}
    public void OpenPractice(ForgeStarlight source=null){
        StopPractice();practice?.Dispose();
        if(source==null){var random=new System.Random();var values=new int[5];for(int i=0;i<5;i++)values[i]=random.Next(10);source=new ForgeStarlight(values);}
        practice=new ForgeStarlightPractice(source);practiceControls.SetActive(true);content.Find("연습 진입").gameObject.SetActive(false);canvas.enabled=true;LayoutPanel(true);ResetPractice();
    }
    void StopPractice(){if(running!=null){StopCoroutine(running);running=null;}practice?.Dispose();}
    void ResetPractice(){StopPractice();practice.Reset();practiceEvaluated=false;shown=practice.Piece.data.starlight.Clone();selectedA=selectedB=-1;lines.Clear();Refresh();}
    void RunPractice(){StopPractice();var session=practice.Run(code.text,owner?owner.starSwapSeconds:.4f,owner?owner.starRemoveSeconds:.25f);running=StartCoroutine(Replay(session,practice.Generation));}
    IEnumerator Replay(BlueprintRuntime.Session session,uint generation){
        int cursor=0;
        while(practice!=null&&generation==practice.Generation){
            if(!session.Completed){yield return BlueprintExecutionTiming.WaitForTick(()=>Time.timeAsDouble);if(generation!=practice.Generation)yield break;var task=session.Advance();while(!task.IsCompleted){if(generation!=practice.Generation)yield break;yield return null;}if(task.IsFaulted){Log(task.Exception.GetBaseException().Message);Refresh();yield break;}}
            while(cursor<session.Evaluation.steps.Count){var step=session.Evaluation.steps[cursor++];if(generation!=practice.Generation)yield break;
                if(step.data?.starlight!=null){if(step.starlightBefore!=null)ShowReceipt(step,true);float elapsed=0;while(elapsed<step.duration){yield return null;if(generation!=practice.Generation)yield break;elapsed+=Time.deltaTime;}ShowReceipt(step,false);}else if(step.debugOutput){Log(step.text);Refresh();}
            }
            if(session.Completed){if(!string.IsNullOrEmpty(session.Evaluation.error)){Log(session.Evaluation.error);ForgeSoundEffects.CodeError();Refresh();}running=null;yield break;}
        }
    }
    void Close(){StopPractice();practice=null;practiceControls.SetActive(false);content.Find("연습 진입").gameObject.SetActive(true);canvas.enabled=false;}
    void OnDestroy(){StopPractice();if(instance==this)instance=null;}
}

public partial class ForgeMvp {
    public void AppendStarlightReceipt(Step step,string station){if(showDebugWindow)AppendDebug(ForgeStarlightPanel.DebugReceipt(step,station));}
}
