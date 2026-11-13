using System;
using System.Collections.Generic;
using UnityEngine;

public partial class ForgeMvp : ISerializationCallbackReceiver
{
    [SerializeField,HideInInspector] List<SavedWindow> reloadCodeWindows=new List<SavedWindow>();
    [SerializeField,HideInInspector] bool hasReloadCodeWindows;
    // HashSet-backed research and immutable shop entries are not Unity-serializable.
    // Keep a DTO snapshot so a script/domain reload cannot erase them before autosave.
    [SerializeField,HideInInspector] bool hasReloadWorkshop;
    [SerializeField,HideInInspector] string[] reloadTechnologyNodes;
    [SerializeField,HideInInspector] List<SavedItem> reloadProducts=new List<SavedItem>();
    [SerializeField,HideInInspector] long reloadNextProductOrder;
    public void OnBeforeSerialize(){
        if(!windowsInitialized)return;
        reloadTechnologyNodes=technology.ExportNodes();
        reloadProducts=CaptureProducts(shop);reloadNextProductOrder=shop.NextOrder;hasReloadWorkshop=true;
        var snapshot=new List<SavedWindow>();
        foreach(var w in codeWindows)snapshot.Add(new SavedWindow{id=w.id,name=w.name,text=w.text,rect=w.rect,seed=w.seed,collapsed=w.collapsed,expandedHeight=w.expandedHeight});
        reloadCodeWindows=snapshot;hasReloadCodeWindows=true;
    }
    public void OnAfterDeserialize(){
        if(hasReloadWorkshop){
            technology.Restore(Array.Empty<BlueprintFeature>(),reloadTechnologyNodes);
            shop.Clear();
            foreach(var item in reloadProducts??new List<SavedItem>())shop.Add(RestoreProduct(item),item.order);
            shop.RestoreNextOrder(reloadNextProductOrder);
        }
        if(!hasReloadCodeWindows||reloadCodeWindows==null)return;
        codeWindows.Clear();foreach(var w in reloadCodeWindows)codeWindows.Add(new CodeWindow{id=w.id,name=w.name,text=w.text,rect=w.rect,seed=w.seed,collapsed=w.collapsed,expandedHeight=w.expandedHeight});
        windowsInitialized=true;activeWindow=Math.Max(0,Math.Min(activeWindow,codeWindows.Count-1));source=codeWindows.Count>0?codeWindows[activeWindow].text:"";
        foreach(var w in codeWindows)nextWindowId=Math.Max(nextWindowId,w.id+1);
    }

    // Index is the material ID; keep the last valid input independently of shop UI lifetime.
    [SerializeField,HideInInspector] int[] materialPurchaseQuantities=new int[10];
    public int MaterialPurchaseQuantity(int material)=>material>0&&material<10&&materialPurchaseQuantities!=null&&material<materialPurchaseQuantities.Length&&materialPurchaseQuantities[material]>0?materialPurchaseQuantities[material]:1;
    public void SetMaterialPurchaseQuantity(int material,int quantity)
    {
        if(material<1||material>=10||quantity<1||MaterialPurchaseQuantity(material)==quantity)return;
        if(materialPurchaseQuantities==null||materialPurchaseQuantities.Length!=10)RestoreMaterialPurchaseQuantities(materialPurchaseQuantities);
        materialPurchaseQuantities[material]=quantity;QueueCodeSave();
    }
    void RestoreMaterialPurchaseQuantities(int[] saved)
    {
        materialPurchaseQuantities=new int[10];
        for(int i=1;i<10;i++)materialPurchaseQuantities[i]=saved!=null&&i<saved.Length&&saved[i]>0?saved[i]:1;
        if(hierarchyTree)hierarchyTree.RestorePurchaseQuantities();
    }

