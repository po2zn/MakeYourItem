using UnityEngine;
public partial class ForgeMvp {
 ForgeLeaderboardClient leaderboard;Vector2 leaderboardScroll;
 void OpenLeaderboard(){if(!leaderboard)leaderboard=gameObject.AddComponent<ForgeLeaderboardClient>();titlePage=5;leaderboardScroll=Vector2.zero;leaderboard.Open();}
 void DrawLeaderboard(){
  var panel=new Rect(16,16,Mathf.Max(1,Screen.width-32),Mathf.Max(1,Screen.height-32));ForgeTitleStyle.Window(panel);
  var text=new GUIStyle(menuText){alignment=TextAnchor.UpperLeft,richText=false};var heading=new GUIStyle(menuTitle){fontSize=26,richText=false};
  GUILayout.BeginArea(new Rect(panel.x+16,panel.y+12,panel.width-32,panel.height-24));
  if(ForgeSoundEffects.LayoutButton("← 메인 메뉴",menuButton,GUILayout.MinHeight(44))){leaderboard.Close();titlePage=0;}
  leaderboardScroll=GUILayout.BeginScrollView(leaderboardScroll);
  ForgeTextLayout.LayoutLabel("최고 판매가 리더보드",heading);GUILayout.Space(12);
  ForgeTextLayout.LayoutLabel("내 최고 기록 · "+ForgeLeaderboardData.Best.ToString("N0")+"G",text);
  ForgeTextLayout.LayoutLabel("모든 판매 배율을 포함한 검 한 자루의 실제 판매 금액입니다. 플레이 중에는 전송하지 않으며 이 화면을 열 때 동기화합니다.",text);GUILayout.Space(12);
  if(leaderboard.Data!=null){
   ForgeTextLayout.LayoutLabel("공개 이름 (1~16자)",text);
   bool enabled=GUI.enabled;GUI.enabled=enabled&&!leaderboard.Busy&&!leaderboard.Data.nameRegistered;
   var input=new GUIStyle(GUI.skin.textField){font=menuText.font,fontSize=22,padding=new RectOffset(10,10,8,8)};
   leaderboard.Data.name=GUILayout.TextField(leaderboard.Data.name??"",16,input,GUILayout.MinHeight(48));GUI.enabled=enabled;
   ForgeTextLayout.LayoutLabel(leaderboard.Data.nameRegistered?"등록된 이름입니다. 브라우저 데이터를 삭제하면 이 사용자 기록에 다시 연결하지 못할 수 있습니다.":"이름과 최고 판매 금액이 다른 플레이어에게 공개됩니다. 등록 후 이름은 고정됩니다.",text);
   GUI.enabled=enabled&&!leaderboard.Busy&&leaderboard.Settings&&leaderboard.Settings.Ready;
   if(ForgeSoundEffects.LayoutButton(leaderboard.Busy?"동기화 중…":"기록 동기화 · 순위 새로고침",menuButton,GUILayout.MinHeight(48)))leaderboard.Sync();GUI.enabled=enabled;
   ForgeTextLayout.LayoutLabel(leaderboard.Status,text);GUILayout.Space(16);
   ForgeTextLayout.LayoutLabel(string.IsNullOrEmpty(leaderboard.Data.updatedAt)?"아직 불러온 순위가 없습니다.":"마지막으로 불러온 순위 · "+leaderboard.Data.updatedAt,text);
   foreach(var entry in leaderboard.Data.cached?.results??System.Array.Empty<ForgeLeaderboardEntry>()){
    if(entry==null||double.IsNaN(entry.score)||double.IsInfinity(entry.score)||entry.score<0)continue;
    ForgeTextLayout.LayoutLabel((entry.rank+1)+"위 · "+(entry.Name.Length>0?entry.Name:"이름 없음")+" · "+entry.score.ToString("N0")+"G",text);GUILayout.Space(8);
   }
  }
  GUILayout.EndScrollView();GUILayout.EndArea();
 }
}
