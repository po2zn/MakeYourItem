using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ForgeMvp
{
    BlueprintRuntime.Session executionSession;
    CodeWindow executionOwner;
    ForgeProductionLedger executionLedger;
    int receiptCursor;
    bool executionSeedConsumed,executionDebugStarted;
    ForgeProduct executionProduct;
    sealed class WindowSnapshot { public string Name, Text; }
    readonly Dictionary<int,WindowSnapshot> executionSources=new Dictionary<int,WindowSnapshot>();

    bool ExecutionSourceChanged()
    {
        if(codeWindows.Count!=executionSources.Count)return true;
        foreach(var window in codeWindows)
            if(!executionSources.TryGetValue(window.id,out var saved)||saved.Name!=window.name||saved.Text!=window.text)return true;
        return false;
    }
    void Execute(bool singleStep)
    {
        if(busy||codeWindows.Count==0)return;
        codeWindows[activeWindow].text=source;
        if(executionSession!=null&&ExecutionSourceChanged()){
            StopExecution(false);reportError=false;
            report+="\n코드창 내용이나 이름이 변경되어 실행을 중단했습니다. 다시 누르면 처음부터 새로 실행합니다. 이미 소비한 비용은 유지됩니다.";
            return;
        }
        if(executionSession==null){
            var owner=codeWindows[activeWindow];
            var materials=new Dictionary<int,long>();
            foreach(var stock in materialStocks??Array.Empty<ForgeMaterialStock>())if(stock!=null&&stock.unlocked)materials[stock.material]=stock.count;
            var modules=new Dictionary<string,string>(StringComparer.Ordinal);
            executionSources.Clear();
            foreach(var window in codeWindows){
                executionSources[window.id]=new WindowSnapshot{Name=window.name,Text=window.text};
                string name;try{name=PythonIdentifier.Parse(window.name);}catch{continue;}
                if(modules.ContainsKey(name))modules[name]=null;else modules.Add(name,window.text);
            }
            var session=BlueprintRuntime.StartSession(source,gold,iron,owner.name,owner.seed,materials,technology,modules,deferErrors:singleStep,clockHour:()=>DayCycle.CurrentHour,context:new ForgeRunContext{Day=()=>DayCycle.Day,Seconds=()=>Time.time,Seed=worldSeed,Speed=craftSpeed});
            if(session.Evaluation.compileError){
                owner.diagnosticCaption="컴파일 오류";
                owner.diagnostic=new ForgeCompileDiagnostic(source,session.Evaluation.errorLine,session.Evaluation.errorMessage);
                owner.showDiagnostic=true;owner.revealDiagnostic=true;owner.diagnosticScroll=Vector2.zero;hover="";session.Dispose();return;
            }
            owner.diagnostic=null;owner.showDiagnostic=false;owner.revealDiagnostic=false;
            executionSession=session;executionOwner=owner;executionLedger=new ForgeProductionLedger();
            receiptCursor=0;executionSeedConsumed=false;executionDebugStarted=false;executionProduct=null;
            resultOwner=owner.name;report="";reportError=false;showSalesPanel=true;
            craftMotion.BeginRun();
        }
        StartCoroutine(RunExecution(singleStep));
    }
    IEnumerator RunExecution(bool singleStep)
    {
        busy=true;var session=executionSession;
        do {
            if(ExecutionSourceChanged()){StopExecution(false);report+="\n코드창이 변경되어 실행을 중단했습니다. 다음 실행은 처음부터 시작합니다.";busy=false;yield break;}
            var advance=session.Advance();
            while(!advance.IsCompleted)yield return null;
            if(executionSession!=session||session.Cancelled){busy=false;yield break;}
            if(advance.IsFaulted){reportError=true;report+="실행 오류: "+advance.Exception.GetBaseException().Message;StopExecution(false);busy=false;yield break;}
            var evaluation=session.Evaluation;
            while(receiptCursor<evaluation.steps.Count){
                var step=evaluation.steps[receiptCursor++];
                if(step.debugOutput){
                    if(!executionDebugStarted){AppendDebug("── "+executionOwner.name+" · 실제 실행 ──");executionDebugStarted=true;}
                    AppendDebug(step.text);continue;
                }
                if(step.process==-1){yield return new WaitForSeconds(step.duration);continue;}
                if(step.process==-2){if(step.setting==1)craftSpeed=step.settingValue;else autoSell=step.settingValue==1;continue;}
                if(!CommitExtraMaterials(step)){reportError=true;report+="추가 재료 부족";StopExecution(false);busy=false;yield break;}
                var stock=step.materialCount>0&&step.material>1?Array.Find(materialStocks??Array.Empty<ForgeMaterialStock>(),s=>s!=null&&s.material==step.material&&s.unlocked):null;
                if((step.materialCount>0&&step.material>1&&(stock==null||stock.count<step.materialCount))||!executionLedger.TrySpend(step.cost,step.iron,ref gold,ref iron)){
                    reportError=true;report+="자금 또는 해당 재료 부족";StopExecution(false);busy=false;yield break;
                }
                if(stock!=null)stock.count-=step.materialCount;
                if(!executionSeedConsumed&&step.process>0){executionOwner.seed=unchecked(executionOwner.seed*1664525u+1013904223u);executionSeedConsumed=true;}
                if(step.warning)AppendDebug(step.line+"줄 · "+step.text);
                report+="Day "+step.day+" "+(step.minute/60).ToString("00")+":"+(step.minute%60).ToString("00")+" · "+(step.module==null?"":step.module+" · ")+step.line+"줄: "+step.text+"\n";
                yield return craftMotion.Play(step);
                if(executionSession!=session){busy=false;yield break;}
                ProgressStep(step);
                if(step.refund>0)RefundMaterial(step.refund);
                if(step.finished){
                    executionProduct=executionLedger.Complete(executionOwner.name,step.heat,step.shaped,step.traits,step.material,step.data,step.productionSeconds);
                    selectedProductId=shop.Add(executionProduct);
                    CompleteProgress(executionProduct);
                    if(autoSell)SellProductInternal(selectedProductId);
                }
            }
            UpdateExecutionLocation();
            if(!singleStep&&!session.Completed)yield return null;
        }while(!singleStep&&!session.Completed);
        if(session.Completed){
            reportError=session.Evaluation.error!=null;
            if(reportError){report+=session.Evaluation.error;AppendDebug(session.Evaluation.error);ShowExecutionError(session.Evaluation);}
            else report+=executionProduct!=null?"\n제작 완료\n"+ForgePricing.Describe(executionProduct,false):"실행 완료 · 결과물 미제출";
            report+="\n실제 제작비 "+executionLedger.CashCost+"G · 소비 재료 · 철 "+executionLedger.IronConsumed+"개";
            StopExecution(false);
        }
        busy=false;
        SaveProgress();
    }
    void ShowExecutionError(Evaluation evaluation)
    {
        CodeWindow target=executionOwner;
        if(evaluation.errorModule!=null){
            target=null;
            foreach(var window in codeWindows){try{if(PythonIdentifier.Parse(window.name)==evaluation.errorModule){target=window;break;}}catch{}}
        }
        if(target==null||!executionSources.TryGetValue(target.id,out var snapshot)||snapshot.Text!=target.text)return;
        target.diagnosticCaption="실행 중 코드 오류";
        target.diagnostic=new ForgeCompileDiagnostic(snapshot.Text,evaluation.errorLine,evaluation.errorMessage);
        target.showDiagnostic=true;target.revealDiagnostic=true;target.diagnosticScroll=Vector2.zero;
    }
    void StopExecution(bool announce)
    {
        var session=executionSession;executionSession=null;session?.Dispose();
        foreach(var window in codeWindows){window.executionLine=0;window.revealExecution=false;}
        if(announce&&session!=null)report+="\n실행 중단 · 이미 소비한 재료는 돌아오지 않습니다.";
    }
    void UpdateExecutionLocation()
    {
        foreach(var window in codeWindows){window.executionLine=0;window.revealExecution=false;}
        if(executionSession==null||executionSession.Completed)return;
        CodeWindow target=executionOwner;
        if(executionSession.NextModule!=null){
            target=null;
            foreach(var window in codeWindows){try{if(PythonIdentifier.Parse(window.name)==executionSession.NextModule){target=window;break;}}catch{}}
        }
        if(target==null)return;
        target.executionLine=executionSession.NextLine;target.executionSource=executionSources[target.id].Text;target.revealExecution=true;
        // Also reveal the marker in a visible, non-focused module window.
        target.scroll.y=Mathf.Max(0,(target.executionLine-3)*(codeStyle==null?22:codeStyle.lineHeight));
    }
    void DrawExecutionHighlight(Rect content,string text,CodeWindow owner)
    {
        if(owner.executionLine<=0||owner.executionSource!=text)return;
        new ForgeCompileDiagnostic(text,owner.executionLine,"").GetLineSpan(out int start,out int length);
        var position=codeStyle.GetCursorPixelPosition(content,new GUIContent(text),start);
        Color previous=GUI.color;GUI.color=new Color(.3f,.65f,1f,.22f);
        GUI.DrawTexture(new Rect(0,position.y,content.width,codeStyle.lineHeight),Texture2D.whiteTexture);
        GUI.color=new Color(.35f,.75f,1f,1f);GUI.DrawTexture(new Rect(0,position.y,3,codeStyle.lineHeight),Texture2D.whiteTexture);GUI.color=previous;
    }
}
