using UnityEngine;
using UnityEditor;
using System.Linq;
public static class BlacksmithInspect {
 public static string Run() {
 AssetDatabase.Refresh();
 string s="";
 foreach(var id in new[]{"tradesman-1707b9","anvil-1286cb"}) {
 var p="Assets/Art/Blacksmith/Source/"+id+".fbx";
 var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);
 s+=p+"\n";
 if(!g)continue;
 foreach(var t in g.GetComponentsInChildren<Transform>(true)) s+=t.name+" "+t.localPosition+" scale="+t.localScale+"\n";
 foreach(var r in g.GetComponentsInChildren<Renderer>())s+="renderer="+r.name+" "+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m.name))+"\n";
 s+="clips="+string.Join(",",AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Select(c=>c.name))+"\n";
 } return s;
 }
}
