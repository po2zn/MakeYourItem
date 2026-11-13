using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class WorkshopImportedModels
{
 const string Output="Assets/Art/Blacksmith/Prefabs/ImportedWorkshop";
 public static Bounds BoundsOf(GameObject obj){var rs=obj.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)throw new Exception("No model renderers: "+obj.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject Build(string key,string pattern,float length,bool ground,bool height=false){
  var source=Directory.GetFiles("Assets/Art/Blacksmith",pattern,SearchOption.AllDirectories).Single().Replace('\\','/');
  var root=new GameObject(key);
  try{
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source),root.transform);model.name="Imported model";
   var b=BoundsOf(model);float size=height?b.size.y:Mathf.Max(b.size.x,b.size.y,b.size.z);if(size<=0)throw new Exception("Invalid model bounds "+source);
   model.transform.localScale*=length/size;b=BoundsOf(model);model.transform.position-=new Vector3(b.center.x,ground?b.min.y:b.center.y,b.center.z);
   string matPath=Output+"/"+key+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
   mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(source.Replace(".fbx",".png")));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.3f);mat.SetFloat("_Metallic",key.Contains("Rune")?.2f:0);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(mat);
   foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
   foreach(var c in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
   if(key=="FrostStation"){b=BoundsOf(model);var target=new GameObject("FrostTarget").transform;target.SetParent(root.transform,false);target.localPosition=new Vector3(0,b.size.y*.58f,0);}
   return PrefabUtility.SaveAsPrefabAsset(root,Output+"/"+key+".prefab");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 public static void BuildAll(){
  Directory.CreateDirectory(Output);AssetDatabase.Refresh();var a=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");if(!a)throw new Exception("Missing workshop catalog");
  a.crystalMaterial=Build("CrystalMaterial","Meshy_AI_Amethyst*.fbx",.28f,false);
  a.starIronMaterial=Build("StarIronMaterial","Meshy_AI_Starlight*.fbx",.38f,false);
  a.frostWaterMaterial=Build("FrostWaterMaterial","Meshy_AI_Frozen*.fbx",.26f,false);
  a.dragonBoneMaterial=Build("DragonBoneMaterial","Meshy_AI_Porous*.fbx",.45f,false);
  a.frostStation=Build("FrostStation","Meshy_AI_Arcane_Water*.fbx",.82f,true,true);
  a.giantRunePillar=Build("GiantRunePillar","Meshy_AI_Glowing_Rune*.fbx",1.25f,true,true);
  a.giantRuneCircle=Build("GiantRuneCircle","Meshy_AI_Runic_Portal*.fbx",1.8f,true);
  EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Artifacts/WorkshopModels");File.WriteAllText("Artifacts/WorkshopModels/models.txt",string.Join("\n",new[]{a.crystalMaterial,a.starIronMaterial,a.frostWaterMaterial,a.dragonBoneMaterial,a.frostStation,a.giantRunePillar,a.giantRuneCircle}.Select(p=>p.name+" "+BoundsOf(p))));
 }
 public static void BuildBatch(){try{BuildAll();Debug.Log("WORKSHOP_MODELS_BUILD_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
