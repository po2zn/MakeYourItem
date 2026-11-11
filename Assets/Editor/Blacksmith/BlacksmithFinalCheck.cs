using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.IO;
public static class BlacksmithFinalCheck {
 public static string Run(){
 const string root="Assets/Art/Blacksmith/";const string set="Set_20261111_171955";
 var scene=EditorSceneManager.OpenScene(root+"Documentation/"+set+"/BlacksmithPreview.unity",OpenSceneMode.Additive);
 try{
 var roots=scene.GetRootGameObjects();var smith=roots.First(g=>g.name=="Blacksmith");smith.transform.rotation=Quaternion.Euler(0,155,0);
 var bucket=roots.First(g=>g.name=="QuenchBucket");var bottom=bucket.transform.Find("BottomCollider");UnityEngine.Object.DestroyImmediate(bottom.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(bottom.GetComponent<MeshFilter>());
 var prefabPath=root+"Prefabs/"+set+"/QuenchBucket.prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);try{var b=prefab.transform.Find("BottomCollider");UnityEngine.Object.DestroyImmediate(b.GetComponent<MeshRenderer>());UnityEngine.Object.DestroyImmediate(b.GetComponent<MeshFilter>());PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
 string report="Saved prefab checks:\n";
 foreach(string p in Directory.GetFiles(root+"Prefabs/"+set,"*.prefab")){
 var obj=AssetDatabase.LoadAssetAtPath<GameObject>(p);int tris=0;foreach(var f in obj.GetComponentsInChildren<MeshFilter>()){if(!f.sharedMesh)throw new Exception("Missing mesh");tris+=f.sharedMesh.triangles.Length/3;}
 foreach(var r in obj.GetComponentsInChildren<SkinnedMeshRenderer>()){tris+=r.sharedMesh.triangles.Length/3;if(r.bones.Any(b=>!b))throw new Exception("Missing bone");}
 foreach(var r in obj.GetComponentsInChildren<Renderer>())if(r.sharedMaterials.Any(m=>!m||m.shader.name!="Universal Render Pipeline/Lit"))throw new Exception("Invalid URP material");
 report+=obj.name+": "+tris+" triangles; references and URP materials OK\n";
 }
 foreach(var t in smith.GetComponentsInChildren<Transform>())if(float.IsNaN(t.position.x)||float.IsInfinity(t.position.x))throw new Exception("Non-finite character transform");
 EditorSceneManager.SaveScene(scene);var cam=roots.First(g=>g.name=="PreviewCamera").GetComponent<Camera>();
 foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3840,2160)}){
 var rt=new RenderTexture(size.x,size.y,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,size.x,size.y),0,0);tex.Apply();File.WriteAllBytes(root+"Documentation/"+set+"/Preview_"+size.x+"x"+size.y+".png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
 }
 File.WriteAllText(root+"Documentation/"+set+"/Validation.txt",report);AssetDatabase.SaveAssets();AssetDatabase.Refresh();return report;
 }finally{EditorSceneManager.CloseScene(scene,true);}
 }
}
