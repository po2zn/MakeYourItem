using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
public static class WorkshopMotionAssets
{
 public static string FitFloor(){var path=AssetDatabase.GetAssetPath(Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets").floor);var g=PrefabUtility.LoadPrefabContents(path);try{g.transform.GetChild(0).localScale=new Vector3(4.35f,.12f,4.35f);PrefabUtility.SaveAsPrefabAsset(g,path);return "Floor 4.35m x 4.35m; top y=0";}finally{PrefabUtility.UnloadPrefabContents(g);}}
 public static string Build(){
 if(EditorApplication.isPlaying)throw new System.Exception("Exit Play first");AssetDatabase.Refresh();
 var catalog=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");
 var steel=new Material(Shader.Find("Universal Render Pipeline/Lit"));steel.name="TongsIron";steel.SetColor("_BaseColor",new Color(.17f,.19f,.21f));steel.SetFloat("_Metallic",.6f);AssetDatabase.CreateAsset(steel,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Materials/TongsIron.mat"));
 var tongs=new GameObject("SmithTongs");
 foreach(float sign in new[]{-1f,1f}){var bar=GameObject.CreatePrimitive(PrimitiveType.Cube);bar.name=sign<0?"LeftJaw":"RightJaw";Object.DestroyImmediate(bar.GetComponent<Collider>());bar.transform.SetParent(tongs.transform,false);bar.transform.localPosition=new Vector3(sign*.018f,0,.20f);bar.transform.localScale=new Vector3(.018f,.018f,.44f);bar.GetComponent<Renderer>().sharedMaterial=steel;}
 catalog.tongs=PrefabUtility.SaveAsPrefabAsset(tongs,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Prefabs/SmithTongs.prefab"));Object.DestroyImmediate(tongs);
 var texturePath=Directory.GetFiles("Assets/Art/Blacksmith", "*.png",SearchOption.AllDirectories).Single(p=>Path.GetFileName(Path.GetDirectoryName(p))=="바닥").Replace('\\','/');
 var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);if(!texture)throw new System.Exception("Floor texture import failed");
 var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="WorkshopFloor";mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Materials/WorkshopFloor.mat"));
 // A saved, UV-mapped slab; top is exactly y=0, so existing foot/prop ground levels remain valid.
 var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="WorkshopFloor";floor.transform.position=new Vector3(2.4f,-.06f,1.1f);floor.transform.localScale=new Vector3(4.7f,.12f,4.5f);floor.GetComponent<Renderer>().sharedMaterial=mat;
 var holder=new GameObject("WorkshopFloorRoot");floor.transform.SetParent(holder.transform,true);
 catalog.floor=PrefabUtility.SaveAsPrefabAsset(holder,AssetDatabase.GenerateUniqueAssetPath("Assets/Art/Blacksmith/Prefabs/WorkshopFloor.prefab"));Object.DestroyImmediate(holder);
 EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();return "Saved tongs and floor with texture: "+texturePath;
 }
}
