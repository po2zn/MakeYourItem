using System.Linq;
using UnityEngine;

// Ending is awarded from a committed product, never from a preview or source text.
public static class ForgeCampaign
{
    public const string FinalGoal="별무늬 별철 검 · 정제 용뼈 손잡이 · 충전 3회 · 서로 다른 계열 룬 3개 · 균열 없음";
    public static bool IsMasterwork(ForgeProduct product)
    {
        if(product==null||product.Kind!=ForgeProductKind.Sword||product.Material!=7)return false;
        var d=product.Data;
        return d.star&&d.handle==9&&d.charge>=3&&!d.cracked&&!d.destroyed&&!d.recovered
            &&d.runes.Count==3&&d.runes.All(r=>r>=1&&r<=9)&&d.runes.Select(r=>(r-1)%3).Distinct().Count()==3;
    }
    public static bool CanComplete(ForgeTechnology technology,ForgeProduct product)=>technology.Owns(ForgeTechnology.MasterCommission)&&IsMasterwork(product);
    public static string Objective(ForgeTechnology tech,int sales,bool completed)
    {
        if(completed)return "명장 달성 · 새로운 조합으로 자유 제작을 계속하세요.";
        if(!tech.Owns(ForgeTechnology.AutomationRoot))return "첫 연구: 기본 검을 제작·판매하고 제어문을 "+ForgeTechnology.Find(ForgeTechnology.AutomationRoot).Price+"G로 해금하세요. 이후 모든 분야에 투자할 수 있습니다.";
        if(sales==0)return "첫 목표: 기본 검을 만들어 판매하세요. 철·나무·물은 바로 구매할 수 있습니다.";
        if(!tech.Owns("material-5")&&!tech.Owns("material-6"))return "다음 목표: 금속(은 공급) 또는 마력(수정 공급) 분야에 투자하세요.";
        if(!tech.Owns("material-7"))return "금속 성장: 연마 → 은 공급 → 도금 → 별철 공급.";
        if(!tech.Owns(BlueprintFeature.Charge))return "마력 성장: 수정 공급 → 9() 충전.";
        if(!tech.Owns(BlueprintFeature.Refine))return "두 분야 결합: 별철 공급 + 충전 → 용뼈 공급 → 10() 정제.";
        if(!tech.Owns(BlueprintFeature.Rune))return "최종 준비: 211() 룬 각인을 해금하세요.";
        if(!tech.Owns(ForgeTechnology.MasterCommission))return "최종 목표: 명장의 마지막 의뢰를 "+ForgeTechnology.Find(ForgeTechnology.MasterCommission).Price+"G로 수락하세요.";
        return "최종 의뢰: "+FinalGoal;
    }
}
public partial class ForgeMvp
{
    bool campaignComplete,endingSeen;
    Vector2 endingScroll;
    GUIStyle endingTitle,endingBody,endingButton;
    string CampaignObjective=>ForgeCampaign.Objective(technology,totalSales,campaignComplete);
    void DrawCampaignProgress()
    {
        GUILayout.Space(12);
        ForgeTextLayout.LayoutLabel("성장 목표 · "+CampaignObjective);
        ForgeTextLayout.LayoutLabel("공통 시작: 기본 제작 → 제어문 해금("+ForgeTechnology.Find(ForgeTechnology.AutomationRoot).Price+"G) → 다섯 분야로 분기");
        ForgeTextLayout.LayoutLabel("금속: 열 조회 → 연마 → 은 → 도금 → 별철 → 용뼈 → 정제");
        ForgeTextLayout.LayoutLabel("마력: 수정 → 충전 → 룬 / 보호 · 상태 조회 → 마력 단조 → 공명");
        ForgeTextLayout.LayoutLabel("운영: 석탄 → 연료 → 봉인 → 스피드 1~5단계 → 자동판매 무료 해금");
        ForgeTextLayout.LayoutLabel("기술 연구: 절댓값 → 최솟값 → 최댓값 / 길이 → 집합 → 난수");
        ForgeTextLayout.LayoutLabel("최종 의뢰: "+ForgeCampaign.FinalGoal);
        ForgeTextLayout.LayoutLabel("의뢰 "+(technology.Owns(ForgeTechnology.MasterCommission)?"수락 완료":"미수락")+" · 엔딩 "+(campaignComplete?"달성":"미달성"));
        if(GUILayout.Button("성장 트리 열기")){showProgress=false;OpenTechTree();}
        if(campaignComplete&&GUILayout.Button("엔딩 다시 보기")){showProgress=false;endingSeen=false;}
    }
    void DrawCampaignEnding()
    {
        if(endingTitle==null){
            endingTitle=new GUIStyle(GUI.skin.label){font=codeStyle.font,fontSize=34,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            endingTitle.normal.textColor=new Color(1,.85f,.5f);
            endingBody=new GUIStyle(endingTitle){fontSize=19,alignment=TextAnchor.UpperLeft};
            endingBody.normal.textColor=Color.white;
            endingButton=new GUIStyle(GUI.skin.button){font=codeStyle.font,fontSize=20,wordWrap=true};
        }
        Color old=GUI.color;GUI.color=new Color(.055f,.08f,.12f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
        float width=Mathf.Min(680,Mathf.Max(1,Screen.width-40));
        GUILayout.BeginArea(new Rect((Screen.width-width)/2,24,width,Mathf.Max(1,Screen.height-48)));
        endingScroll=GUILayout.BeginScrollView(endingScroll);
        GUILayout.Space(40);
        ForgeTextLayout.LayoutLabel("당신의 도면이 전설이 되었습니다",endingTitle);
        GUILayout.Space(24);
        ForgeTextLayout.LayoutLabel("작은 철 조각으로 시작한 공방에서, 별철과 용뼈, 마력과 룬을 하나의 검으로 완성했습니다.\n\n재료의 성질을 읽고 코드로 연결한 당신은 이제 공방의 명장입니다.",endingBody);
        GUILayout.Space(20);
        ForgeTextLayout.LayoutLabel("명장의 마지막 의뢰 완료\n"+ForgeCampaign.FinalGoal+"\n\nDay "+DayCycle.Day+" · 판매 "+totalSales+"개 · 누적 판매액 "+earnedGold+"G",endingBody);
        GUILayout.Space(28);
        if(GUILayout.Button("계속 제작하기",endingButton,GUILayout.MinHeight(52))){endingSeen=true;SaveProgress();}
        GUILayout.Space(10);
        if(GUILayout.Button("저장하고 타이틀로",endingButton,GUILayout.MinHeight(52))){endingSeen=true;SaveProgress();ReturnToTitle();}
        if(saveMessage.StartsWith("저장 실패"))ForgeTextLayout.LayoutLabel(saveMessage,endingBody);
        GUILayout.EndScrollView();GUILayout.EndArea();
    }
}
