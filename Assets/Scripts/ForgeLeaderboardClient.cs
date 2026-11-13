using System;using System.Collections;using System.Collections.Generic;using System.Text;using UnityEngine;using UnityEngine.Networking;
// No initialization or network traffic until the title leaderboard is opened.
public sealed class ForgeLeaderboardClient : MonoBehaviour {
 [Serializable] sealed class Login {public string userId,idToken,sessionToken;}
 [Serializable] sealed class Session {public string sessionToken;}
 [Serializable] sealed class Submission {public int score;public ForgeLeaderboardName metadata;}
 public ForgeLeaderboardSettings Settings{get;private set;}public ForgeLeaderboardData Data{get;private set;}
 public bool Busy{get;private set;}public string Status{get;private set;}="";public bool Opened{get;private set;}
#if UNITY_EDITOR
 public Func<string,string,string,Action<long,string>,IEnumerator> TestRequest;
#endif
 Coroutine routine;UnityWebRequest active;float nextAttempt;string token;bool failed;long status;string response;
 public void Open(ForgeLeaderboardSettings settings=null){Settings=settings?settings:Resources.Load<ForgeLeaderboardSettings>("ForgeLeaderboardSettings");Data=ForgeLeaderboardData.Load(Settings?Settings.Key:"unconfigured");Opened=true;Status="인터넷에 연결하면 저장된 최고 기록을 등록하고 순위를 불러옵니다.";if(!Settings||!Settings.Ready)Status="리더보드는 준비 중입니다. 내 최고 판매 기록은 기기에 저장됩니다.";else if(ForgeLeaderboardData.CleanName(Data.name).Length>0)Sync();}
 public void Close(){Opened=false;if(active!=null)active.Abort();if(routine!=null)StopCoroutine(routine);routine=null;active?.Dispose();active=null;Busy=false;token=null;}
 void OnDisable(){Close();}
 public void Sync(){if(!Opened||Busy||Time.realtimeSinceStartup<nextAttempt||!Settings||!Settings.Ready)return;Data.name=ForgeLeaderboardData.CleanName(Data.name);if(Data.name.Length==0){Status="공개할 이름을 1~16자로 입력하세요.";return;}Data.Save(Settings.Key);Busy=true;failed=false;nextAttempt=Time.realtimeSinceStartup+5;routine=StartCoroutine(Guard(Synchronize()));}
 IEnumerator Guard(IEnumerator work){var stack=new Stack<IEnumerator>();stack.Push(work);while(stack.Count>0&&Opened){var item=stack.Peek();bool more;try{more=item.MoveNext();}catch{Status="응답을 처리하지 못했습니다. 기록은 보존됩니다. 다시 시도해주세요.";break;}if(!more){(item as IDisposable)?.Dispose();stack.Pop();continue;}if(item.Current is IEnumerator nested)stack.Push(nested);else yield return item.Current;}Busy=false;routine=null;token=null;}
 IEnumerator Request(string method,string url,string body=null,bool auth=false,bool missingOkay=false){
#if UNITY_EDITOR
  if(TestRequest!=null){yield return TestRequest(method,url,body,(code,text)=>{status=code;response=text;});if(status<200||status>=300){if(!(missingOkay&&status==404)){failed=true;Status="인터넷 연결을 확인해주세요. 내 기록과 이전 순위는 보존됩니다.";}}yield break;}
#endif
  using(var request=new UnityWebRequest(url,method)){active=request;request.timeout=15;request.downloadHandler=new DownloadHandlerBuffer();if(body!=null){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));request.SetRequestHeader("Content-Type","application/json");}
   if(auth){request.SetRequestHeader("ProjectId",Settings.projectId);request.SetRequestHeader("UnityEnvironment",Settings.environment);}else request.SetRequestHeader("Authorization","Bearer "+token);
   yield return request.SendWebRequest();status=request.responseCode;response=request.downloadHandler.text;active=null;
   if(request.result!=UnityWebRequest.Result.Success&&!(missingOkay&&status==404)){failed=true;Status=status==429?"요청이 많습니다. 잠시 후 새로고침해주세요.":status==401?"인증을 갱신하지 못했습니다. 다시 시도해주세요.":status==403||status==404?"리더보드 연결 설정을 확인해야 합니다. 내 기록은 보존됩니다.":"인터넷 연결을 확인해주세요. 내 기록과 이전 순위는 보존됩니다.";}
  }
 }
 IEnumerator Synchronize(){
  Status="저장된 최고 기록을 동기화하고 있습니다…";
  string auth="https://player-auth.services.api.unity.com/v1/authentication/";
  bool returning=!string.IsNullOrEmpty(Data.sessionToken);
  yield return Request("POST",auth+(returning?"session-token":"anonymous"),returning?JsonUtility.ToJson(new Session{sessionToken=Data.sessionToken}):"{}",true);if(failed)yield break;
  var login=JsonUtility.FromJson<Login>(response);if(string.IsNullOrEmpty(login.userId)||string.IsNullOrEmpty(login.idToken)||string.IsNullOrEmpty(login.sessionToken))throw new Exception();
  Data.playerId=login.userId;Data.sessionToken=login.sessionToken;Data.Save(Settings.Key);token=login.idToken;
  string url="https://leaderboards.services.api.unity.com/v1/projects/"+Uri.EscapeDataString(Settings.projectId)+"/leaderboards/"+Uri.EscapeDataString(Settings.leaderboardId)+"/scores";
  string player=url+"/players/"+Uri.EscapeDataString(Data.playerId);
  yield return Request("GET",player+"?includeMetadata=true",missingOkay:true);if(failed)yield break;
  double remote=0;if(status!=404){var own=JsonUtility.FromJson<ForgeLeaderboardEntry>(response);remote=own.score;if(double.IsNaN(remote)||double.IsInfinity(remote)||remote<0)throw new Exception();Data.nameRegistered=true;if(!string.IsNullOrEmpty(own.metadata?.name))Data.name=own.Name;Data.Save(Settings.Key);}
  int best=ForgeLeaderboardData.Best;
  if(best>remote){yield return Request("POST",player,JsonUtility.ToJson(new Submission{score=best,metadata=new ForgeLeaderboardName{name=Data.name}}));if(failed)yield break;var saved=JsonUtility.FromJson<ForgeLeaderboardEntry>(response);if(saved.score<best)throw new Exception();Data.nameRegistered=true;Data.Save(Settings.Key);}
  yield return Request("GET",url+"?limit=20&includeMetadata=true");if(failed)yield break;
  var scores=JsonUtility.FromJson<ForgeLeaderboardScores>(response);if(scores?.results==null)throw new Exception();Data.cached=scores;Data.updatedAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm");Data.Save(Settings.Key);Status="동기화 완료 · 실제 검 한 자루의 최고 판매 대금 순위입니다.";
 }
}
