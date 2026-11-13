using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class WorkshopModelChecks
{
 const string Key="WorkshopModelChecks.Pending";
 const string Folder="Artifacts/WorkshopModels";
 static double started;
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 [InitializeOnLoadMethod] static void Resume(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);Start();}};}
 public static void RunBatch(){Directory.CreateDirectory(Folder);WorkshopImportedModels.BuildAll();SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.EnterPlaymode();}
 static void Start(){Application.runInBackground=true;Time.timeScale=1;Time.captureDeltaTime=1f/60;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Watchdog;var host=new GameObject("Model checks").AddComponent<ForgeCraftMotion>();host.StartCoroutine(Guard(Run()));}
 static void Watchdog(){if(EditorApplication.timeSinceStartup-started>180)Fail(new Exception("Model test timeout"));}
 static void Fail(Exception e){File.WriteAllText(Folder+"/failure.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
 static IEnumerator Guard(IEnumerator iterator){var stack=new Stack<IEnumerator>();stack.Push(iterator);while(stack.Count>0){var top=stack.Peek();bool more=false;try{more=top.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more){(top as IDisposable)?.Dispose();stack.Pop();continue;}if(top.Current is IEnumerator nested)stack.Push(nested);else yield return top.Current;}Debug.Log("WORKSHOP_MODELS_PLAY_PASS "+DateTime.UtcNow.ToString("O"));File.WriteAllText(Folder+"/results.txt","PASS real Play Mode: imported textured props, raw-to-shaped transition, frost unlock/hide/idempotency, frost route and ingredient, cancellation, rune model grades/upgrade/expiry.\n");EditorApplication.Exit(0);}
 static void Capture(Camera camera,string name){var rt=new RenderTexture(1280,900,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,900),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.Destroy(image);rt.Release();UnityEngine.Object.Destroy(rt);}
 static IEnumerator CheckStationFeedbackAndAudio(){
  var root=new GameObject("Station feedback test");var motion=root.AddComponent<ForgeCraftMotion>();var source=new GameObject("Feedback source");motion.Initialize(source);motion.SetFrostUnlocked(true);
  var graph=ForgeCoolingGraph.Create(84,new ForgeCoolingSettings());var step=new ForgeMvp.Step{process=4,coolingCall=84,pieceId=37,material=7,shaped=true,duration=.8f,heat=0,consumed=new Dictionary<int,int>{{8,1}},coolingPath=graph.DefaultPath().ToArray(),data=new ForgeItemData{cooling=graph}};
  yield return motion.Play(step);Require(motion.StationCoolingView.Blade.id==37&&motion.StationCoolingView.Path.SequenceEqual(step.coolingPath)&&motion.StationCoolingView.Progress==1,"actual receipt updates local station view");
  yield return motion.Play(new ForgeMvp.Step{finished=true,pieceId=37});Require(motion.StationCoolingView.Blade==null,"submitted piece clears station view");
  var audio=root.GetComponent<ForgeActionAudio>();var lib=ForgeSfxLibrary.Instance;
  foreach(float speed in new[]{1f,4f,5f}){step.workSpeed=speed;audio.Begin(step);audio.Frame(.6f,false);yield return null;var cooling=root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>s.loop&&s.clip==lib.AtSpeed(lib.frostCooling,speed));Require(cooling&&cooling.isPlaying&&Mathf.Abs(cooling.volume-.7f*ForgeAudioSettings.Sfx)<.001f,"frost loop and configured volume at "+speed);audio.Stop();Require(root.GetComponentsInChildren<AudioSource>().All(s=>!s.isPlaying),"cancel stops all station voices");}
  UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(source);
 }
 static void CheckUnlockAndRuneModels(){
  const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  var host=new GameObject("Unlock integration checks");host.SetActive(false);var game=host.AddComponent<ForgeMvp>();var motion=host.AddComponent<ForgeCraftMotion>();var source=new GameObject("Source workpiece");motion.Initialize(source);
  void Set(string key,object value)=>typeof(ForgeMvp).GetField(key,flags).SetValue(game,value);
  object Get(string key)=>typeof(ForgeMvp).GetField(key,flags).GetValue(game);
  void Call(string key)=>typeof(ForgeMvp).GetMethod(key,flags).Invoke(game,null);
  try{
   Set("craftMotion",motion);Set("work",source);Set("<DayCycle>k__BackingField",host.AddComponent<ForgeDayCycle>());Set("showTitle",false);Set("showTutorial",false);
   var tech=(ForgeTechnology)Get("technology");tech.Restore(Array.Empty<BlueprintFeature>());Call("UpdateSmiths");var visual=host.GetComponent<ForgeWorkshopVisuals>();Require(!visual.FrostStationVisible,"actual locked tech hides workstation");
   tech.Restore(Array.Empty<BlueprintFeature>(),new[]{"material-8"});Call("UpdateSmiths");Call("UpdateSmiths");Require(visual.FrostStationVisible,"restored frost unlock creates workstation");
   var hoverCamera=new GameObject("Hover ray test").AddComponent<Camera>();hoverCamera.transform.position=visual.FrostWorkPosition+new Vector3(0,3,-5);hoverCamera.transform.LookAt(visual.FrostWorkPosition);Set("workspaceCamera",hoverCamera);var point=(Vector2)hoverCamera.WorldToScreenPoint(visual.FrostWorkPosition);Require(game.TryFrostStationHover(point,out int hovered,out var selectedMotion)&&hovered==0&&selectedMotion==motion,"world station hover chooses its owner");Set("showGameMenu",true);Require(!game.TryFrostStationHover(point,out _,out _),"menu blocks station hover");Set("showGameMenu",false);UnityEngine.Object.Destroy(hoverCamera.gameObject);Require(host.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Frost water workstation")==1,"repeated update does not duplicate station");
   var state=(ForgeBlacksmiths)Get("blacksmiths");var rune=new ForgeBlacksmiths.GiantRune{rune=1,x=0,y=0,timed=true,expiresAt=1000};state.runes.Add(rune);Call("UpdateGiantRuneViews");var views=(List<GameObject>)Get("giantRuneViews");Require(views[0].transform.Find("Rune model").GetChild(0).name=="GiantRunePillar","grade 1 physical model");
   rune.rune=4;Call("UpdateGiantRuneViews");Require(views[0].transform.Find("Rune model").GetChild(0).name=="GiantRunePillar","grade 2 same physical model");rune.rune=7;Call("UpdateGiantRuneViews");Call("UpdateGiantRuneViews");Require(views[0].transform.Find("Rune model").GetChild(0).name=="GiantRuneCircle","upgrade switches to circle");Require(views[0].GetComponentsInChildren<Transform>().Count(t=>t.name=="Rune model")==1,"same-frame model swap idempotent");
   state.runes.Clear();Call("UpdateGiantRuneViews");Require(views.Count==0,"expired rune removes model");
  }finally{UnityEngine.Object.Destroy(host);UnityEngine.Object.Destroy(source);}
 }
 static IEnumerator Run(){
  var a=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");Require(a&&a.Ready&&a.frostStation&&a.giantRuneCircle&&a.giantRunePillar,"catalog references");
  for(int id=6;id<=9;id++)Require(a.MaterialModel(id)&&a.MaterialModel(id).GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.GetTexture("_BaseMap")),"textured material "+id);
  var root=new GameObject("Workshop model validation");root.transform.position=new Vector3(30,0,0);var v=root.AddComponent<ForgeWorkshopVisuals>();v.Initialize(a);
  var camera=new GameObject("Model camera").AddComponent<Camera>();camera.gameObject.AddComponent<AudioListener>();camera.orthographic=true;camera.orthographicSize=2.7f;camera.transform.position=new Vector3(36,4,-6);camera.transform.LookAt(new Vector3(32.2f,.8f,.6f));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.20f,.24f);
  var light=new GameObject("Model light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-35,0);
  Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
  Require(!v.FrostStationVisible,"locked station hidden");v.SetFrostUnlocked(true);v.SetFrostUnlocked(true);Require(v.FrostStationVisible&&root.GetComponentsInChildren<Transform>().Count(t=>t.name=="Frost water workstation")==1,"one station on unlock");Require(v.FrostWorkPosition.x>Find("StrikeTarget").position.x,"station right of anvil");v.SetFrostUnlocked(false);Require(!v.FrostStationVisible,"new/locked state hides station");v.SetFrostUnlocked(true);yield return null;yield return null;Capture(camera,"workshop-unlocked");
  var evaluation=BlueprintRuntime.Evaluate("p=1(7)\n2(p)\n3(p)\n4(p,8)\n211(p,6)\n!p",0,0,"models",1,new Dictionary<int,long>{{7,1},{8,100},{6,1}},context:new ForgeRunContext{CoolingUnlocked=true});Require(evaluation.error==null,evaluation.error);
  foreach(var step in evaluation.steps){step.duration=1.6f;bool raw=false,ingredient=false;int shots=0;var action=v.Play(step);float elapsed=0;
   try{while(action.MoveNext()){
    elapsed+=Time.deltaTime;float t=elapsed/step.duration;
    raw|=root.GetComponentsInChildren<Transform>().Any(x=>x.name=="Raw material model");ingredient|=root.GetComponentsInChildren<Transform>().Any(x=>x.name=="Held ingredient "+(step.process==4?8:6));
    foreach(var bone in Find("Blacksmith").GetComponentsInChildren<Transform>())Require(float.IsFinite(bone.position.x)&&float.IsFinite(bone.position.y),"finite hand/leg pose");
    if(step.process==4&&t>.55f&&t<.85f)Require(Vector3.Distance(v.SmithPosition,new Vector3(v.FrostWorkPosition.x-.25f,0,v.FrostWorkPosition.z+.8f))<.05f,"smith reaches frost workstation");
    if(shots<3&&t>.32f+shots*.25f){Capture(camera,"process-"+step.process+"-"+shots);shots++;}yield return action.Current;
   }}finally{(action as IDisposable)?.Dispose();}
   if(step.process==1)Require(raw,"star ore replaces billet");if(step.process==3)Require(!root.GetComponentsInChildren<Transform>().Any(x=>x.name=="Raw material model"),"shaped blade replaces raw model");
   if(step.process==4||step.process==211)Require(ingredient,"imported ingredient shown "+step.process);
   v.Cancel();Require(!root.GetComponentsInChildren<Transform>().Any(x=>x.name.StartsWith("Held ingredient")||x.name=="Frost cooling glow"),"transient props cleared");
  }
  v.Clear();var boneStep=new ForgeMvp.Step{process=1,pieceId=91,material=9,duration=.4f,data=new ForgeItemData()};yield return v.Play(boneStep);Require(Find("Raw material model").GetChild(0).GetComponentInChildren<MeshFilter>()!=null,"bone mesh");Capture(camera,"bone-raw");
  var cancelStep=new ForgeMvp.Step{process=4,pieceId=91,material=9,shaped=true,heat=0,duration=2,data=new ForgeItemData(),consumed=new Dictionary<int,int>{{8,1}}};
  var cancel=v.Play(cancelStep);bool interrupted=false;try{while(cancel.MoveNext()){if(root.GetComponentsInChildren<Transform>().Any(x=>x.name=="Held ingredient 8")){v.Cancel();interrupted=true;break;}yield return cancel.Current;}}finally{(cancel as IDisposable)?.Dispose();}Require(interrupted&&!v.IsActing&&!root.GetComponentsInChildren<Transform>().Any(x=>x.name.StartsWith("Held ingredient")),"cancel cleans ingredient and pose");
  yield return CheckStationFeedbackAndAudio();
  CheckUnlockAndRuneModels();
  // Existing numeric call tests cover saved expiry and only-once economics with the new models.
  ForgeRuneChecks.RunUnity();
  for(int grade=1;grade<=3;grade++){var rune=UnityEngine.Object.Instantiate(a.GiantRuneModel(grade));rune.transform.position=new Vector3(32,0,-.5f);Require(a.GiantRuneModel(1)==a.GiantRuneModel(2)&&a.GiantRuneModel(2)!=a.GiantRuneModel(3),"grade model mapping");Capture(camera,"rune-grade-"+grade);UnityEngine.Object.Destroy(rune);yield return null;}
  UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(light.gameObject);
 }
}