    const string LegacySaveKey="MakeYourItem.Progress.v1";
    const int SlotCount=3;
    int activeSaveSlot;
    static string SlotKey(int slot)=>"MakeYourItem.Progress.v1.Slot"+slot;
    string SaveKey=>SlotKey(activeSaveSlot);
    int worldSeed=72431,diamonds,totalSales,tutorialStage;
    long earnedGold;
    float craftSpeed=1;
    bool autoSell,showProgress,progressLoaded;
    string saveMessage="";
    readonly HashSet<string> discoveries=new HashSet<string>();
    Vector2 progressScroll;
    [Serializable] public class SavedWindow { public int id;public string name,text; public Rect rect; public uint seed; public bool collapsed;public float expandedHeight; }
    [Serializable] public class SavedItem { public long order;public string owner; public int material,heat,iron,cost;public bool shaped;public ForgeTraits traits;public ForgeItemData data;public double seconds; }
    static List<SavedItem> CaptureProducts(ForgeShop source){
        var result=new List<SavedItem>();
        foreach(var entry in source.Items){var p=entry.Product;result.Add(new SavedItem{order=entry.Order,owner=p.BlueprintName,material=p.Material,heat=p.FinalHeat,shaped=p.Shaped,traits=p.Traits,cost=p.CashCost,iron=p.IronConsumed,data=p.Data,seconds=p.ProductionSeconds});}
        return result;
    }
    static ForgeProduct RestoreProduct(SavedItem item)=>new ForgeProduct(item.owner,item.heat,item.shaped,item.traits,item.cost,item.iron,item.material,item.data,item.seconds);
    [Serializable] public class SavedProgress {
        public int windowDataVersion,activeCodeWindow,nextCodeWindowId;public Vector2 viewOffset;public float viewZoom;
        public ForgeBlacksmiths blacksmiths;
        public int introStage,introSaleVersion;public string introCode;public string[] technologyNodes;public bool campaignComplete,endingSeen;
        public string savedAt;public long nextProductOrder;
        public long savedAtUtcTicks;public List<SavedItem> titleSwords;
        public int version=1,gold,iron,day,seed,diamonds,sales,tutorial;public long earned;public double hour;public float speed;public bool autoSell;
        public int frostRevealVersion;public bool frostRevealSeen;
        public int[] materialPurchaseQuantities;
        public ForgeMaterialStock[] stocks;public BlueprintFeature[] features;public string[] discoveries;
        public List<SavedWindow> windows=new List<SavedWindow>();public List<SavedItem> items=new List<SavedItem>();
    }
    bool LoadProgress()
    {
        progressLoaded=false;codeSavePending=false;
        if(activeSaveSlot<1||!PlayerPrefs.HasKey(SaveKey)){saveMessage="저장 데이터가 없습니다.";return false;}
        try {
            var saved=JsonUtility.FromJson<SavedProgress>(PlayerPrefs.GetString(SaveKey));
            if(saved==null||saved.version!=1||saved.gold<0||saved.iron<0||saved.diamonds<0||saved.day<1||saved.hour<0||saved.hour>=24)throw new Exception("저장 형식이 올바르지 않습니다.");
            if(double.IsNaN(saved.hour)||double.IsInfinity(saved.hour)||saved.items==null)throw new Exception("저장 데이터가 손상되었습니다.");
            foreach(var window in saved.windows??new List<SavedWindow>())if(window==null||window.name==null||window.text==null)throw new Exception("도면 데이터가 손상되었습니다.");
            // Parse products before applying state so an invalid product cannot half-load a save.
            var restoredShop=new ForgeShop();foreach(var item in saved.items){if(item==null)throw new Exception("완성품 데이터가 손상되었습니다.");restoredShop.Add(RestoreProduct(item),item.order);}
            restoredShop.RestoreNextOrder(saved.nextProductOrder);
            var restoredHistory=DisplayHistory(saved);
            ResetProgressForDebug(false);titleSwordHistory=restoredHistory;
            gold=saved.gold;iron=saved.iron;worldSeed=saved.seed;diamonds=saved.diamonds;totalSales=saved.sales;tutorialStage=saved.tutorial;earnedGold=saved.earned;
            // Older saves completed the tutorial immediately on sale (stage 10).
            int restoredIntro=saved.introSaleVersion==0&&saved.introStage==10?11:saved.introStage;
            lesson=ForgeTutorialLesson.Restore(restoredIntro,saved.introCode);
            craftSpeed=Mathf.Clamp(saved.speed,1,5);autoSell=saved.autoSell;materialStocks=saved.stocks??Array.Empty<ForgeMaterialStock>();technology.Restore(saved.features,saved.technologyNodes,saved.stocks);campaignComplete=saved.campaignComplete;endingSeen=saved.endingSeen;
            frostRevealSeen=saved.frostRevealVersion>=1?saved.frostRevealSeen:technology.MaterialAvailable(8);
            RestoreMaterialPurchaseQuantities(saved.materialPurchaseQuantities);
            craftSpeed=technology.RestoreSpeed(craftSpeed);
            bool legacySpeed=saved.technologyNodes==null?Array.Exists(saved.features??Array.Empty<BlueprintFeature>(),f=>f==BlueprintFeature.Speed||f==BlueprintFeature.AutoSell):Array.Exists(saved.technologyNodes,id=>id=="function-Speed");
            autoSell=technology.Owns(BlueprintFeature.AutoSell)&&(saved.autoSell||legacySpeed);
            DayCycle.Clock.Restore(saved.day,saved.hour);
            blacksmiths=saved.blacksmiths??new ForgeBlacksmiths();blacksmiths.Normalize(SmithHours);
            foreach(var entry in saved.discoveries??Array.Empty<string>())discoveries.Add(entry);
            foreach(var item in restoredShop.Items)shop.Add(item.Product,item.Order);shop.RestoreNextOrder(restoredShop.NextOrder);
            if(saved.windows!=null&&(saved.windows.Count>0||saved.windowDataVersion>=1)){codeWindows.Clear();nextWindowId=0;foreach(var window in saved.windows)codeWindows.Add(new CodeWindow{id=saved.windowDataVersion>=1?window.id:nextWindowId++,name=window.name,text=window.text,rect=RestoredCodeRect(window),seed=window.seed,collapsed=window.collapsed,expandedHeight=Mathf.Max(180,window.expandedHeight>0?window.expandedHeight:240)});windowsInitialized=true;activeWindow=Mathf.Clamp(saved.activeCodeWindow,0,Mathf.Max(0,codeWindows.Count-1));source=codeWindows.Count>0?codeWindows[activeWindow].text:"";foreach(var restored in codeWindows)nextWindowId=Mathf.Max(nextWindowId,restored.id+1);nextWindowId=Mathf.Max(nextWindowId,saved.nextCodeWindowId);}
            if(saved.windowDataVersion>=1){workspaceOffset=saved.viewOffset;workspaceZoom=Mathf.Clamp(saved.viewZoom,.25f,2.5f);}else ClampGameCodeWindows();
            progressLoaded=true;saveMessage="슬롯 "+activeSaveSlot+"의 공방을 불러왔습니다.";return true;
        }catch(Exception error){saveMessage="저장 불러오기 실패: "+error.Message;progressLoaded=false;return false;}
    }
    static Rect RestoredCodeRect(SavedWindow window){
        var rect=window.rect;
        if(float.IsNaN(rect.x)||float.IsInfinity(rect.x))rect.x=24;
        if(float.IsNaN(rect.y)||float.IsInfinity(rect.y))rect.y=200;
        if(float.IsNaN(rect.width)||float.IsInfinity(rect.width)||rect.width<340)rect.width=360;
        // Older saves stored a folded height without storing the folded state.
        if(window.collapsed)rect.height=36;
        else if(float.IsNaN(rect.height)||float.IsInfinity(rect.height)||rect.height<180)rect.height=Mathf.Max(240,window.expandedHeight);
        return rect;
    }
    bool codeSavePending;
    float codeSaveAt,codeSaveDeadline;
    void QueueCodeSave(){if(!progressLoaded||activeSaveSlot<1)return;float now=Time.realtimeSinceStartup;if(!codeSavePending)codeSaveDeadline=now+3f;codeSavePending=true;codeSaveAt=Mathf.Min(now+1f,codeSaveDeadline);}
    void SaveEditedCode(bool immediately=false){if(!codeSavePending||(!immediately&&Time.realtimeSinceStartup<codeSaveAt))return;codeSaveAt=codeSaveDeadline=Time.realtimeSinceStartup+5f;SaveProgress();}
    void SaveProgress()
    {
        if(!progressLoaded||activeSaveSlot<1||DayCycle==null)return;
        try {
            if(hierarchyGame)hierarchyGame.CommitCodeWindowBounds();
            var saved=new SavedProgress{windowDataVersion=1,activeCodeWindow=activeWindow,nextCodeWindowId=nextWindowId,viewOffset=workspaceOffset,viewZoom=workspaceZoom,savedAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm"),gold=gold,iron=iron,seed=worldSeed,diamonds=diamonds,sales=totalSales,tutorial=tutorialStage,earned=earnedGold,day=DayCycle.Day,hour=DayCycle.CurrentHour,speed=craftSpeed,autoSell=autoSell,stocks=materialStocks,features=technology.Export(),technologyNodes=technology.ExportNodes(),campaignComplete=campaignComplete,endingSeen=endingSeen,discoveries=new List<string>(discoveries).ToArray()};
            saved.frostRevealVersion=1;saved.frostRevealSeen=frostRevealSeen;
            saved.materialPurchaseQuantities=new int[10];
            for(int material=1;material<10;material++)saved.materialPurchaseQuantities[material]=MaterialPurchaseQuantity(material);
            saved.nextProductOrder=shop.NextOrder;
            saved.savedAtUtcTicks=DateTime.UtcNow.Ticks;saved.titleSwords=new List<SavedItem>(titleSwordHistory);
            saved.introStage=lesson.Stage;saved.introSaleVersion=1;saved.introCode=lesson.Code;
            saved.blacksmiths=blacksmiths;
            for(int i=0;i<codeWindows.Count;i++){var w=codeWindows[i];saved.windows.Add(new SavedWindow{id=w.id,name=w.name,text=w.text,rect=w.rect,seed=w.seed,collapsed=w.collapsed,expandedHeight=w.expandedHeight});}
            saved.items=CaptureProducts(shop);
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(saved));PlayerPrefs.Save();codeSavePending=false;saveMessage="공방 저장 완료";
        }catch(Exception error){saveMessage="저장 실패: "+error.Message;}
    }
    void ResetProgressForDebug(bool persist=true)
    {
        CoolingView.Clear();coolingObservation.Clear();coolingBreakpoints.Clear();coolingPanel?.TargetChanged();coolingProductId=-1;
        if(busy||executionSession!=null)return;
        if(hierarchyGame)hierarchyGame.DiscardCodeViews();
        codeSavePending=false;
        RestoreMaterialPurchaseQuantities(null);
        StopSmithJobs();ResetFrostRevealProgress();blacksmiths=new ForgeBlacksmiths();showSmithTree=false;
        EndWorkspaceDrag();CancelHierarchyTreeDrag();guideBook.CancelDrag();
        GUIUtility.hotControl=0;if(Event.current!=null)ClearUIFocus();
        campaignComplete=false;endingSeen=false;
        gold=100;iron=10;diamonds=0;totalSales=0;earnedGold=0;tutorialStage=0;
        lesson=new ForgeTutorialLesson();showTutorial=false;tutorialAnimating=false;
        showGameMenu=false;returnToTitleRequested=false;
        worldSeed=Guid.NewGuid().GetHashCode();craftSpeed=1;autoSell=false;
        materialStocks=new ForgeMaterialStock[8];
        for(int i=0;i<materialStocks.Length;i++)materialStocks[i]=new ForgeMaterialStock{material=i+2,unlocked=i<2};
        technology.Restore(Array.Empty<BlueprintFeature>());discoveries.Clear();shop.Clear();titleSwordHistory.Clear();
        DayCycle.Clock.Restore(1,8);
        workspaceOffset=Vector2.zero;workspaceZoom=1;UpdateWorkspaceCamera();
        source=InitialSource;workspaceWindow=InitialCodeWindow;showSalesPanel=false;
        codeWindows.Clear();
        codeWindows.Add(new CodeWindow{id=0,name="f1",text=InitialSource,rect=workspaceWindow});
        activeWindow=0;nextWindowId=1;windowsInitialized=true;
        undoHistory=codeWindows[0].undo;scroll=Vector2.zero;
        deleteWindowId=-1;renamingWindow=-1;selectedProductId=-1;
        executionSources.Clear();executionOwner=null;executionLedger=null;executionProduct=null;receiptCursor=0;
        workspaceOffset=Vector2.zero;workspaceZoom=1;
        showGuideBook=false;showTechTree=false;showProgress=false;showDebugWindow=false;showProductInventory=false;
        debugOutput.Clear();debugScroll=Vector2.zero;logs=Vector2.zero;
        if(craftMotion!=null)craftMotion.BeginRun();
        reportError=false;resultOwner="아직 실행하지 않음";
        report="게임을 초기화했습니다. ▷로 한 줄씩 실행하거나 ▶로 전체 실행하세요.";
        purchaseMessage="선행 노드부터 구매하세요. 고급 재료는 공급 해금 후 재료 상점에서 구매합니다.";
        purchaseSucceeded=false;
        progressLoaded=persist;if(persist)SaveProgress();
    }
    void OnApplicationQuit(){ForgeMouseTrace.Mark("APPLICATION_QUIT","",true);SaveProgress();StopMouseTrace();}
    bool CommitExtraMaterials(Step step)
    {
        if(step.consumed==null)return true;
        foreach(var pair in step.consumed){if(pair.Key==1){if(iron<pair.Value)return false;}else {var stock=Array.Find(materialStocks,s=>s!=null&&s.material==pair.Key&&s.unlocked);if(stock==null||stock.count<pair.Value)return false;}}
        foreach(var pair in step.consumed){if(pair.Key==1)executionLedger.TrySpend(0,pair.Value,ref gold,ref iron);else Array.Find(materialStocks,s=>s!=null&&s.material==pair.Key&&s.unlocked).count-=pair.Value;}
        return true;
    }
    void RefundMaterial(int material)
    {
        if(material==1){if(iron<int.MaxValue)iron++;return;}
        var stock=Array.Find(materialStocks,s=>s!=null&&s.material==material);if(stock!=null&&stock.count<long.MaxValue)stock.count++;
    }
    void CompleteProgress(ForgeProduct product)
    {
        RememberTitleSword(product);
        if(ForgeCampaign.CanComplete(technology,product))campaignComplete=true;
        diamonds=checked(diamonds+product.DiamondReward);
        foreach(var bonus in ForgePricing.Bonuses(product))if(bonus.Value>0)discoveries.Add(bonus.Key);
        if(product.Kind!=ForgeProductKind.Unfinished)tutorialStage=Math.Max(tutorialStage,5);
    }
    bool SellProductInternal(int id)
    {
        if(!shop.TrySell(id,ref gold,out var sold))return false;
        ForgeSoundEffects.ProductSold();
        if(coolingProductId==id){CoolingView.Clear();coolingPanel?.TargetChanged();coolingProductId=-1;}
        reportError=false;totalSales++;earnedGold+=ForgePricing.Quote(sold).SalePrice;tutorialStage=Math.Max(tutorialStage,6);selectedProductId=-1;
        resultOwner=sold.BlueprintName;report= "판매 영수증 · #"+id+"\n"+ForgePricing.Describe(sold,false);return true;
    }
    void ProgressStep(Step step){if(step.process>=1&&step.process<=4)tutorialStage=Math.Max(tutorialStage,step.process);}
    void DrawProgressButton()
    {
        if(ForgeTextLayout.Button(ProgressButtonRect,"성장 목표 · 공방 기록"))showProgress=true;
    }
    void DrawProgress()
    {
        GUI.Box(new Rect(0,0,Screen.width,Screen.height),"");
        GUILayout.BeginArea(new Rect(20,20,Mathf.Max(100,Screen.width-40),Mathf.Max(100,Screen.height-40)));
        if(ForgeSoundEffects.LayoutButton("← 대장간으로 돌아가기")){showProgress=false;}
        bool canReturn=GUI.enabled;GUI.enabled=canReturn&&!busy&&executionSession==null;
        if(ForgeSoundEffects.LayoutButton("저장하고 타이틀로"))ReturnToTitle();
        GUI.enabled=canReturn;
        progressScroll=GUILayout.BeginScrollView(progressScroll);
        ForgeTextLayout.LayoutLabel("공방 기록 · Day "+DayCycle.Day+" · "+((int)DayCycle.CurrentHour).ToString("00")+":"+((int)(DayCycle.CurrentHour*60)%60).ToString("00"));
        ForgeTextLayout.LayoutLabel("골드 "+gold+"G · 다이아몬드 "+diamonds+"D · 판매 "+totalSales+"개 · 누적 판매액 "+earnedGold+"G");
        ForgeTextLayout.LayoutLabel("가속 "+craftSpeed+"배 · 자동판매 "+(autoSell?"켜짐":"꺼짐"));
        ForgeTextLayout.LayoutLabel(lesson.Heading);
        bool tutorialEnabled=GUI.enabled;GUI.enabled=tutorialEnabled&&!busy&&executionSession==null;
        if(ForgeSoundEffects.LayoutButton(lesson.Complete?"검 제작 튜토리얼 다시 보기":"검 제작 튜토리얼 시작 / 이어하기"))OpenTutorial();
        GUI.enabled=tutorialEnabled;
        DrawCampaignProgress();
        ForgeTextLayout.LayoutLabel("발견 도감");
        if(discoveries.Count==0)ForgeTextLayout.LayoutLabel("아직 발견한 특성이 없습니다.");
        foreach(var discovery in discoveries)ForgeTextLayout.LayoutLabel(discovery+" · "+DiscoveryRule(discovery));
        ForgeTextLayout.LayoutLabel("행운 완성 검은 판매 +30G와 다이아 기본 1개. 별무늬 별철 검 +1개, 최종 의뢰 조건의 명작 +1개로 최대 3개입니다. 다이아는 대장장이 연구·고용·선물·거대한 룬에 사용합니다.");
        bool old=GUI.enabled;GUI.enabled=!busy&&executionSession==null;
        if(ForgeSoundEffects.LayoutButton("공방 저장"))SaveProgress();
        if(ForgeSoundEffects.LayoutButton("현재 도면을 숫자 표기로 변환"))TranslateCurrent(false);
        if(ForgeSoundEffects.LayoutButton("현재 도면을 영문 표기로 변환"))TranslateCurrent(true);
        GUI.enabled=old;
        ForgeTextLayout.LayoutLabel(saveMessage);
        ForgeTextLayout.LayoutLabel("제작·판매·구매 후 자동 저장. 실행 도중 종료하면 지불한 재료는 유지되며 실행 스택과 작업 중 물체는 복원하지 않습니다. 종료 중에는 시간이 흐르지 않습니다.");
        GUILayout.EndScrollView();GUILayout.EndArea();
    }
    static string DiscoveryRule(string name)
    {
        switch(name){
            case "적정 온도 담금질":return "검별 적정 온도에 정확히 맞춰 가열·타격한 뒤 물로 냉각";
            case "담금질 경도 7":return "850°C 이상에서 가열·타격한 검날을 물(3)로 냉각";
            case "손잡이":return "성형하고 식힌 검날에 철·나무·은·별철로 손잡이 제작, 또는 정제 용뼈 장착";
            case "연마":return "식힌 성형 검날을 연마하여 판매 가치 향상";
            case "은도금":return "검날을 식힌 뒤 연마하고 은도금";
            case "별무늬":return "성형한 별철 검날을 900°C에서 냉각";
            case "서리 문양":return "100~900°C의 검날을 서리수로 냉각";
            case "마력 전도":return "손잡이에 약한 마력(1)을 3회 주입";
            case "용의 맥동":return "용뼈 손잡이와 충전된 수정 조합";
            case "공명 수정":return "숨은 열을 찾아 활성화한 수정을 식혀 장착";
            case "결계":return "봉인층을 손상 없이 역순으로 해제";
            case "마력 단조":return "시간에 따라 감소하는 강도를 냉각 시 확정";
            case "삼중 공명":return "서로 다른 계열의 룬 3개 각인";
            case "오늘의 행운":return "777?() 코드로 777(코드,완성 검) 적용 후 제출";
            case "저주 배율":return "666() 적용 후 파괴되지 않은 작업물";
            default:return "룬 번호의 계열과 등급에 따른 각인 보너스";
        }
    }
    void TranslateCurrent(bool english)
    {
        if(english&&!englishGuideUnlocked){saveMessage="테크 트리에서 영어 예약어 연구를 먼저 구매하세요.";return;}
        source=ForgeCodeTranslation.Translate(source,english);
        if(codeWindows.Count>0)codeWindows[activeWindow].text=source;
        QueueCodeSave();
    }
}
public static class ForgeCodeTranslation
{
    public static string Translate(string source,bool english)
    {
        var tokens=BlueprintLexer.Scan(source,true);var result=new System.Text.StringBuilder(source);
        for(int i=tokens.Count-1;i>=0;i--){var t=tokens[i];var alias=BlueprintAliases.Find(t.Feature);if(alias==null||alias.Symbol.Length==0)continue;result.Remove(t.Start,t.Length).Insert(t.Start,english?alias.English:alias.Symbol);}
        return result.ToString();
    }
}

