using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
public static class AnvilReplacement
{
 static string Model=>Directory.GetFiles("Assets/Art/Blacksmith","Meshy_AI_Anvil*.fbx",SearchOption.AllDirectories).Single().Replace('\\','/');
 public static string Build(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");AssetDatabase.Refresh();
 var catalog=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");var root=new GameObject("Anvil_UserModel");
 try{
 var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model),root.transform);model.name="Anvil and wooden stand";
 var renderers=model.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
 float factor=.83f/b.size.y;model.transform.localScale*=factor;
 b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
 var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="Anvil_UserTexture";material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Model.Replace(".fbx",".png")));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.25f);
 AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Materials/Anvil_UserTexture.mat"));foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(m=>material).ToArray();
 var strike=new GameObject("StrikeTarget").transform;strike.SetParent(root.transform,false);strike.localPosition=new Vector3(.06f,.835f,0);
 var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.29f,0);collider.size=new Vector3(.48f,.58f,.48f);
 var face=new GameObject("WorkFaceCollider");face.transform.SetParent(root.transform,false);var box=face.AddComponent<BoxCollider>();box.center=new Vector3(0,.73f,0);box.size=new Vector3(.48f,.2f,.24f);
 string path=AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Prefabs/Anvil_UserModel.prefab");catalog.anvil=PrefabUtility.SaveAsPrefabAsset(root,path);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
 return path+"; triangles="+model.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)+"; height=.83m; materials=1";
 }finally{Object.DestroyImmediate(root);}
 }
 public static string Inspect(){AssetDatabase.Refresh();var g=AssetDatabase.LoadAssetAtPath<GameObject>(Model);var old=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets").anvil;return Model+"\n"+string.Join("\n",g.GetComponentsInChildren<Renderer>().Select(r=>r.name+" "+r.bounds))+"\n"+string.Join("\n",old.GetComponentsInChildren<Transform>().Select(t=>t.name+" "+t.position))+"\n"+string.Join("\n",AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Mesh>().Select(m=>m.name+" "+m.vertexCount+" "+m.bounds));}
}
