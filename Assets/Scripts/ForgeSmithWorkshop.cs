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
    readonly HashSet<ForgeBlacksmiths.GiantRune> arrivingRunes=new HashSet<ForgeBlacksmiths.GiantRune>();
    readonly List<ForgeBlacksmiths.GiantRune> giantRuneBindings=new List<ForgeBlacksmiths.GiantRune>();
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
        for(int i=0;i<smithMotions.Count;i++){smithMotions[i].SetBurning(blacksmiths.Burning(i,SmithHours));smithMotions[i].SetFrostUnlocked(CoolingUnlocked&&!showTutorial&&!showTitle);}
        UpdateGiantRuneViews();
    }
    static void ReleaseRuneObject(UnityEngine.Object value){if(!value)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    void UpdateGiantRuneViews()
    {
        arrivingRunes.RemoveWhere(r=>!blacksmiths.runes.Contains(r));
        while(giantRuneViews.Count>blacksmiths.runes.Count){int last=giantRuneViews.Count-1;ReleaseRuneObject(giantRuneViews[last]);ReleaseRuneObject(giantRuneMaterials[last]);giantRuneViews.RemoveAt(last);giantRuneMaterials.RemoveAt(last);giantRuneBindings.RemoveAt(last);}
        for(int i=0;i<blacksmiths.runes.Count;i++){
            var rune=blacksmiths.runes[i];
            if(i==giantRuneViews.Count){
                var view=GameObject.CreatePrimitive(PrimitiveType.Cube);view.name="Giant rune";view.transform.SetParent(transform,false);var collider=view.GetComponent<Collider>();collider.enabled=false;ReleaseRuneObject(collider);
                var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color"));view.GetComponent<Renderer>().sharedMaterial=material;giantRuneMaterials.Add(material);giantRuneViews.Add(view);giantRuneBindings.Add(null);
            }
            int family=(rune.rune-1)%3,grade=(rune.rune-1)/3+1;
            var floor=new Vector3(1.2f+(float)rune.x*4.5f,.02f,(float)rune.y*4.5f-1.05f);
            var arrival=giantRuneViews[i].GetComponent<ForgeGiantRuneArrival>();
            if(giantRuneBindings[i]!=rune){if(arrival&&arrival.Playing)arrival.Sample(ForgeGiantRuneArrival.DropSeconds+ForgeGiantRuneArrival.BounceSeconds);giantRuneBindings[i]=rune;}
            if(!arrival||!arrival.Playing)giantRuneViews[i].transform.localPosition=floor;
            giantRuneViews[i].transform.localScale=ApplyGiantRuneModel(giantRuneViews[i],grade)?Vector3.one:new Vector3(2.25f,.02f,2.25f);
            if(arrivingRunes.Remove(rune)){if(!arrival)arrival=giantRuneViews[i].AddComponent<ForgeGiantRuneArrival>();arrival.Begin(floor);}
            giantRuneMaterials[i].color=family==0?new Color(.5f,.25f,.9f):family==1?new Color(.2f,.65f,.9f):new Color(.9f,.65f,.15f);
        }
        UpdateGiantRuneGround();
    }
    ForgeWorkshopAssets runeWorkshopAssets;
    bool ApplyGiantRuneModel(GameObject view,int grade){
        if(!runeWorkshopAssets)runeWorkshopAssets=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");
        var prefab=runeWorkshopAssets?runeWorkshopAssets.GiantRuneModel(grade):null;if(!prefab)return false;
        var existing=view.transform.Find("Rune model");
        if(!existing||existing.GetChild(0).name!=prefab.name){
            if(existing){existing.name="Retired rune model";existing.gameObject.SetActive(false);ReleaseRuneObject(existing.gameObject);}
            var holder=new GameObject("Rune model");holder.transform.SetParent(view.transform,false);
            var model=Instantiate(prefab,holder.transform,false);model.name=prefab.name;
        }
        view.GetComponent<Renderer>().enabled=false;return true;
    }
    Dictionary<int,long> SmithMaterials(){var values=new Dictionary<int,long>();foreach(var s in materialStocks??Array.Empty<ForgeMaterialStock>())if(s!=null&&s.unlocked)values[s.material]=s.count;return values;}
    ForgeRunContext SmithContext(int id)=>new ForgeRunContext{
        Day=()=>DayCycle.Day,Seconds=()=>Time.time,Seed=worldSeed,Speed=craftSpeed,StarSwapSeconds=starSwapSeconds,StarRemoveSeconds=starRemoveSeconds,DragonSettings=dragonSettings,DragonUnlocked=technology.MaterialAvailable(9),CoolingSettings=coolingSettings,CoolingUnlocked=CoolingUnlocked,Observation=id==0?coolingObservation:null,
        SmithCall=(feature,args)=>SmithCall(id,feature,args),StartSmith=(target,create)=>StartSmithJob(target,create,id),
        RefreshResources=session=>session.SetResources(gold,iron,SmithMaterials())
    };
    int SmithInt(object value){if(!(value is int))throw new ArgumentException("정수 인자가 필요합니다.");return (int)value;}
    static double RuneCoordinate(object value){
        if(!(value is int)&&!(value is double)&&!(value is float))throw new ArgumentException("룬 좌표에는 숫자가 필요합니다.");
        double number=Convert.ToDouble(value);
        if(!ForgeBlacksmiths.ValidCoordinate(number))throw new ArgumentException("룬 좌표는 0~3의 유한한 숫자입니다. 소수도 사용할 수 있습니다.");
        return number;
    }
    int CheckSmith(int id){if(id<0||id>=blacksmiths.smiths.Count)throw new ArgumentException("고용하지 않은 대장장이 번호: "+id);return id;}
    bool SmithBusy(int id)=>(id<smithMotions.Count&&smithMotions[id]&&smithMotions[id].IsStunned)||smithJobs.ContainsKey(id)||(id==0&&(busy||executionSession!=null));
    object SmithCall(int current,BlueprintFeature feature,List<object> args)
    {
        blacksmiths.Tick(DayCycle.Day,SmithHours);
        if(feature==BlueprintFeature.SmithCount)return blacksmiths.smiths.Count;
        if(feature==BlueprintFeature.SmithSelf)return current;
        if(ForgeSmithLanguage.IsGiantRune(feature)){
            if(args.Count!=3)throw new ArgumentException("룬 번호, x, y 인자 3개가 필요합니다.");
            int rune=SmithInt(args[0]);double x=RuneCoordinate(args[1]),y=RuneCoordinate(args[2]);
            if(!technology.Owns(BlueprintFeature.GiantRune))throw new ArgumentException("먼저 룬 각인·거대한 룬을 연구하세요.");
            if(rune<1||rune>9)throw new ArgumentException("룬 번호는 정수 1~9입니다.");
            if(feature==BlueprintFeature.GiantRuneTime)return blacksmiths.Remaining(rune,x,y,SmithHours);
            if(blacksmiths.RuneAreaOccupied(x,y,SmithHours))throw new ArgumentException("거대한 룬 효과 범위가 겹칩니다. 기존 룬과 x 또는 y 간격을 0.5보다 크게 두세요.");
            int price=((rune-1)/3+1)*25;
            var stock=Array.Find(materialStocks??Array.Empty<ForgeMaterialStock>(),v=>v!=null&&v.material==rune&&v.unlocked);
            if(diamonds<price||(rune==1?iron<1:stock==null||stock.count<1))throw new ArgumentException("거대한 룬: 다이아 "+price+"개와 "+ForgeMaterialGuide.Find(rune).Name+" 1개가 필요합니다.");
            ForgeBlacksmiths.GiantRune existing=null;
            // Validate everything before either shared resource is spent.
            diamonds-=price;if(rune==1)iron--;else stock.count--;
            if(existing==null){existing=new ForgeBlacksmiths.GiantRune{x=x,y=y};blacksmiths.runes.Add(existing);}
            existing.rune=rune;existing.timed=true;existing.expiresAt=SmithHours+ForgeBlacksmiths.RuneLifetimeHours;arrivingRunes.Add(existing);
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
            data.smithSaleMultiplier=Math.Max(1,data.smithSaleMultiplier)*(1+blacksmiths.RuneBonus(id,3,SmithHours));
        }
        var completed=new ForgeProduct(product.BlueprintName,product.FinalHeat,product.Shaped,product.Traits,product.CashCost,product.IronConsumed,product.Material,data,product.ProductionSeconds);
        for(int i=0;i<copies;i++){selectedProductId=shop.Add(completed);CompleteProgress(completed);if(autoSell)SellProductInternal(selectedProductId);}
        return completed;
    }
}
