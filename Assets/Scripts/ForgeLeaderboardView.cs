using System;
using UnityEngine;
using UnityEngine.UI;

// All layout, fonts, colors and rows are authored in the Resources prefab.
public sealed class ForgeLeaderboardView : MonoBehaviour {
 [Header("Inspector references — edit the prefab to change layout")]
 public Button backButton, syncButton;
 public InputField nameInput;
 public ScrollRect scroll;
 public Text bestText, nameNotice, statusText, updatedText;
 public Text[] rows;
 [Header("Display text")]
 [TextArea] public string registeredNotice="등록된 이름입니다. 브라우저 데이터를 삭제하면 이 사용자 기록에 다시 연결하지 못할 수 있습니다.";
 [TextArea] public string newNotice="이름과 최고 판매 금액이 다른 플레이어에게 공개됩니다. 등록 후 이름은 고정됩니다.";
 public string bestPrefix="내 최고 기록 · ", updatedPrefix="마지막으로 불러온 순위 · ";
 public string noScores="아직 불러온 순위가 없습니다.", syncLabel="기록 동기화 · 순위 새로고침", busyLabel="동기화 중…";
 ForgeLeaderboardClient client;
 Action closed;
 public void Bind(ForgeLeaderboardClient value,Action onClose){
  client=value;closed=onClose;
  backButton.onClick.RemoveListener(Back);backButton.onClick.AddListener(Back);
  syncButton.onClick.RemoveListener(Sync);syncButton.onClick.AddListener(Sync);
  nameInput.onValueChanged.RemoveListener(NameChanged);nameInput.onValueChanged.AddListener(NameChanged);
  scroll.verticalNormalizedPosition=1;Refresh();
 }
 void Back(){ForgeSoundEffects.Click();client.Close();gameObject.SetActive(false);closed?.Invoke();}
 void Sync(){ForgeSoundEffects.Click();client.Sync();Refresh();}
 void NameChanged(string value){if(client?.Data!=null&&!client.Data.nameRegistered)client.Data.name=value;}
 void Update(){if(client==null||!client.Opened){gameObject.SetActive(false);return;}Refresh();}
 static void Set(Text label,string value){if(label.text!=value)label.text=value;}
 public void Refresh(){
  if(client?.Data==null)return;
  Set(bestText,bestPrefix+ForgeLeaderboardData.Best.ToString("N0")+"G");
  if(!nameInput.isFocused)nameInput.SetTextWithoutNotify(client.Data.name??"");
  nameInput.interactable=!client.Busy&&!client.Data.nameRegistered;
  Set(nameNotice,client.Data.nameRegistered?registeredNotice:newNotice);
  syncButton.interactable=!client.Busy&&client.Settings&&client.Settings.Ready;
  Set(syncButton.GetComponentInChildren<Text>(),client.Busy?busyLabel:syncLabel);
  Set(statusText,client.Status);
  Set(updatedText,string.IsNullOrEmpty(client.Data.updatedAt)?noScores:updatedPrefix+client.Data.updatedAt);
  var scores=client.Data.cached?.results;
  for(int i=0;i<rows.Length;i++){
   var entry=scores!=null&&i<scores.Length?scores[i]:null;
   bool valid=entry!=null&&!double.IsNaN(entry.score)&&!double.IsInfinity(entry.score)&&entry.score>=0;
   if(rows[i].gameObject.activeSelf!=valid)rows[i].gameObject.SetActive(valid);
   if(valid)Set(rows[i],(entry.rank+1)+"위 · "+(entry.Name.Length>0?entry.Name:"이름 없음")+" · "+entry.score.ToString("N0")+"G");
  }
 }
}
