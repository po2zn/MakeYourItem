using System;using System.IO;using System.Text;using UnityEditor;using UnityEngine;using UnityEngine.UI;
public static class ForgeLeaderboardLayoutChecks {
 public static void RunBatch(){try{Run();Debug.Log("LEADERBOARD_LAYOUT_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Run(){
 Directory.CreateDirectory("Artifacts/Leaderboard/Layout");var report=new StringBuilder();
 var prefab=AssetDatabase.LoadAssetAtPath<ForgeLeaderboardView>("Assets/Resources/ForgeLeaderboardUI.prefab");if(!prefab)throw new Exception("Missing prefab");
 foreach(var size in new[]{new Vector2Int(640,480),new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)}){
 var view=UnityEngine.Object.Instantiate(prefab);var canvas=view.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;var root=(RectTransform)view.transform;root.sizeDelta=size;root.position=Vector3.zero;
 foreach(var row in view.rows){row.gameObject.SetActive(true);row.text="20위 · 가나다라마바사아자차카타파하검왕 · 2,147,483,647G";}
 view.statusText.text="인터넷 연결을 확인해주세요. 내 기록과 이전 순위는 보존됩니다.";
 Canvas.ForceUpdateCanvases();for(int j=0;j<3;j++)LayoutRebuilder.ForceRebuildLayoutImmediate(root);
 float lastBottom=float.PositiveInfinity;foreach(Transform child in view.scroll.content){if(!child.gameObject.activeSelf)continue;var r=(RectTransform)child;var corners=new Vector3[4];r.GetWorldCorners(corners);if(corners[1].y>lastBottom+.1f)throw new Exception("Overlap: "+child.name+" at "+size);lastBottom=corners[0].y;var t=child.GetComponent<Text>();if(t&&t.preferredHeight>r.rect.height+.5f)throw new Exception("Clipped: "+t.name+" at "+size+" preferred="+t.preferredHeight+" actual="+r.rect.height);}
 if(view.scroll.content.rect.height>view.scroll.viewport.rect.height&&view.scroll.verticalScrollbar==null)throw new Exception("Missing scroll");
 report.AppendLine("PASS "+size+" wrapped Korean prose, long names and max amount: no overlapping rows or clipped labels; scroll available.");
 var cg=new GameObject("CaptureCamera",typeof(Camera));var camera=cg.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=size.y/2f;camera.transform.position=new Vector3(0,0,-100);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=ForgeTitleStyle.Background;
 var rt=new RenderTexture(size.x,size.y,24);camera.targetTexture=rt;canvas.worldCamera=camera;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes("Artifacts/Leaderboard/Layout/"+size.x+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(view.gameObject);
 }
 File.WriteAllText("Artifacts/Leaderboard/Layout/results.txt",report.ToString());
 }
}
