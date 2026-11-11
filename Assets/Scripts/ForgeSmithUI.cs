using System;
using UnityEngine;

public partial class ForgeMvp
{
    GUIStyle smithLabel,smithButton,smithTooltip;
    void SmithStyles()
    {
        if(smithLabel!=null&&smithLabel.fontSize==24&&smithButton!=null&&smithButton.fontSize==24&&smithTooltip!=null&&smithTooltip.fontSize==22)return;
        smithLabel=new GUIStyle(GUI.skin.label){font=codeStyle.font,fontSize=24,wordWrap=true};smithLabel.normal.textColor=new Color(.65f,.86f,1);smithLabel.hover=smithLabel.normal;
        smithButton=new GUIStyle(GUI.skin.button){font=codeStyle.font,fontSize=24,wordWrap=true,padding=new RectOffset(12,12,10,10)};
        smithTooltip=new GUIStyle(GUI.skin.box){font=codeStyle.font,fontSize=22,alignment=TextAnchor.MiddleLeft,wordWrap=true,padding=new RectOffset(12,12,8,8)};
        smithTooltip.normal.textColor=Color.white;smithTooltip.hover=smithTooltip.normal;
    }
    string SmithInfo(int id)=>"대장장이 #"+id+(blacksmiths.Burning(id,SmithHours)?" · 불타는 대장장이":"")+
        "\nx: "+ForgeBlacksmiths.X(id)+"   y: "+ForgeBlacksmiths.Y(id)+"\n호감도: "+blacksmiths.smiths[id].favor+"\n상태: "+(SmithBusy(id)?"작업 중":"대기 중")+
        (blacksmiths.Burning(id,SmithHours)?"\n불타는 대장장이 · 대성공 확률 50%\n남은 시간: "+Math.Ceiling(Math.Max(0,blacksmiths.burningUntil-SmithHours)*60)+"분 (게임 시간)":"");
    public string SmithHoverInfo(Vector2 screenPoint)
    {
        if(!workspaceCamera||!workspaceCamera.enabled||showTitle||showTechTree||showGameMenu||showProgress||showTutorial||deleteWindowId>=0||campaignComplete&&!endingSeen)return null;
        var ray=workspaceCamera.ScreenPointToRay(screenPoint);int closest=-1;float distance=float.MaxValue;
        for(int i=0;i<smithMotions.Count&&i<blacksmiths.smiths.Count;i++)if(smithMotions[i].SmithBounds.IntersectRay(ray,out float hit)&&hit<distance){closest=i;distance=hit;}
        return closest<0?null:SmithInfo(closest);
    }
    void DrawSmithHover()
    {
        if(!workspaceCamera||!workspaceCamera.enabled||WorkspacePointerActive||showGameMenu||showTitle)return;
        var point=Event.current.mousePosition;
        if(ToolbarContains(point)||DebugResourcesRect.Contains(point)||showDebugWindow&&debugWindow.Contains(point))return;
        foreach(var window in codeWindows){var r=window.rect;r.position+=workspaceOffset;r=new Rect(r.x*workspaceZoom,r.y*workspaceZoom,r.width*workspaceZoom,r.height*workspaceZoom);if(r.Contains(point))return;}
        if(showGuideBook&&guideBook.Contains(BookWorkspacePoint(point)))return;
        var sales=salesWindow;sales.position+=workspaceOffset;
        if(showSalesPanel&&sales.Contains(point/workspaceZoom))return;
        var ray=workspaceCamera.ScreenPointToRay(new Vector3(point.x,Screen.height-point.y,0));
        int closest=-1;float distance=float.MaxValue;
        for(int i=0;i<smithMotions.Count;i++)if(smithMotions[i].SmithBounds.IntersectRay(ray,out float hit)&&hit<distance){closest=i;distance=hit;}
        if(closest<0)return;
        SmithStyles();float width=Mathf.Min(320,Screen.width-16);
        ForgeTreeTooltip.Draw(point,"대장장이 정보",SmithInfo(closest),codeStyle.font);
    }
    bool SmithNodeOwned(int id)=>id==0||id<=5&&blacksmiths.hireTier>=id||id==6&&blacksmiths.favorUnlocked||id==7&&blacksmiths.positionUnlocked||id==8&&blacksmiths.demandUnlocked;
    int SmithNodePrice(int id)=>id==0?0:id<=5?ForgeBlacksmiths.HireCosts[id-1]:id==8?ForgeBlacksmiths.DemandResearchCost:id==7?ForgeBlacksmiths.PositionResearchCost:ForgeBlacksmiths.FavorResearchCost;
    string SmithNodeTitle(int id)=>id==0?"기본 대장장이 · 1명":id<=5?"대장장이 "+ForgeBlacksmiths.Counts[id]+"명 고용":id==6?"호감도 조회 · 90902()":id==7?"위치 조회 · 90903()":"요구 수량 조회 · 90909()";
    string SmithNodeDescription(int id)=>id==0?"선물 90901 · 병렬 명령 90904 · 인원 90905 · 자기 번호 90906 · 작업 여부 90907은 기본 제공됩니다.\n번호는 0부터 시작하며, 각 대장장이는 독립 실행하고 공용 재료를 사용합니다.":id<=5?"고용 인원이 "+ForgeBlacksmiths.Counts[id-1]+"명에서 "+ForgeBlacksmiths.Counts[id]+"명으로 증가합니다.\n90904(번호, 함수)로 병렬 제작을 맡길 수 있습니다. 제작 함수가 반환한 작업물은 자동 제출됩니다.":id==6?"90902([번호])로 호감도 -100~100을 조회합니다.\n호감도는 게임 시간 2시간마다 1 감소합니다.":id==7?"90903([번호])로 공방 격자 좌표 (x, y)를 조회합니다.\n첫 대장장이의 위치는 (0, 0)입니다.":"90909([번호])로 요구 다이아 수량 1~9를 조회합니다.\n선물 수량이 일치하면 호감도 +5, 불일치하면 -10. 요구 수량은 선물마다 다시 정해집니다.";
    void PurchaseSmithNode(int id)
    {
        if(SmithNodeOwned(id)){smithMessage="이미 구매한 항목입니다.";return;}
        if(id<=5&&blacksmiths.hireTier!=id-1){smithMessage="선행 구매 필요: "+SmithNodeTitle(id-1);return;}
        int price=SmithNodePrice(id);
        if(diamonds<price){smithMessage="다이아가 부족합니다.";return;}
        if(id<=5){if(!blacksmiths.Hire(ref diamonds,SmithHours))return;UpdateSmiths();}
        else{diamonds-=price;if(id==6)blacksmiths.favorUnlocked=true;else if(id==7)blacksmiths.positionUnlocked=true;else blacksmiths.demandUnlocked=true;}
        smithMessage=SmithNodeTitle(id)+" 구매 완료";SaveProgress();
    }
}
