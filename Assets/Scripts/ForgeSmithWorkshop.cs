using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ForgeMvp
{
    ForgeBlacksmiths blacksmiths=new ForgeBlacksmiths();
    sealed class SmithJob { public BlueprintRuntime.Session session; public ForgeProductionLedger ledger=new ForgeProductionLedger(); public int cursor; public Coroutine coroutine; public int ownerId,line;public string module;public readonly Dictionary<int,WindowSnapshot> sources=new Dictionary<int,WindowSnapshot>(); }
    readonly Dictionary<int,SmithJob> smithJobs=new Dictionary<int,SmithJob>();
    readonly List<ForgeCraftMotion> smithMotions=new List<ForgeCraftMotion>();
    readonly List<GameObject> giantRuneViews=new List<GameObject>();
    readonly List<Material> giantRuneMaterials=new List<Material>();
    bool showSmithTree;
    string smithMessage="";
    double SmithHours=>(DayCycle.Day-1)*24d+DayCycle.CurrentHour;
    // Average every occupied station, including partially filled rows.
    public Vector3 SmithStationCenter {
        get {
            int count=blacksmiths?.smiths?.Count??0;
            var center=Vector3.zero;
            for(int i=0;i<count;i++)center+=new Vector3(ForgeBlacksmiths.X(i)*4.5f,0,ForgeBlacksmiths.Y(i)*4.5f);
            return transform.TransformPoint(count>0?center/count:Vector3.zero);
        }
    }
    void UpdateSmiths()
    {
        if(DayCycle==null||craftMotion==null)return;
        if(!showTitle&&!showTutorial)blacksmiths.Tick(DayCycle.Day,SmithHours);
        else blacksmiths.Normalize(SmithHours);
        if(smithMotions.Count==0)smithMotions.Add(craftMotion);
        while(smithMotions.Count<blacksmiths.smiths.Count){
            int id=smithMotions.Count;
            var root=new GameObject("Blacksmith station "+id);root.transform.SetParent(transform,false);
            var motion=root.AddComponent<ForgeCraftMotion>();motion.Initialize(work);motion.SetStationOffset(new Vector3(ForgeBlacksmiths.X(id)*4.5f,0,ForgeBlacksmiths.Y(id)*4.5f));smithMotions.Add(motion);
        }
        for(int i=smithMotions.Count-1;i>=blacksmiths.smiths.Count;i--){Destroy(smithMotions[i].gameObject);smithMotions.RemoveAt(i);}
        for(int i=0;i<smithMotions.Count;i++)smithMotions[i].SetBurning(blacksmiths.Burning(i,SmithHours));
        UpdateGiantRuneViews();
    }
    void UpdateGiantRuneViews()
    {
        while(giantRuneViews.Count>blacksmiths.runes.Count){int last=giantRuneViews.Count-1;Destroy(giantRuneViews[last]);Destroy(giantRuneMaterials[last]);giantRuneViews.RemoveAt(last);giantRuneMaterials.RemoveAt(last);}
        for(int i=0;i<blacksmiths.runes.Count;i++){
            var rune=blacksmiths.runes[i];
            if(i==giantRuneViews.Count){
                var view=GameObject.CreatePrimitive(PrimitiveType.Cylinder);view.name="Giant rune";view.transform.SetParent(transform,false);Destroy(view.GetComponent<Collider>());
                var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color"));view.GetComponent<Renderer>().sharedMaterial=material;giantRuneMaterials.Add(material);giantRuneViews.Add(view);
            }
            int family=(rune.rune-1)%3,grade=(rune.rune-1)/3+1;
            giantRuneViews[i].transform.localPosition=new Vector3(1.2f+rune.x*4.5f,.02f,rune.y*4.5f);
            giantRuneViews[i].transform.localScale=new Vector3(1.6f+grade*.2f,.02f,1.6f+grade*.2f);
            giantRuneMaterials[i].color=family==0?new Color(.5f,.25f,.9f):family==1?new Color(.2f,.65f,.9f):new Color(.9f,.65f,.15f);
        }
    }
    Dictionary<int,long> SmithMaterials(){var values=new Dictionary<int,long>();foreach(var s in materialStocks??Array.Empty<ForgeMaterialStock>())if(s!=null&&s.unlocked)values[s.material]=s.count;return values;}
    ForgeRunContext SmithContext(int id)=>new ForgeRunContext{
        Day=()=>DayCycle.Day,Seconds=()=>Time.time,Seed=worldSeed,Speed=craftSpeed,
        SmithCall=(feature,args)=>SmithCall(id,feature,args),StartSmith=(target,create)=>StartSmithJob(target,create,id),
        RefreshResources=session=>session.SetResources(gold,iron,SmithMaterials())
    };
    int SmithInt(object value){if(!(value is int))throw new ArgumentException("정수 인자가 필요합니다.");return (int)value;}
    int CheckSmith(int id){if(id<0||id>=blacksmiths.smiths.Count)throw new ArgumentException("고용하지 않은 대장장이 번호: "+id);return id;}
    bool SmithBusy(int id)=>(id<smithMotions.Count&&smithMotions[id]&&smithMotions[id].IsStunned)||smithJobs.ContainsKey(id)||(id==0&&(busy||executionSession!=null));
    object SmithCall(int current,BlueprintFeature feature,List<object> args)
    {
        blacksmiths.Tick(DayCycle.Day,SmithHours);
        if(feature==BlueprintFeature.SmithCount)return blacksmiths.smiths.Count;
        if(feature==BlueprintFeature.SmithSelf)return current;
        if(feature==BlueprintFeature.GiantRune){
            if(args.Count!=3)throw new ArgumentException("90910(룬 번호, x, y)로 호출하세요.");
            int rune=SmithInt(args[0]),x=SmithInt(args[1]),y=SmithInt(args[2]);
            if(!technology.Owns(BlueprintFeature.Rune))throw new ArgumentException("먼저 룬 각인을 연구하세요.");
            if(rune<1||rune>9||x<0||x>3||y<0||y>3)throw new ArgumentException("룬은 1~9, 좌표는 각각 0~3입니다.");
            int price=((rune-1)/3+1)*25;var crystal=Array.Find(materialStocks,s=>s!=null&&s.material==6&&s.unlocked);
            if(diamonds<price||crystal==null||crystal.count<1)throw new ArgumentException("거대한 룬: 다이아 "+price+"개와 수정 1개가 필요합니다.");
            int family=(rune-1)%3;
            var existing=blacksmiths.runes.Find(r=>r.x==x&&r.y==y&&(r.rune-1)%3==family);
            if(existing!=null&&existing.rune>=rune)throw new ArgumentException("이 위치에 같은 계열의 같거나 더 강한 룬이 있습니다.");
            diamonds-=price;crystal.count--;if(existing!=null)existing.rune=rune;else blacksmiths.runes.Add(new ForgeBlacksmiths.GiantRune{rune=rune,x=x,y=y});
            SaveProgress();return null;
        }
        if(feature==BlueprintFeature.SmithGift){
            if(args.Count<1||args.Count>2)throw new ArgumentException("90901(수량) 또는 90901(번호, 수량)으로 호출하세요.");
            int target=CheckSmith(args.Count==1?current:SmithInt(args[0])),amount=SmithInt(args[args.Count-1]);
            if(!blacksmiths.Gift(target,amount,ref diamonds))throw new ArgumentException("선물은 다이아 1~9개이며 보유 수량이 충분해야 합니다.");
            SaveProgress();return null;
        }
        if(args.Count>1||(feature==BlueprintFeature.SmithBusy&&args.Count!=1))throw new ArgumentException("대장장이 번호 하나를 지정하세요.");
        int id=CheckSmith(args.Count==0?current:SmithInt(args[0]));
        switch(feature){
            case BlueprintFeature.SmithDemand:if(!blacksmiths.demandUnlocked)throw new ArgumentException("대장장이 트리에서 요구 수량 함수를 구매하세요.");return blacksmiths.smiths[id].required;
            case BlueprintFeature.SmithFavor:if(!blacksmiths.favorUnlocked)throw new ArgumentException("대장장이 트리에서 호감도 함수를 구매하세요.");return blacksmiths.smiths[id].favor;
            case BlueprintFeature.SmithPosition:if(!blacksmiths.positionUnlocked)throw new ArgumentException("대장장이 트리에서 위치 함수를 구매하세요.");return new[]{ForgeBlacksmiths.X(id),ForgeBlacksmiths.Y(id)};
            case BlueprintFeature.SmithBusy:return SmithBusy(id)?1:0;
        }
        throw new ArgumentException("지원하지 않는 대장장이 명령입니다.");
    }
    int StartSmithJob(int id,Func<ForgeRunContext,BlueprintRuntime.Session> create,int caller)
    {
        UpdateSmiths();
        CheckSmith(id);if(SmithBusy(id))return -1;
        var job=new SmithJob();
        if(smithJobs.TryGetValue(caller,out var parent)){job.ownerId=parent.ownerId;foreach(var pair in parent.sources)job.sources[pair.Key]=pair.Value;}
        else{job.ownerId=executionOwner!=null?executionOwner.id:-1;foreach(var pair in executionSources)job.sources[pair.Key]=pair.Value;}
        smithJobs.Add(id,job);
        try{job.session=create(SmithContext(id));}catch{smithJobs.Remove(id);throw;}
        smithMotions[id].BeginRun();job.coroutine=StartCoroutine(RunSmith(id,job));return id;
    }
    IEnumerator RunSmith(int id,SmithJob job)
    {
        // Do not reenter the dispatcher's current statement while its receipt is being built.
        yield return null;
        try{
            while(smithJobs.TryGetValue(id,out var active)&&active==job&&!job.session.Cancelled){
                job.line=job.session.NextLine;job.module=job.session.NextModule;
                if(!job.session.Completed)yield return BlueprintExecutionTiming.WaitForTick(()=>Time.timeAsDouble);
                if(job.session.Cancelled||!smithJobs.TryGetValue(id,out var current)||current!=job)yield break;
                var advance=job.session.Advance();while(!advance.IsCompleted)yield return null;
                if(job.session.Cancelled)yield break;
                if(advance.IsFaulted){AppendDebug("대장장이 "+id+": "+advance.Exception.GetBaseException().Message);smithMotions[id].ReactToCodeError();yield break;}
                var evaluation=job.session.Evaluation;
                while(job.cursor<evaluation.steps.Count){
                    var step=evaluation.steps[job.cursor++];job.line=step.line;job.module=step.module;
                    if(step.debugOutput){AppendDebug("대장장이 "+id+" · "+step.text);continue;}
                    if(step.process==-1){yield return new WaitForSeconds(step.duration);continue;}
                    if(step.process==-2){if(step.setting==1)craftSpeed=ForgeTechnology.SpeedMultiplier(step.settingValue);else autoSell=step.settingValue==1;continue;}
                    if(!SpendSmithStep(step,job.ledger)){AppendDebug("대장장이 "+id+": 공용 재료 부족으로 중단");smithMotions[id].ReactToCodeError();yield break;}
                    yield return smithMotions[id].Play(step);
                    if(job.session.Cancelled)yield break;
                    ProgressStep(step);if(step.refund>0)RefundMaterial(step.refund);
                    if(step.finished&&FinishSmithProduct(id,job.ledger.CompleteNext("대장장이 "+id,step.heat,step.shaped,step.traits,step.material,step.data,step.productionSeconds))==null)yield return smithMotions[id].DestructionReaction();
                }
                if(job.session.Completed){if(evaluation.error!=null){AppendDebug("대장장이 "+id+" · "+evaluation.error);smithMotions[id].ReactToCodeError();}yield break;}
                // Every worker owns its tick wait independently.
            }
        }finally{job.session.Dispose();if(smithJobs.TryGetValue(id,out var active)&&active==job)smithJobs.Remove(id);SaveProgress();}
    }
    void StopSmithJobs(){foreach(var id in smithJobs.Keys)if(id<smithMotions.Count&&smithMotions[id])smithMotions[id].CancelCurrentAction();foreach(var job in new List<SmithJob>(smithJobs.Values)){job.session?.Dispose();if(job.coroutine!=null)StopCoroutine(job.coroutine);}smithJobs.Clear();}
    public bool GameCodeHasJobs(int windowId){
        if(executionSession!=null&&executionOwner!=null&&executionOwner.id==windowId)return true;
        foreach(var job in smithJobs.Values)if(job.ownerId==windowId)return true;
        return false;
    }
    void StopCodeExecution(int windowId){
        var window=codeWindows.Find(w=>w.id==windowId);
        if(window!=null){window.diagnostic=null;window.showDiagnostic=false;window.revealDiagnostic=false;window.diagnosticCaption="";window.diagnosticScroll=Vector2.zero;window.executionLine=0;window.revealExecution=false;if(reportError&&resultOwner==window.name){reportError=false;report="";}}
        if(hierarchyGame)hierarchyGame.HideCodeHelp();
        if(executionSession!=null&&executionOwner!=null&&executionOwner.id==windowId)StopExecution(true);
        var ids=new List<int>();foreach(var pair in smithJobs)if(pair.Value.ownerId==windowId)ids.Add(pair.Key);
        foreach(int id in ids){var job=smithJobs[id];smithJobs.Remove(id);job.session.Dispose();if(job.coroutine!=null)StopCoroutine(job.coroutine);if(id<smithMotions.Count&&smithMotions[id])smithMotions[id].CancelCurrentAction();}
        SaveProgress();
    }
    public void GameWorkerLines(int windowId,List<int> lines){
        lines.Clear();var window=codeWindows.Find(w=>w.id==windowId);if(window==null)return;
        foreach(var job in smithJobs.Values){
            if(job.line<=0||!job.sources.TryGetValue(windowId,out var saved)||saved.Text!=window.text)continue;
            string module=job.module??job.session.MainModuleName;
            bool match=module=="__main__"?windowId==job.ownerId:false;
            if(module!="__main__")try{match=PythonIdentifier.Parse(saved.Name)==module;}catch{}
            if(match&&!lines.Contains(job.line))lines.Add(job.line);
        }
    }
    bool SpendSmithStep(Step step,ForgeProductionLedger ledger)
    {
        var needed=new Dictionary<int,int>();
        if(step.iron>0)needed[1]=step.iron;
        if(step.material>1&&step.materialCount>0)needed[step.material]=step.materialCount;
        if(step.consumed!=null)foreach(var pair in step.consumed){needed.TryGetValue(pair.Key,out int n);needed[pair.Key]=checked(n+pair.Value);}
        if(gold<step.cost)return false;
        foreach(var pair in needed){if(pair.Key==1){if(iron<pair.Value)return false;}else{var stock=Array.Find(materialStocks,s=>s!=null&&s.material==pair.Key&&s.unlocked);if(stock==null||stock.count<pair.Value)return false;}}
        needed.TryGetValue(1,out int ironCost);if(!ledger.TrySpend(step.cost,ironCost,ref gold,ref iron))return false;
        foreach(var pair in needed)if(pair.Key!=1)Array.Find(materialStocks,s=>s!=null&&s.material==pair.Key&&s.unlocked).count-=pair.Value;
        if(hierarchyGame)foreach(var pair in needed)if(pair.Value>0)hierarchyGame.AnimateMaterialUse(pair.Key);
        return true;
    }
    ForgeProduct FinishSmithProduct(int id,ForgeProduct product)
    {
        blacksmiths.Tick(DayCycle.Day,SmithHours);
        var data=product.Data;int copies=1;
        if(product.Kind!=ForgeProductKind.Unfinished){
            blacksmiths.Chances(id,SmithHours,out double destruction,out double great);
            double roll=blacksmiths.Next(1000000)/1000000d;
            if(roll<destruction){AppendDebug("대장장이 "+id+" · 제작 파괴");return null;}
            if(roll<destruction+great){
                copies=2;data.smithSaleMultiplier=1.25+blacksmiths.Next(20001)/10000d;
                for(int material=1;material<data.materials.Length;material++)for(int n=0;n<data.materials[material];n++)RefundMaterial(material);
                AppendDebug("대장장이 "+id+" · 대성공! 재료 반환 · 2개 생산 · 판매 ×"+data.smithSaleMultiplier.ToString("0.00"));
            }
            data.smithSaleMultiplier=Math.Max(1,data.smithSaleMultiplier)*(1+blacksmiths.RuneBonus(id,3)*5);
        }
        var completed=new ForgeProduct(product.BlueprintName,product.FinalHeat,product.Shaped,product.Traits,product.CashCost,product.IronConsumed,product.Material,data,product.ProductionSeconds);
        for(int i=0;i<copies;i++){selectedProductId=shop.Add(completed);CompleteProgress(completed);if(autoSell)SellProductInternal(selectedProductId);}
        return completed;
    }
}
