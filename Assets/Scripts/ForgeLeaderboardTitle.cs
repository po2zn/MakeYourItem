using UnityEngine;
public partial class ForgeMvp {
 ForgeLeaderboardClient leaderboard;
 [SerializeField,Tooltip("Resources/ForgeLeaderboardUI prefab. All child controls are editable in its Inspector.")]
 ForgeLeaderboardView leaderboardPrefab;
 ForgeLeaderboardView leaderboardView;
 void OpenLeaderboard(){
  if(!leaderboard)leaderboard=gameObject.AddComponent<ForgeLeaderboardClient>();
  titlePage=5;leaderboard.Open();ShowLeaderboardView();
 }
 void ShowLeaderboardView(){
  if(!leaderboardView){
   var prefab=leaderboardPrefab?leaderboardPrefab:Resources.Load<ForgeLeaderboardView>("ForgeLeaderboardUI");
   if(!prefab){Debug.LogError("ForgeLeaderboardUI prefab missing");return;}
   leaderboardView=Instantiate(prefab,transform);leaderboardView.name="Leaderboard UI";
  }
  if(!leaderboardView.gameObject.activeSelf)leaderboardView.gameObject.SetActive(true);
  leaderboardView.Bind(leaderboard,()=>titlePage=0);
 }
 void DrawLeaderboard(){if(!leaderboardView||!leaderboardView.gameObject.activeSelf)ShowLeaderboardView();}
}
