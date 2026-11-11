using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class DeveloperDaylightChecks
{
 public static async Task<string> Run(){
 if(!EditorApplication.isPlaying)throw new Exception("Enter Play Mode");
 var game=UnityEngine.Object.FindAnyObjectByType<ForgeMvp>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
 if((bool)typeof(ForgeMvp).GetField("progressLoaded",flags).GetValue(game))throw new Exception("Run from title without a loaded save");
 var cycle=UnityEngine.Object.FindAnyObjectByType<ForgeDayCycle>();var ui=game.HierarchyGame;ui.Bind(game);
 var action=ui.GetComponentsInChildren<ForgeUIAction>(true).Single(a=>a.action=="daylight");
 int day=cycle.Day;double hour=cycle.CurrentHour;bool paused=cycle.Paused;int notices=0;Action<int> listener=_=>notices++;
 cycle.Paused=true;cycle.Clock.DayChanged+=listener;
 try{
 cycle.Clock.SetHour(23);action.button.onClick.Invoke();
 if(cycle.Day!=day||cycle.CurrentHour!=12||notices!=0)throw new Exception("Day or noon or notification mismatch");
 if(RenderSettings.ambientLight.r<.47f)throw new Exception("Lighting not refreshed while paused");
 var label=action.GetComponentInChildren<Text>(true);string report="PASS bound UI button: night -> noon, same day, no DayChanged event, immediate daylight while paused\n";
 foreach(var size in new[]{new Vector2(800,600),new Vector2(1280,720),new Vector2(1920,1080),new Vector2(3840,2160),new Vector2(380,240)}){
 float panelWidth=Mathf.Min(816,size.x-24);float cell=Mathf.Max(120,(panelWidth-40)/2);float width=cell-20;
 var settings=label.GetGenerationSettings(new Vector2(width,77));settings.resizeTextForBestFit=false;settings.fontSize=label.resizeTextMinSize;
 var generator=new TextGenerator();float preferred=generator.GetPreferredHeight(label.text,settings)/label.pixelsPerUnit;
 if(preferred>77)throw new Exception("Text overflow at "+size);
 report+="PASS label metrics "+size+" numeric/English modes (same debug label), minimum font="+settings.fontSize+"\n";
 }
 bool title=(bool)typeof(ForgeMvp).GetField("showTitle",flags).GetValue(game);bool background=Application.runInBackground;
 try{Application.runInBackground=true;typeof(ForgeMvp).GetField("showTitle",flags).SetValue(game,false);await Task.Delay(200);ui.Active("Developer",true);ui.Node("DeveloperScroll").GetComponent<ScrollRect>().verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();await Task.Delay(200);ScreenCapture.CaptureScreenshot("Assets/Art/Blacksmith/Documentation/DeveloperDaylight.png");await Task.Delay(500);}
 finally{ui.Active("Developer",false);typeof(ForgeMvp).GetField("showTitle",flags).SetValue(game,title);Application.runInBackground=background;}
 System.IO.File.WriteAllText("Assets/Art/Blacksmith/Documentation/DeveloperDaylightChecks.txt",report+"Resolution checks are text metrics; screenshot is the current Editor Game view only.\n");return report;
 }finally{cycle.Clock.DayChanged-=listener;cycle.Clock.SetHour(hour);cycle.Paused=paused;typeof(ForgeDayCycle).GetMethod("ApplyLighting",flags).Invoke(cycle,null);}
 }
}
