using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ForgeTitleDisplayChecks
{
    public static string Run(){
        var root=new GameObject("Title history check");root.SetActive(false);
        var game=root.AddComponent<ForgeMvp>();
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var remember=typeof(ForgeMvp).GetMethod("RememberTitleSword",flags);
        var history=typeof(ForgeMvp).GetField("titleSwordHistory",flags);
        var restore=typeof(ForgeMvp).GetMethod("DisplayHistory",BindingFlags.Static|BindingFlags.NonPublic);
        void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        try{
            Require(((List<ForgeMvp.SavedItem>)history.GetValue(game)).Count==0,"New player display not empty");
            remember.Invoke(game,new object[]{new ForgeProduct("unfinished",5,false,ForgeTraits.None,0,1)});
            Require(((List<ForgeMvp.SavedItem>)history.GetValue(game)).Count==0,"Unfinished item displayed");
            for(int i=0;i<5;i++)remember.Invoke(game,new object[]{new ForgeProduct("sword"+i,0,true,ForgeTraits.None,0,1,1,new ForgeItemData{handle=2,polished=i==4},1)});
            var entries=(List<ForgeMvp.SavedItem>)history.GetValue(game);
            Require(entries.Count==3&&entries[0].owner=="sword2"&&entries[2].owner=="sword4","Newest three/order incorrect");
            var saved=JsonUtility.FromJson<ForgeMvp.SavedProgress>(JsonUtility.ToJson(new ForgeMvp.SavedProgress{titleSwords=entries,items=new List<ForgeMvp.SavedItem>()}));
            var loaded=(List<ForgeMvp.SavedItem>)restore.Invoke(null,new object[]{saved});
            Require(loaded.Count==3&&loaded[2].data.polished,"Sold sword history/appearance lost in save");
            var legacy=new ForgeMvp.SavedProgress{items=entries};
            Require(((List<ForgeMvp.SavedItem>)restore.Invoke(null,new object[]{legacy})).Count==3,"Legacy fallback failed");
            legacy.titleSwords=new List<ForgeMvp.SavedItem>();
            Require(((List<ForgeMvp.SavedItem>)restore.Invoke(null,new object[]{legacy})).Count==0,"Explicit empty history ignored");
            var rack=Resources.Load<GameObject>("BlacksmithTitleRack");
            Require(rack&&rack.transform.Find("Slot0")&&rack.transform.Find("Slot1")&&rack.transform.Find("Slot2"),"Rack/attachment slots missing");
            return "PASS empty / incomplete exclusion / newest three / sold history / appearance JSON / legacy fallback / explicit empty / 3D rack slots";
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