public partial class ForgeMvp
{
    bool showTitle=true;
    int titlePage,confirmSlot;
    string titleMessage="";
    Vector2 titleScroll;
    GUIStyle menuTitle,menuText,menuButton;
    void InitializeTitle()
    {
        // Keep the legacy copy intact; migrating never overwrites an existing slot.
        if(PlayerPrefs.HasKey(LegacySaveKey)&&!PlayerPrefs.HasKey(SlotKey(1))){
            PlayerPrefs.SetString(SlotKey(1),PlayerPrefs.GetString(LegacySaveKey));PlayerPrefs.Save();
        }
        ForgeAudioSettings.Load();
        if(PlayerPrefs.HasKey("MakeYourItem.Settings.Fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("MakeYourItem.Settings.Fullscreen")==1;
        InitializeDisplaySettings();
        showTitle=true;titlePage=0;progressLoaded=false;activeSaveSlot=0;DayCycle.Paused=true;RefreshTitleDisplay();
    }
    string SlotSummary(int slot)
    {
        if(!PlayerPrefs.HasKey(SlotKey(slot)))return "빈 슬롯";
        try {
            var saved=JsonUtility.FromJson<SavedProgress>(PlayerPrefs.GetString(SlotKey(slot)));
            if(saved==null||saved.version!=1||saved.day<1)throw new Exception();
            return "Day "+saved.day+" · "+saved.gold+"G · "+saved.diamonds+"D\n"+(string.IsNullOrEmpty(saved.savedAt)?"기존 저장 데이터":saved.savedAt);
        }catch{return "읽을 수 없는 저장 데이터";}
    }
    void EnterSavedSlot(int slot)
    {
        activeSaveSlot=slot;
        if(LoadProgress()){showTitle=false;DayCycle.Paused=false;titleMessage="";if(lesson.Crafted&&!lesson.Complete)OpenTutorial();}
        else {titleMessage=saveMessage;activeSaveSlot=0;}
        if(Event.current!=null)ClearUIFocus();
    }
    void StartNewSlot(int slot)
    {
        activeSaveSlot=slot;ResetProgressForDebug();
        if(saveMessage.StartsWith("저장 실패")){titleMessage=saveMessage;progressLoaded=false;return;}
        showTitle=false;DayCycle.Paused=false;titleMessage="";if(Event.current!=null)ClearUIFocus();
        OpenTutorial();
    }
    void ReturnToTitle()
    {
        if(busy||executionSession!=null)return;
        SaveProgress();
        if(saveMessage.StartsWith("저장 실패"))return;
        showProgress=false;showTitle=true;titlePage=0;titleMessage="";DayCycle.Paused=true;progressLoaded=false;RefreshTitleDisplay();
        if(Event.current!=null)ClearUIFocus();
    }
    void QuitFromTitle()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
    void DrawTitle()
    {
        if(menuTitle==null){
            menuTitle=new GUIStyle(GUI.skin.label){font=codeStyle.font,fontSize=36,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            menuTitle.normal.textColor=ForgeTitleStyle.Ink;
            menuText=new GUIStyle(GUI.skin.label){font=codeStyle.font,fontSize=18,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            menuText.normal.textColor=new Color(.78f,.83f,.89f);
            menuButton=new GUIStyle(GUI.skin.button){font=codeStyle.font,fontSize=22,wordWrap=true,padding=new RectOffset(12,12,10,10)};
            ForgeTitleStyle.Button(menuButton);
        }
        if(titlePage==4){DrawSettingsScreen();return;}
        ForgeTitleStyle.Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.04f,.07f,.11f,.18f));
        float width=Screen.width>=760?Mathf.Min(620,Screen.width*.46f):Mathf.Max(1,Screen.width-32);
        bool wide=Screen.width>=760;
        float topReserve=wide?16:130;
        float height=Mathf.Max(1,Mathf.Min(titlePage==0?460:700,Screen.height-topReserve-16));
        Rect panel=new Rect(wide?Screen.width-width-24:(Screen.width-width)/2,topReserve+(Screen.height-topReserve-16-height)/2,width,height);
        ForgeTitleStyle.Logo(new Rect(16,wide?Mathf.Max(16,Screen.height*.06f):16,wide?Screen.width*.5f-32:Screen.width-32,100),menuTitle);
        ForgeTitleStyle.Window(panel,titlePage!=0);
        var caption=new GUIStyle(menuText){fontSize=16,alignment=TextAnchor.MiddleLeft};
        if(titlePage!=0)GUI.Label(new Rect(panel.x+16,panel.y+3,panel.width-32,28),titlePage==3?"새 게임 확인":"저장 슬롯",caption);
        float inset=titlePage==0?24:48;
        GUILayout.BeginArea(new Rect(panel.x+24,panel.y+inset,panel.width-48,panel.height-inset-24));
        titleScroll=GUILayout.BeginScrollView(titleScroll);
        ForgeTextLayout.LayoutLabel("숫자로 도면을 그리고, 나만의 무기를 만드세요.",menuText);GUILayout.Space(24);
        if(titlePage==0){
            if(ForgeSoundEffects.LayoutButton("게임 시작",menuButton,GUILayout.MinHeight(58))){titlePage=1;titleMessage="";}
            GUILayout.Space(14);
            if(ForgeSoundEffects.LayoutButton("이어하기",menuButton,GUILayout.MinHeight(58))){titlePage=2;titleMessage="";}
            GUILayout.Space(14);
            if(ForgeSoundEffects.LayoutButton("설정",SettingsButton,GUILayout.MinHeight(58))){titlePage=4;RefreshDisplaySizes();settingsDropdown=0;settingsMessage="";}
            GUILayout.Space(14);
            if(ForgeSoundEffects.LayoutButton("종료",menuButton,GUILayout.MinHeight(58)))QuitFromTitle();
        }else if(titlePage==1||titlePage==2){
            bool fresh=titlePage==1;
            ForgeTextLayout.LayoutLabel(fresh?"새 게임을 저장할 슬롯을 선택하세요.":"이어할 슬롯을 선택하세요.",menuText);
            for(int slot=1;slot<=SlotCount;slot++){
                bool enabled=GUI.enabled;GUI.enabled=enabled&&(fresh||PlayerPrefs.HasKey(SlotKey(slot)));
                if(ForgeGoldGUI.LayoutButton("슬롯 "+slot+" · "+SlotSummary(slot),menuButton,GUILayout.MinHeight(86))){
                    if(fresh){confirmSlot=slot;titlePage=3;}else EnterSavedSlot(slot);
                }
                GUI.enabled=enabled;
                GUILayout.Space(12);
            }
            if(ForgeSoundEffects.LayoutButton("뒤로",menuButton))titlePage=0;
        }else if(titlePage==3){
            ForgeTextLayout.LayoutLabel("슬롯 "+confirmSlot+"에서 새 게임을 시작할까요?",new GUIStyle(menuTitle){fontSize=26});
            GUILayout.Space(16);
            ForgeTextLayout.LayoutLabel(PlayerPrefs.HasKey(SlotKey(confirmSlot))?"이 슬롯의 기존 진행 상황과 작성한 도면을 덮어씁니다. 되돌릴 수 없습니다. 다른 슬롯은 유지됩니다.":"100G와 철 10개로 Day 1 오전 8시에 시작합니다. 진행 상황은 선택한 슬롯에 저장됩니다.",menuText);
            GUILayout.Space(20);
            if(ForgeSoundEffects.LayoutButton("확인 · 새 게임 시작",menuButton,GUILayout.MinHeight(58)))StartNewSlot(confirmSlot);
            GUILayout.Space(14);
            if(ForgeSoundEffects.LayoutButton("취소",menuButton,GUILayout.MinHeight(58)))titlePage=1;
        }
        if(!string.IsNullOrEmpty(titleMessage)){GUILayout.Space(12);ForgeTextLayout.LayoutLabel(titleMessage,menuText);}
        GUILayout.EndScrollView();GUILayout.EndArea();
    }
}
