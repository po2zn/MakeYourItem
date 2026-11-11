using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
if(!EditorApplication.isPlaying){EditorApplication.EnterPlaymode();return "Entered Play Mode; run again for validation.";}
var oldBackground=Application.runInBackground;Application.runInBackground=true;
var root=new GameObject("Smith repair validation");root.transform.position=new Vector3(30,0,0);
var visual=root.AddComponent<ForgeWorkshopVisuals>();visual.Initialize(Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets"));
Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
var camera=new GameObject("Repair camera").AddComponent<Camera>();camera.transform.position=new Vector3(34.5f,2.8f,-3.3f);camera.transform.LookAt(new Vector3(32.2f,.95f,.5f));camera.orthographic=true;camera.orthographicSize=1.7f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.23f,.28f);
var light=new GameObject("Repair light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(35,-30,0);
Directory.CreateDirectory("Docs/SmithRepair");
void Capture(string name){var smith=Find("Blacksmith");camera.transform.position=smith.position+new Vector3(-2,1.8f,-2);camera.transform.LookAt(smith.position+Vector3.up*1.05f);camera.orthographicSize=name.StartsWith("hammer")?.85f:1.15f;var rt=new RenderTexture(1200,1000,24);var prior=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1200,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1000),0,0);image.Apply();File.WriteAllBytes("Docs/SmithRepair/"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=prior;UnityEngine.Object.Destroy(image);rt.Release();UnityEngine.Object.Destroy(rt);}
string report="";
try{
 var evaluation=ForgeMvp.Evaluate("p = 1(1)\n2(p, 7)\n3(p)\n4(p, 3)\n2(p, 7)\n3(p)\n4(p, 0)",100,10,materialStocks:new Dictionary<int,long>{{3,10}});
 int index=0;
 foreach(var step in evaluation.steps){
 step.duration=1.3f;bool done=false;Exception failure=null;float elapsed=0;int shot=0;
 IEnumerator Perform(){var action=visual.Play(step);try{while(action.MoveNext()){elapsed+=Time.deltaTime;if(step.process==3&&shot<4&&elapsed>step.duration*(.48f+shot*.12f)){Capture("hammer-"+index+"-"+shot);shot++;}yield return action.Current;}}finally{(action as IDisposable)?.Dispose();done=true;}}
 IEnumerator Guard(){var task=Perform();while(true){bool more=false;try{more=task.MoveNext();}catch(Exception e){failure=e;done=true;}if(!more)break;yield return task.Current;}}
 visual.StartCoroutine(Guard());float begin=Time.realtimeSinceStartup;while(!done){if(Time.realtimeSinceStartup-begin>12)throw new Exception("timeout");await Task.Delay(20);}if(failure!=null)throw failure;
 if(step.process==4){await Task.Delay(500);var piece=Find("Workpiece "+step.pieceId);float error=Vector3.Distance(piece.position,Find("LeftHandSocket").position);if(error>.001f)throw new Exception("Floating cooled sword: "+error);Capture("cooling-held-"+index);report+="PASS cooling held (wet="+(step.consumed!=null&&step.consumed.ContainsKey(3))+") grip error="+error+"\n";}
 if(step.process==4){visual.Cancel();var expected=Find("StrikeTarget").position+new Vector3(.34f,.025f,0);if(Vector3.Distance(Find("Workpiece "+step.pieceId).position,expected)>.001f)throw new Exception("cancel support");report+="PASS cancel places held sword on anvil\n";}
 index++;
 }
 var catalog=Resources.Load<ForgeMaterialIcons>("BlacksmithMaterialIcons");if(!AssetDatabase.GetAssetPath(catalog.textures[3]).Contains("물방울"))throw new Exception("water source");report+="PASS supplied water texture and alpha crop: "+catalog.regions[3]+"\n";
 var skin=Find("Blacksmith").GetComponentInChildren<SkinnedMeshRenderer>();foreach(var w in skin.sharedMesh.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.0001f)throw new Exception("weight sum");report+="PASS normalized skin weights; hammer and cooling captures saved\n";
 report+=await MaterialIconChecks.Run();
 File.WriteAllText("Docs/SmithRepair/checks.txt",report);return report;
}finally{Application.runInBackground=oldBackground;UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(light.gameObject);EditorApplication.ExitPlaymode();}
