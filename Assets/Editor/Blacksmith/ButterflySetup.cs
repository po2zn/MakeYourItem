using UnityEngine;
using UnityEditor;
using System.Linq;
public static class ButterflySetup
{
 const string Path="Assets/Art/Blacksmith/Source/Butterfly/butterfly-base.fbx";
 public static string Build(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");
 AssetDatabase.Refresh();var catalog=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");
 var source=AssetDatabase.LoadAssetAtPath<GameObject>(Path);var g=Object.Instantiate(source);g.name="Butterfly";
 try{
 foreach(var a in g.GetComponentsInChildren<Animator>())Object.DestroyImmediate(a);
 foreach(var a in g.GetComponentsInChildren<Animation>())Object.DestroyImmediate(a);
 var map=new System.Collections.Generic.Dictionary<Material,Material>();
 foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!map.ContainsKey(m)){var n=new Material(Shader.Find("Universal Render Pipeline/Lit"));n.name="Butterfly_"+m.name;n.SetColor("_BaseColor",m.HasProperty("_Color")?m.color:Color.white);n.SetFloat("_Smoothness",.15f);n.SetFloat("_Cull",0);AssetDatabase.CreateAsset(n,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Materials/"+n.name+".mat"));map.Add(m,n);}return map[m];}).ToArray();
 catalog.butterfly=PrefabUtility.SaveAsPrefabAsset(g,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Prefabs/Butterfly.prefab"));EditorUtility.SetDirty(catalog);
 string path=AssetDatabase.GetAssetPath(catalog.blacksmith);var smith=PrefabUtility.LoadPrefabContents(path);
 try{
 var head=smith.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
 var mesh=smith.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
 var candidates=mesh.vertices.Where(v=>Mathf.Abs(v.x)<.055f&&v.y>1.3f&&v.y<1.55f).OrderByDescending(v=>v.z).ToArray();
 var tip=candidates.First();var anchor=new GameObject("ButterflyNoseAnchor").transform;anchor.SetParent(head,false);anchor.position=smith.transform.TransformPoint(tip+Vector3.up*.008f);anchor.rotation=smith.transform.rotation;
 PrefabUtility.SaveAsPrefabAsset(smith,path);
 AssetDatabase.SaveAssets();return "Butterfly imported: "+g.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3)+" triangles, "+map.Count+" materials; nose="+tip.ToString("F4");
 }finally{PrefabUtility.UnloadPrefabContents(smith);}
 }finally{Object.DestroyImmediate(g);}
 }
 public static string Inspect(){AssetDatabase.Refresh();var g=AssetDatabase.LoadAssetAtPath<GameObject>(Path);return string.Join("\n",g.GetComponentsInChildren<Transform>(true).Select(t=>t.name+" p="+t.position+" r="+t.localEulerAngles+" s="+t.localScale))+"\n"+string.Join("\n",AssetDatabase.LoadAllAssetsAtPath(Path).Select(a=>a.GetType().Name+" "+a.name));}
}
