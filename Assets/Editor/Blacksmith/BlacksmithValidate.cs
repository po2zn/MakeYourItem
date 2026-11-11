using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using System.IO;
public static class BlacksmithValidate {
 public static string Inspect(){
 var scene=EditorSceneManager.OpenScene("Assets/Art/Blacksmith/Documentation/Set_20261111_171823/BlacksmithPreview.unity",OpenSceneMode.Additive);
 try{var g=scene.GetRootGameObjects().First(x=>x.name=="Blacksmith");string s=g.transform.position.ToString()+" scale="+g.transform.localScale+" active="+g.activeSelf;
 foreach(var r in g.GetComponentsInChildren<SkinnedMeshRenderer>()){var m=new Mesh();r.BakeMesh(m);s+="\n"+r.name+" enabled="+r.enabled+" bounds="+r.bounds+" baked="+m.bounds+" bones="+r.bones.Length;Object.DestroyImmediate(m);}return s;
 }finally{EditorSceneManager.CloseScene(scene,true);}}
}
