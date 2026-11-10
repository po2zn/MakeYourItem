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
        "\nx: "+ForgeBlacksmiths.X(id)+"   y: "+ForgeBlacksmiths.Y(id)+"\n호감도: "+blacksmiths.smiths[id].favor+"\n상태: "+(SmithBusy(id)?"작업 중":"대기 중");
    void DrawSmithHover()
    {
        if(!workspaceCamera||!workspaceCamera.enabled||WorkspacePointerActive||showGameMenu||showTitle)return;
        var point=Event.current.mousePosition;
        if(point.y<ToolbarHeight||DebugResourcesRect.Contains(point)||showDebugWindow&&debugWindow.Contains(point))return;
        foreach(var window in codeWindows){var r=window.rect;r.position+=workspaceOffset;r=new Rect(r.x*workspaceZoom,r.y*workspaceZoom,r.width*workspaceZoom,r.height*workspaceZoom);if(r.Contains(point))return;}
        if(showGuideBook&&guideBook.Contains(BookWorkspacePoint(point)))return;
        var sales=salesWindow;sales.position+=workspaceOffset;
        if(showSalesPanel&&sales.Contains(point/workspaceZoom))return;
        var ray=workspaceCamera.ScreenPointToRay(new Vector3(point.x,Screen.height-point.y,0));
        int closest=-1;float distance=float.MaxValue;
        for(int i=0;i<smithMotions.Count;i++)if(smithMotions[i].SmithBounds.IntersectRay(ray,out float hit)&&hit<distance){closest=i;distance=hit;}
        if(closest<0)return;
        SmithStyles();float width=Mathf.Min(320,Screen.width-16);
        GUI.Box(new Rect(Mathf.Clamp(point.x+18,8,Mathf.Max(8,Screen.width-width-8)),Mathf.Clamp(point.y+20,8,Mathf.Max(8,Screen.height-148)),width,140),SmithInfo(closest),smithTooltip);
    }
    void BuySmithResearch(ref bool unlocked,int price,string title)
    {
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!unlocked&&diamonds>=price;
        if(GUILayout.Button(title+" · "+(unlocked?"구매 완료":price+" 다이아"),smithButton)){
            diamonds-=price;unlocked=true;smithMessage=title+" 해금 완료";SaveProgress();
        }
        GUI.enabled=enabled;
    }
    void DrawSmithTree()
    {
        SmithStyles();var old=GUI.color;GUI.color=new Color(.07f,.11f,.17f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
        GUILayout.BeginArea(new Rect(16,12,Screen.width-32,Screen.height-24));
        GUILayout.BeginHorizontal();GUILayout.Label("대장장이 테크 트리 · 다이아 "+diamonds,smithLabel);
        if(GUILayout.Button("성장 트리",smithButton,GUILayout.Width(140)))showSmithTree=false;
        if(GUILayout.Button("대장간",smithButton,GUILayout.Width(120))){showSmithTree=false;showTechTree=false;}
        GUILayout.EndHorizontal();
        smithTreeScroll=GUILayout.BeginScrollView(smithTreeScroll);
        GUILayout.Label("기본 제공 → 선물 90901 · 병렬 명령 90904 · 인원 90905 · 자기 번호 90906 · 작업 여부 90907",smithLabel);
        GUILayout.Label("고용: 1 → 2 → 3 → 4 → 9 → 16명 / 대장장이마다 독립 실행 · 공용 재료 사용",smithLabel);
        int tier=blacksmiths.hireTier;bool enabled=GUI.enabled;GUI.enabled=enabled&&tier<5&&diamonds>=(tier<5?ForgeBlacksmiths.HireCosts[tier]:int.MaxValue);
        if(GUILayout.Button(tier<5?"고용 "+blacksmiths.smiths.Count+" → "+ForgeBlacksmiths.Counts[tier+1]+"명 · "+ForgeBlacksmiths.HireCosts[tier]+" 다이아":"고용 완료 · 16명",smithButton)){
            blacksmiths.Hire(ref diamonds,SmithHours);UpdateSmiths();SaveProgress();
        }
        GUI.enabled=enabled;
        BuySmithResearch(ref blacksmiths.favorUnlocked,25,"호감도 조회 90902()");
        BuySmithResearch(ref blacksmiths.positionUnlocked,25,"위치 조회 90903()");
        BuySmithResearch(ref blacksmiths.demandUnlocked,50,"요구 수량 조회 90909()");
        GUILayout.Label("선물: 일치 +5 / 불일치 -10 · 요구 수량은 매번 1~9 재추첨\n호감도: 게임 2시간마다 -1 · -100에서 파괴 10%, +100에서 대성공 10%\n매일 한 명이 2시간 동안 불타는 상태: 대성공 50% · 속도 변화 없음",smithLabel);
        GUILayout.Label("거대한 룬 · 90910(룬 번호, x, y)\n룬 각인 연구 후 수정 1개 + 다이아 25/50/75개. 자신과 상하좌우 한 칸에 적용.\n계열 1: 대성공 +1/2/3%p · 계열 2: 파괴 -1/2/3%p · 계열 3: 판매 +5/10/15%. 같은 계열은 최댓값만 적용.",smithLabel);
        foreach(var rune in blacksmiths.runes)GUILayout.Label("설치한 거대한 룬 "+rune.rune+" · ("+rune.x+", "+rune.y+")",smithLabel);
        GUILayout.Label("병렬 제작 예시 (제어문 연구 후):\n333 sword():\n    p = 1(1)\n    2(p, 7)\n    3(p)\n    4(p, 0)\n    5(p, 2)\n    << p\n90904(1, sword)\n90904(2, sword)\n\n제작 함수가 반환한 작업물은 자동 제출됩니다.\n조회와 선물: 90901(1, 90909(1))\n번호는 0부터 시작. 작업 중인 대상에 다시 명령하면 -1 반환. 실행 중인 코드는 시작 당시의 복사본입니다.",smithLabel);
        if(smithMessage.Length>0)GUILayout.Label(smithMessage,smithLabel);
        for(int i=0;i<blacksmiths.smiths.Count;i++){
            blacksmiths.Chances(i,SmithHours,out double destruction,out double great);
            GUILayout.Box(SmithInfo(i)+"\n파괴 "+(destruction*100).ToString("0.0")+"% · 대성공 "+(great*100).ToString("0.0")+"%",smithTooltip);
        }
        if(smithJobs.Count>0&&GUILayout.Button("병렬 작업 모두 중단",smithButton)){StopSmithJobs();smithMessage="이미 소비한 재료는 유지됩니다.";}
        GUILayout.EndScrollView();GUILayout.EndArea();
    }
}
