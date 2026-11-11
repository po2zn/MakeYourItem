using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
public static class MaterialResolutionChecks
{
 public static async Task<string> Run(){
 const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 var assembly=typeof(Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance",flags).GetValue(null);
 var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});var gt=group.GetType();
 var viewType=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);var selected=viewType.GetProperty("selectedSizeIndex",flags);int oldIndex=(int)selected.GetValue(view);
 var game=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();var title=typeof(ForgeMvp).GetField("showTitle",flags);var tree=typeof(ForgeMvp).GetField("showTechTree",flags);bool oldTitle=(bool)title.GetValue(game),oldTree=(bool)tree.GetValue(game),background=Application.runInBackground;int custom=-1;string report="";
 try{Application.runInBackground=true;title.SetValue(game,false);tree.SetValue(game,true);await Task.Delay(250);game.HierarchyTree.ShowPage(1);
 foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)}){
 custom=(int)gt.GetMethod("GetCustomCount",flags).Invoke(group,null);int built=(int)gt.GetMethod("GetBuiltinCount",flags).Invoke(group,null);
 var sizeType=assembly.GetType("UnityEditor.GameViewSize");var sizeEnum=assembly.GetType("UnityEditor.GameViewSizeType");var item=Activator.CreateInstance(sizeType,flags,null,new object[]{Enum.Parse(sizeEnum,"FixedResolution"),size.x,size.y,"Material check"},null);
 gt.GetMethod("AddCustomSize",flags).Invoke(group,new[]{item});selected.SetValue(view,built+custom);view.Repaint();await Task.Delay(500);
 if(Screen.width!=size.x||Screen.height!=size.y)throw new Exception("Requested "+size+" actual "+Screen.width+"x"+Screen.height);
 game.HierarchyTree.maps[1].Fit();Canvas.ForceUpdateCanvases();await Task.Delay(200);ScreenCapture.CaptureScreenshot("Assets/Art/Blacksmith/Documentation/MaterialTree_"+size.x+"x"+size.y+".png");await Task.Delay(350);
 report+="Captured live Game view "+size+"\n";selected.SetValue(view,oldIndex);gt.GetMethod("RemoveCustomSize",flags).Invoke(group,new object[]{custom});custom=-1;
 }
 File.WriteAllText("Assets/Art/Blacksmith/Documentation/MaterialResolutionChecks.txt",report);return report;
 }finally{selected.SetValue(view,oldIndex);if(custom>=0)gt.GetMethod("RemoveCustomSize",flags).Invoke(group,new object[]{custom});title.SetValue(game,oldTitle);tree.SetValue(game,oldTree);Application.runInBackground=background;}
 }
}
