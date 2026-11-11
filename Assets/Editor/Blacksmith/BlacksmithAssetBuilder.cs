using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Editor-only, persistent asset authoring. Never executes at runtime or overwrites assets.
public static class BlacksmithAssetBuilder {
 const string Root="Assets/Art/Blacksmith/";
 static string run;
 static Dictionary<string,Material> mats;
 static List<GameObject> models;
 static Material Mat(string n,string hex,float metal=0,float smooth=.25f,bool glow=false) {
  if(mats.ContainsKey(n))return mats[n];
  ColorUtility.TryParseHtmlString(hex,out var col);
  var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=n};
  m.SetColor("_BaseColor",col);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
  if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",col*3);}
  AssetDatabase.CreateAsset(m,Root+"Materials/"+run+"/"+n+".mat");mats[n]=m;return m;
 }
 static GameObject Node(string n,Transform p,Vector3 pos) {var g=new GameObject(n);if(p)g.transform.SetParent(p,false);g.transform.localPosition=pos;return g;}
 static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
 static GameObject MeshPart(string n,Transform p,List<Vector3> v,List<int> t,Material m) {
  // Split each triangle for true flat normals.
  var a=t.Select(i=>v[i]).ToArray();var mesh=new Mesh{name=n};mesh.vertices=a;mesh.triangles=Enumerable.Range(0,a.Length).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
  AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Root+"Props/"+run+"/"+n+".asset"));
  var g=Node(n,p,Vector3.zero);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;return g;
 }
 static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material m,bool collider=false) {
  var v=new List<Vector3>();foreach(int z in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int x in new[]{-1,1})v.Add(Vector3.Scale(V(x,y,z),size)*.5f);
  int[] t={0,2,3,0,3,1,4,5,7,4,7,6,0,4,6,0,6,2,1,3,7,1,7,5,0,1,5,0,5,4,2,6,7,2,7,3};
  var g=MeshPart(n,p,v,t.ToList(),m);g.transform.localPosition=pos;if(collider)g.AddComponent<BoxCollider>().size=size;return g;
 }
 static GameObject Ring(string n,Transform p,float y,float height,float outer,float inner,Material m,int sides=12) {
  var v=new List<Vector3>();var t=new List<int>();
  for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;foreach(var r in new[]{outer,inner})foreach(var h in new[]{y,y+height})v.Add(V(Mathf.Cos(a)*r,h,Mathf.Sin(a)*r));}
  Action<int,int,int,int> q=(a,b,c,d)=>{t.AddRange(new[]{a,b,c,a,c,d});};
  for(int i=0;i<sides;i++){int a=i*4,b=(i+1)%sides*4;q(a,a+1,b+1,b);q(a+2,b+2,b+3,a+3);q(a+1,a+3,b+3,b+1);q(a,b,b+2,a+2);}
  return MeshPart(n,p,v,t,m);
 }
 static GameObject Disk(string n,Transform p,float y,float r,Material m,int sides=12) {
  var v=new List<Vector3>{V(0,y,0)};var t=new List<int>();for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(V(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));}for(int i=0;i<sides;i++)t.AddRange(new[]{0,(i+1)%sides+1,i+1});return MeshPart(n,p,v,t,m);
 }
 static GameObject Imported(string id,string name) {
  var src=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Source/"+id+".fbx");if(!src)throw new Exception("Missing "+id);
  var g=UnityEngine.Object.Instantiate(src);g.name=name;
  foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(old=>{
   string n=old.name;Color c=old.HasProperty("_Color")?old.color:Color.gray;
   string hex="#"+ColorUtility.ToHtmlStringRGB(c);
   if(n=="iron")hex="#353b43";if(n=="wood")hex="#805636";
   if(n=="apron"||n=="apronTie")hex="#72462e";
   return Mat("Imported_"+n,hex,n=="iron"||n=="buckleMetal"?.65f:0);
  }).ToArray();return g;
 }
 static GameObject Forge() {
  var g=Node("Forge",null,Vector3.zero);var body=Node("StoneBody",g.transform,Vector3.zero);
  var stone=Mat("Stone","#655b50");var brick=Mat("Brick","#795c49");var iron=Mat("Iron","#353b43",.65f);
  Box("Foundation",body.transform,V(0,.12f,0),V(1.3f,.24f,.95f),stone,true);
  for(int row=0;row<3;row++)for(int i=0;i<4;i++)Box("FrontBrick",body.transform,V((i-1.5f)*.31f,.33f+row*.19f,-.34f),V(.30f,.18f,.23f),row%2==0?stone:brick,true);
  Box("HearthFloor",body.transform,V(0,.73f,0),V(1.2f,.13f,.85f),stone,true);
  Box("LeftWall",body.transform,V(-.54f,.93f,0),V(.16f,.3f,.85f),brick,true);
  Box("RightWall",body.transform,V(.54f,.93f,0),V(.16f,.3f,.85f),brick,true);
  Box("BackWall",body.transform,V(0,1.04f,.36f),V(1.1f,.52f,.16f),stone,true);
  Box("WorkLip",body.transform,V(0,.83f,-.43f),V(1.14f,.08f,.23f),iron,true);
  var fuel=Node("Charcoal",g.transform,Vector3.zero);var coal=Mat("Charcoal","#252322");
  for(int x=0;x<7;x++)for(int z=0;z<4;z++){var c=Box("Coal",fuel.transform,V((x-3)*.135f,.83f+(x%2)*.012f,(z-1.5f)*.14f),V(.12f,.08f,.11f),coal);c.transform.localRotation=Quaternion.Euler(0,x*19+z*31,8);}
  var fire=Node("Fire",g.transform,Vector3.zero);Box("Embers",fire.transform,V(0,.808f,0),V(.97f,.02f,.62f),Mat("Embers","#ff6518",0,.2f,true));
  for(int i=0;i<7;i++){float x=(i-3)*.12f;var v=new List<Vector3>{V(x-.055f,.84f,0),V(x+.055f,.84f,0),V(x,.84f,.09f),V(x+.02f,1.03f+(i%3)*.045f,.035f)};MeshPart("Flame",fire.transform,v,new List<int>{0,3,1,1,3,2,2,3,0,0,1,2},Mat("Flame","#ff9e32",0,.2f,true));}
  var light=Node("FireLight",fire.transform,V(0,1.08f,-.1f)).AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.35f,.08f);light.range=2.4f;light.intensity=2;
  Node("HeatTarget",g.transform,V(0,.92f,-.05f));return g;
 }
 static GameObject Bucket() {
  var g=Node("QuenchBucket",null,Vector3.zero);var wood=Mat("Wood","#805636");var iron=Mat("Iron","#353b43",.65f);
  Ring("HollowStaves",g.transform,.04f,.53f,.32f,.28f,wood);
  Disk("InnerBottom",g.transform,.04f,.28f,wood);var bottom=Node("BottomCollider",g.transform,V(0,.02f,0)).AddComponent<BoxCollider>();bottom.size=V(.48f,.04f,.48f);
  foreach(float y in new[]{.08f,.46f})Ring("IronHoop",g.transform,y,.055f,.333f,.319f,iron);
  for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;var c=Node("WallCollider",g.transform,V(Mathf.Cos(a)*.3f,.3f,Mathf.Sin(a)*.3f));c.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);c.AddComponent<BoxCollider>().size=V(.04f,.53f,.163f);}
  Disk("WaterSurface",g.transform,.43f,.278f,Mat("Water","#326b76",.15f,.8f));
  var trigger=Node("QuenchVolume",g.transform,V(0,.24f,0)).AddComponent<BoxCollider>();trigger.size=V(.38f,.38f,.38f);trigger.isTrigger=true;
  for(int side=-1;side<=1;side+=2)Box("HandleLug",g.transform,V(side*.33f,.46f,0),V(.06f,.09f,.09f),iron);
  Node("QuenchTarget",g.transform,V(0,.43f,0));return g;
 }
 static GameObject Sword() {
  var g=Node("Sword",null,Vector3.zero);var blade=Mat("Blade_HeatIndependent","#aeb9bf",.8f,.48f);blade.EnableKeyword("_EMISSION");blade.SetColor("_EmissionColor",Color.black);
  var v=new List<Vector3>();foreach(float y in new[]{.13f,.77f}){float w=y<.2f?.045f:.032f;v.AddRange(new[]{V(-w,y,0),V(0,y,.012f),V(w,y,0),V(0,y,-.012f)});}v.Add(V(0,.94f,0));
  var t=new List<int>();for(int i=0;i<4;i++){int j=(i+1)%4;t.AddRange(new[]{i,j,j+4,i,j+4,i+4,i+4,j+4,8});}t.AddRange(new[]{0,3,2,0,2,1});
  MeshPart("Blade",g.transform,v,t,blade);
  Box("Guard",g.transform,V(0,.115f,0),V(.27f,.045f,.045f),Mat("Iron","#353b43",.65f));
  Box("Grip",g.transform,V(0,0,0),V(.043f,.19f,.048f),Mat("Leather","#72462e"));
  Box("Pommel",g.transform,V(0,-.125f,0),V(.075f,.06f,.06f),Mat("Iron","#353b43",.65f));
  Node("GripAnchor",g.transform,Vector3.zero);Node("Tip",g.transform,V(0,.94f,0));return g;
 }
 static GameObject Hammer() {
  var g=Node("Hammer",null,Vector3.zero);Box("Handle",g.transform,V(0,.1f,0),V(.04f,.36f,.045f),Mat("Wood","#805636"));
  Box("Head",g.transform,V(0,.28f,0),V(.23f,.105f,.10f),Mat("Iron","#353b43",.65f));Node("GripAnchor",g.transform,Vector3.zero);return g;
 }
 static GameObject Smith() {
  var g=Imported("tradesman-1707b9","Blacksmith");
  var hat=g.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="attach-hat");if(hat)UnityEngine.Object.DestroyImmediate(hat.gameObject);
  foreach(string side in new[]{"Left","Right"}){
   var hand=g.GetComponentsInChildren<Transform>().First(t=>t.name==side+"Hand");
   float sign=side=="Left"?-1:1;
   Box(side+"LeatherGlove",hand,V(sign*.065f,-.012f,0),V(.18f,.105f,.12f),Mat("Gloves","#593b29"));
   Node(side+"HandSocket",hand,V(sign*.065f,-.045f,.025f));
  }
  var anim=g.GetComponent<Animator>();if(anim)anim.runtimeAnimatorController=null;
  return g;
 }
 static GameObject Anvil() {
  var g=Imported("anvil-1286cb","Anvil");
  var baseNode=Node("TimberBase",g.transform,Vector3.zero);
  foreach(var t in g.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("stump_")).ToArray())t.SetParent(baseNode.transform,true);
  var c=baseNode.AddComponent<BoxCollider>();c.center=V(0,.23f,0);c.size=V(.60f,.46f,.60f);
  var face=Node("WorkFaceCollider",g.transform,V(.06f,.785f,0));face.AddComponent<BoxCollider>().size=V(.48f,.075f,.25f);
  var body=Node("BodyCollider",g.transform,V(.04f,.61f,0));body.AddComponent<BoxCollider>().size=V(.34f,.28f,.23f);
  Node("StrikeTarget",g.transform,V(.06f,.825f,0));return g;
 }
 [MenuItem("Tools/Blacksmith/Build New Asset Set")]
 public static void BuildMenu(){Build();}
 public static string Build() {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode before building assets.");
  run="Set_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");mats=new Dictionary<string,Material>();models=new List<GameObject>();
  foreach(string d in new[]{"Characters","Props","Materials","Prefabs","Documentation"})Directory.CreateDirectory(Root+d+"/"+run);AssetDatabase.Refresh();
  var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  try{
   models.Add(Smith());models.Add(Forge());models.Add(Bucket());models.Add(Anvil());models.Add(Sword());models.Add(Hammer());
   var report=new System.Text.StringBuilder("Model,Triangles,Materials,SkinnedRenderers,Source\n");
   for(int i=0;i<models.Count;i++){
    var g=models[i];var meshes=g.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).Concat(g.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>r.sharedMesh)).ToArray();
    int tris=meshes.Sum(m=>m.triangles.Length/3),mc=g.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Count();
    report.AppendLine(g.name+","+tris+","+mc+","+g.GetComponentsInChildren<SkinnedMeshRenderer>().Length+","+(i==0||i==3?"Polyfork adapted":"Original procedural"));
    PrefabUtility.SaveAsPrefabAsset(g,Root+"Prefabs/"+run+"/"+g.name+".prefab");
   }
   var clips=AssetDatabase.LoadAllAssetsAtPath(Root+"Source/tradesman-1707b9.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
   foreach(var clip in clips)AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(clip),Root+"Characters/"+run+"/"+clip.name+".anim");
   File.WriteAllText(Root+"Documentation/"+run+"/Metrics.csv",report.ToString());
   models[0].transform.position=V(-1.55f,0,.35f);models[0].transform.rotation=Quaternion.Euler(0,155,0);models[1].transform.position=V(0,0,.65f);models[2].transform.position=V(1.45f,0,.45f);models[3].transform.position=V(0,0,-.65f);
   // FBX clip sampling produced NaN transforms in Unity 6000.4.5f1; use a safe static rest pose.
   var left=models[0].GetComponentsInChildren<Transform>().First(t=>t.name=="LeftArm");
   var right=models[0].GetComponentsInChildren<Transform>().First(t=>t.name=="RightArm");
   left.localRotation=Quaternion.Euler(0,0,65);right.localRotation=Quaternion.Euler(0,0,-65);
   models[4].transform.position=V(0,.86f,-.65f);models[4].transform.rotation=Quaternion.Euler(0,0,-90);
   models[5].transform.position=V(.35f,.87f,-.6f);models[5].transform.rotation=Quaternion.Euler(90,0,0);
   Box("PreviewGround",null,V(0,-.07f,0),V(5,.12f,4),Mat("Ground","#47423b"));
   var camera=Node("PreviewCamera",null,V(3.6f,3.1f,-5.4f)).AddComponent<Camera>();camera.transform.LookAt(V(0,.65f,0));camera.orthographic=true;camera.orthographicSize=2.1f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.15f,.18f);
   var key=Node("KeyLight",null,V(0,4,-2)).AddComponent<Light>();key.type=LightType.Directional;key.intensity=2.0f;key.transform.rotation=Quaternion.Euler(45,-35,0);
   var fill=Node("FillLight",null,V(-3,3,2)).AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.8f;fill.transform.rotation=Quaternion.Euler(40,145,0);
   EditorSceneManager.SaveScene(scene,Root+"Documentation/"+run+"/BlacksmithPreview.unity");
   var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Root+"Documentation/"+run+"/Preview.png",tex.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
   AssetDatabase.SaveAssets();AssetDatabase.Refresh();return run+"\n"+report;
  }finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);}
 }
}
