using System;
using System.Collections.Generic;
using UnityEngine;

public partial class ForgeMvp
{
    const string SaveKey="MakeYourItem.Progress.v1";
    int worldSeed=72431,diamonds,totalSales,tutorialStage;
    long earnedGold;
    float craftSpeed=1;
    bool autoSell,showProgress,progressLoaded;
    string saveMessage="";
    readonly HashSet<string> discoveries=new HashSet<string>();
    Vector2 progressScroll;
    [Serializable] public class SavedWindow { public string name,text; public Rect rect; public uint seed; }
    [Serializable] public class SavedItem { public string owner; public int material,heat,iron,cost;public bool shaped;public ForgeTraits traits;public ForgeItemData data;public double seconds; }
    [Serializable] public class SavedProgress {
        public int version=1,gold,iron,day,seed,diamonds,sales,tutorial;public long earned;public double hour;public float speed;public bool autoSell;
        public ForgeMaterialStock[] stocks;public BlueprintFeature[] features;public string[] discoveries;
        public List<SavedWindow> windows=new List<SavedWindow>();public List<SavedItem> items=new List<SavedItem>();
    }
    void LoadProgress()
    {
        progressLoaded=true;
        if(!PlayerPrefs.HasKey(SaveKey)){worldSeed=Guid.NewGuid().GetHashCode();return;}
        try {
            var saved=JsonUtility.FromJson<SavedProgress>(PlayerPrefs.GetString(SaveKey));
            if(saved==null||saved.version!=1||saved.gold<0||saved.iron<0||saved.diamonds<0||saved.day<1||saved.hour<0||saved.hour>=24)throw new Exception("저장 형식이 올바르지 않습니다.");
            // Parse products before applying state so an invalid product cannot half-load a save.
            var items=new List<ForgeProduct>();foreach(var item in saved.items)items.Add(new ForgeProduct(item.owner,item.heat,item.shaped,item.traits,item.cost,item.iron,item.material,item.data,item.seconds));
            gold=saved.gold;iron=saved.iron;worldSeed=saved.seed;diamonds=saved.diamonds;totalSales=saved.sales;tutorialStage=saved.tutorial;earnedGold=saved.earned;
            craftSpeed=Mathf.Clamp(saved.speed,1,5);autoSell=saved.autoSell;materialStocks=saved.stocks??Array.Empty<ForgeMaterialStock>();technology.Restore(saved.features);
            DayCycle.Clock.Restore(saved.day,saved.hour);
            foreach(var entry in saved.discoveries??Array.Empty<string>())discoveries.Add(entry);
            foreach(var item in items)shop.Add(item);
            if(saved.windows!=null&&saved.windows.Count>0){codeWindows.Clear();nextWindowId=0;foreach(var window in saved.windows)codeWindows.Add(new CodeWindow{id=nextWindowId++,name=window.name,text=window.text,rect=window.rect,seed=window.seed});windowsInitialized=true;activeWindow=0;source=codeWindows[0].text;}
            saveMessage="저장된 공방을 불러왔습니다.";
        }catch(Exception error){saveMessage="저장 불러오기 실패: "+error.Message;progressLoaded=false;}
    }
    void SaveProgress()
    {
        if(!progressLoaded||DayCycle==null)return;
        try {
            var saved=new SavedProgress{gold=gold,iron=iron,seed=worldSeed,diamonds=diamonds,sales=totalSales,tutorial=tutorialStage,earned=earnedGold,day=DayCycle.Day,hour=DayCycle.CurrentHour,speed=craftSpeed,autoSell=autoSell,stocks=materialStocks,features=technology.Export(),discoveries=new List<string>(discoveries).ToArray()};
            for(int i=0;i<codeWindows.Count;i++){var w=codeWindows[i];saved.windows.Add(new SavedWindow{name=w.name,text=i==activeWindow?source:w.text,rect=w.rect,seed=w.seed});}
            foreach(var entry in shop.Items){var p=entry.Product;saved.items.Add(new SavedItem{owner=p.BlueprintName,material=p.Material,heat=p.FinalHeat,shaped=p.Shaped,traits=p.Traits,cost=p.CashCost,iron=p.IronConsumed,data=p.Data,seconds=p.ProductionSeconds});}
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(saved));PlayerPrefs.Save();saveMessage="공방 저장 완료";
        }catch(Exception error){saveMessage="저장 실패: "+error.Message;}
    }
    void OnApplicationQuit(){SaveProgress();}
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
        diamonds=checked(diamonds+product.DiamondReward);
        foreach(var bonus in ForgePricing.Bonuses(product))if(bonus.Value>0)discoveries.Add(bonus.Key);
        if(product.Kind!=ForgeProductKind.Unfinished)tutorialStage=Math.Max(tutorialStage,5);
    }
    bool SellProductInternal(int id)
    {
        if(!shop.TrySell(id,ref gold,out var sold))return false;
        reportError=false;totalSales++;earnedGold+=ForgePricing.Quote(sold).SalePrice;tutorialStage=Math.Max(tutorialStage,6);selectedProductId=-1;
        resultOwner=sold.BlueprintName;report= "판매 영수증 · #"+id+"\n"+ForgePricing.Describe(sold,false);return true;
    }
    void ProgressStep(Step step){if(step.process>=1&&step.process<=4)tutorialStage=Math.Max(tutorialStage,step.process);}
    void DrawProgressButton()
    {
        if(GUI.Button(new Rect(12,Screen.height-36,190,28),"공방 기록 · 다이아 "+diamonds))showProgress=true;
    }
    void DrawProgress()
    {
        GUI.Box(new Rect(0,0,Screen.width,Screen.height),"");
        GUILayout.BeginArea(new Rect(20,20,Mathf.Max(100,Screen.width-40),Mathf.Max(100,Screen.height-40)));
        if(GUILayout.Button("← 대장간으로 돌아가기")){showProgress=false;}
        progressScroll=GUILayout.BeginScrollView(progressScroll);
        GUILayout.Label("공방 기록 · Day "+DayCycle.Day+" · "+((int)DayCycle.CurrentHour).ToString("00")+":"+((int)(DayCycle.CurrentHour*60)%60).ToString("00"));
        GUILayout.Label("골드 "+gold+"G · 다이아몬드 "+diamonds+" · 판매 "+totalSales+"개 · 누적 판매액 "+earnedGold+"G");
        GUILayout.Label("가속 "+craftSpeed+"배 · 자동판매 "+(autoSell?"켜짐":"꺼짐"));
        string[] tutorial={"1. 기본 도면에서 재료를 올리세요.","2. 가열해 열을 6 이상으로 만드세요.","3. 타격으로 검날을 성형하세요.","4. 냉각하세요.","5. !작업물로 제출하세요.","6. 상점에서 검날을 판매하세요.","기본 튜토리얼 완료! 물 냉각과 나무 손잡이로 더 가치 있는 검을 만들어 보세요."};
        GUILayout.Label(tutorial[Mathf.Clamp(tutorialStage,0,6)]);
        GUILayout.Label(earnedGold>=5000&&diamonds>=5?"공방의 명장 달성! 자유 제작을 계속할 수 있습니다.":"명장 목표: 누적 판매 5000G · 다이아몬드 5개");
        GUILayout.Label("발견 도감");
        if(discoveries.Count==0)GUILayout.Label("아직 발견한 특성이 없습니다.");
        foreach(var discovery in discoveries)GUILayout.Label(discovery+" · "+DiscoveryRule(discovery));
        GUILayout.Label("미정 기획의 현재 규칙: 행운 완성 검은 판매 +30G, 제작 시 다이아 1개. 다이아는 재료 구매와 분리된 수집 재화입니다.");
        bool old=GUI.enabled;GUI.enabled=!busy&&executionSession==null;
        if(GUILayout.Button("공방 저장"))SaveProgress();
        if(GUILayout.Button("현재 도면을 숫자 표기로 변환"))TranslateCurrent(false);
        if(GUILayout.Button("현재 도면을 영문 표기로 변환"))TranslateCurrent(true);
        GUI.enabled=old;
        GUILayout.Label(saveMessage);
        GUILayout.Label("제작·판매·구매 후 자동 저장. 실행 도중 종료하면 지불한 재료는 유지되며 실행 스택과 작업 중 물체는 복원하지 않습니다. 종료 중에는 시간이 흐르지 않습니다.");
        GUILayout.EndScrollView();GUILayout.EndArea();
    }
    static string DiscoveryRule(string name)
    {
        switch(name){
            case "담금질 경도 7":return "열 6 이상인 성형 검날을 물(3)로 냉각";
            case "손잡이":return "성형하고 식힌 검날에 철·나무·은·별철로 손잡이 제작, 또는 정제 용뼈 장착";
            case "은도금":return "검날을 식힌 뒤 연마하고 은도금";
            case "별무늬":return "성형한 별철 검날이 열 7일 때 냉각";
            case "서리 문양":return "검날의 열과 서리수의 열 4의 차이가 3 이하";
            case "마력 전도":return "손잡이에 약한 마력(1)을 3회 주입";
            case "용의 맥동":return "용뼈 손잡이와 충전된 수정 조합";
            case "공명 수정":return "숨은 열을 찾아 활성화한 수정을 식혀 장착";
            case "결계":return "봉인층을 손상 없이 역순으로 해제";
            case "마력 단조":return "시간에 따라 감소하는 강도를 냉각 시 확정";
            case "삼중 공명":return "서로 다른 계열의 룬 3개 각인";
            case "오늘의 행운":return "778() 코드로 777(코드,완성 검) 적용 후 제출";
            case "저주 배율":return "666() 적용 후 파괴되지 않은 작업물";
            default:return "룬 번호의 계열과 등급에 따른 각인 보너스";
        }
    }
    void TranslateCurrent(bool english)
    {
        source=ForgeCodeTranslation.Translate(source,english);
        if(codeWindows.Count>0)codeWindows[activeWindow].text=source;
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
