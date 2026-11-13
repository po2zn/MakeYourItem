using System;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.Build.Player;
public static class ForgeLeaderboardWebChecks {
 public static void RunBatch(){try{
  var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/NotoSansKR.ttf");var button=new GUIStyle{font=font,fontSize=22,wordWrap=true,padding=new RectOffset(12,12,10,10)};
  foreach(var size in new[]{new Vector2Int(640,480),new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)}){
   float width=size.x>=760?Mathf.Min(620,size.x*.46f):size.x-32;var rect=new Rect(0,0,width-48,58);var fitted=ForgeTextLayout.Fit(rect,new GUIContent("리더보드"),button);if(!ForgeTextLayout.Fits(rect,new GUIContent("리더보드"),fitted))throw new Exception("title button fit "+size);
   var input=new GUIStyle{font=font,fontSize=22};if(input.CalcSize(new GUIContent("가나다라마바사아자차카타파하검왕")).x>size.x-96)throw new Exception("name input width "+size);
  }
  var settings=new ScriptCompilationSettings{target=BuildTarget.WebGL,group=BuildTargetGroup.WebGL};Directory.CreateDirectory("Artifacts/Leaderboard/WebScripts");var result=PlayerBuildInterface.CompilePlayerScripts(settings,"Artifacts/Leaderboard/WebScripts");if(result.assemblies==null||result.assemblies.Count==0)throw new Exception("WebGL script compilation failed");
  File.WriteAllText("Artifacts/Leaderboard/web-results.txt","PASS WebGL player-script compilation and title control/name-field text metrics at 640x480,800x600,1280x720,1920x1080,3840x2160. No hosted browser or visual screenshot verification.");Debug.Log("LEADERBOARD_WEB_SCRIPTS_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
