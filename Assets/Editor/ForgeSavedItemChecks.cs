using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
public static class ForgeSavedItemChecks {
 static T Empty<T>()=>(T)FormatterServices.GetUninitializedObject(typeof(T));
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static ForgeProduct Restore(ForgeItemData data)=>new ForgeProduct("legacy",0,true,ForgeTraits.None,0,1,1,data);
 public static string Run(){
  var empty=Empty<ForgeItemData>();empty.dragonBone=Empty<ForgeDragonBone>();empty.starlight=Empty<ForgeStarlight>();empty.cooling=Empty<ForgeCoolingGraph>();
  Require(empty.dragonBone.Clone()==null&&empty.starlight.Clone()==null&&empty.cooling.Clone()==null,"constructor-free empty payloads");
  var restored=Restore(empty);var data=restored.Data;Require(data.dragonBone==null&&data.starlight==null&&data.cooling!=null,"optional data discarded without granting rewards");Require(data.materials.Length==10&&data.seals.Count==0&&data.runes.Count==0,"missing collections");Require(ForgePricing.Quote(restored).SalePrice==28,"legacy base price");
  var bone=new ForgeDragonBone(10,new[]{2,4,7});foreach(int pos in new[]{4,2,7}){Action cancel;bone.BeginCut(pos,1,out cancel)();}bone.Heated(1010);bone.Use();
  var light=new ForgeStarlight(new[]{5,0,2,5,2});light.Read(0);light.Remove(1,1);
  var valid=new ForgeItemData{dragonBone=bone,starlight=light,cooling=ForgeCoolingGraph.Tutorial(),handle=9,materials=new[]{0,2},seals=new List<int>(),runes=new List<int>{1}};
  var clone=valid.Clone();Require(clone.dragonBone.Qualified&&clone.dragonBone.Cost==20&&clone.dragonBone.Used&&clone.dragonBone.Position(0)==2,"real bone state preserved");Require(clone.starlight.Count==4&&clone.starlight.Revealed(0)&&clone.cooling.Cost(0,1)==2,"other processing preserved");Require(clone.materials.Length==10&&clone.materials[1]==2,"short old material array");clone.starlight.Remove(0,1);clone.materials[1]=0;Require(light.Count==4&&valid.materials[1]==2,"independent clone");Require(ForgePricing.Bonuses(Restore(valid))["용뼈 절단 후 가열"]==200,"valid reward preserved");
  typeof(ForgeDragonBone).GetField("cuts",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(bone,null);Require(bone.Clone()==null,"partial bone missing cuts");
  foreach(int pass in new[]{0,1,2}){data=Restore(data).Data;Require(data.dragonBone==null&&data.starlight==null,"repeated history reads safe");}
  return "Saved-item checks passed: absent/empty/partial payloads, collection defaults, legacy pricing, valid progress/rewards, deep copies, repeated history restoration";
 }
 [MenuItem("Tools/Validation/Saved Item Compatibility")]
 public static void RunUnity(){Run();foreach(string json in new[]{"{}","{\"dragonBone\":{},\"starlight\":{},\"cooling\":{}}","{\"handle\":9,\"materials\":[],\"seals\":null,\"runes\":null}"}){var data=JsonUtility.FromJson<ForgeItemData>(json);var p=Restore(data);Restore(JsonUtility.FromJson<ForgeItemData>(JsonUtility.ToJson(p.Data)));Require(p.Data.dragonBone==null,"JSON missing bone must not generate state");}Debug.Log("PASS Saved Item Compatibility including Unity JSON round trips");}
}
