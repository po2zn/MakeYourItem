using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
public static class ForgeReplacement
{
 static string Model=>Directory.GetFiles("Assets/Art/Blacksmith","Meshy_AI_Hearthstone*.fbx",SearchOption.AllDirectories).Single().Replace('\\','/');
 public static string Connect(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");
 const string path="Assets/Art/Blacksmith/Prefabs/Forge_UserModel.prefab";var root=PrefabUtility.LoadPrefabContents(path);
 try{
 root.transform.GetChild(0).localRotation=Quaternion.Euler(0,180,0)*root.transform.GetChild(0).localRotation;
 var target=new GameObject("HeatTarget").transform;target.SetParent(root.transform,false);target.localPosition=new Vector3(0,.66f,-.2f);
 var fire=new GameObject("FireGlow").AddComponent<Light>();fire.transform.SetParent(root.transform,false);fire.transform.localPosition=new Vector3(0,.64f,-.32f);fire.type=LightType.Point;fire.color=new Color(1,.32f,.06f);fire.intensity=.7f;fire.range=2;fire.shadows=LightShadows.None;
 void Box(string name,Vector3 p,Vector3 s){var g=new GameObject(name);g.transform.SetParent(root.transform,false);g.transform.localPosition=p;g.AddComponent<BoxCollider>().size=s;}
 Box("BaseCollider",new Vector3(0,.19f,0),new Vector3(.78f,.38f,.65f));
 Box("LeftWallCollider",new Vector3(-.34f,.62f,0),new Vector3(.16f,.48f,.6f));Box("RightWallCollider",new Vector3(.34f,.62f,0),new Vector3(.16f,.48f,.6f));
 Box("BackCollider",new Vector3(0,.65f,.22f),new Vector3(.6f,.5f,.13f));Box("ChimneyCollider",new Vector3(0,1.16f,0),new Vector3(.56f,.68f,.52f));
 var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);var catalog=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");catalog.forge=prefab;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();return "Connected "+path;
 }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 public static string Build(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");AssetDatabase.Refresh();
 var root=new GameObject("Forge_UserModel");
 try{
 var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model),root.transform);model.name="Hearthstone Forge";
 var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.localScale*=1.5f/b.size.y;
 b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
 var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="Forge_UserTexture";mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Model.Replace(".fbx",".png")));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Materials/Forge_UserTexture.mat"));foreach(var r in rs)r.sharedMaterials=r.sharedMaterials.Select(m=>mat).ToArray();
 string path=AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Prefabs/Forge_UserModel.prefab");PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();return path+" triangles="+model.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
 }finally{Object.DestroyImmediate(root);}
 }
 public static string Preview(){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Blacksmith/Prefabs/Forge_UserModel.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g,scene);
 var cam=new GameObject("PreviewCamera").AddComponent<Camera>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam.gameObject,scene);cam.scene=scene;cam.orthographic=true;cam.orthographicSize=1.1f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.2f,.22f,.25f);
 var light=new GameObject("PreviewLight").AddComponent<Light>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject,scene);light.type=LightType.Directional;light.intensity=1.5f;
 foreach(int side in new[]{-1,1}){cam.transform.position=new Vector3(2,1.9f,side*3);cam.transform.LookAt(new Vector3(0,.75f,0));light.transform.rotation=cam.transform.rotation;
 var rt=new RenderTexture(800,800,24);var prev=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(800,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,800,800),0,0);tex.Apply();File.WriteAllBytes("Assets/Art/Blacksmith/Documentation/ForgePreview"+side+".png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=prev;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);}
 UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);return "Saved front/back previews";
 }
}
