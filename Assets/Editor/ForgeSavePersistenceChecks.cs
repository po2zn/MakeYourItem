using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public static class ForgeSavePersistenceChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Get(ForgeMvp game,string name)=>typeof(ForgeMvp).GetField(name,Flags).GetValue(game);
    static void Set(ForgeMvp game,string name,object value)=>typeof(ForgeMvp).GetField(name,Flags).SetValue(game,value);
    static object Call(ForgeMvp game,string name,params object[] args)=>typeof(ForgeMvp).GetMethod(name,Flags).Invoke(game,args);
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static ForgeMvp Create(string name){
        var root=new GameObject(name);root.SetActive(false);
        var game=root.AddComponent<ForgeMvp>();
        typeof(ForgeMvp).GetProperty("DayCycle").GetSetMethod(true).Invoke(game,new object[]{root.AddComponent<ForgeDayCycle>()});
        Set(game,"windowsInitialized",true);return game;
    }
    static string Products(ForgeShop shop)=>JsonUtility.ToJson(new ForgeMvp.SavedProgress{nextProductOrder=shop.NextOrder,items=shop.Items.Select(e=>new ForgeMvp.SavedItem{order=e.Order,owner=e.Product.BlueprintName,material=e.Product.Material,heat=e.Product.FinalHeat,shaped=e.Product.Shaped,traits=e.Product.Traits,cost=e.Product.CashCost,iron=e.Product.IronConsumed,data=e.Product.Data,seconds=e.Product.ProductionSeconds}).ToList()});
    public static string Run(){
        const int slot=987651;
        const string key="MakeYourItem.Progress.v1.Slot987651";
        Require(!PlayerPrefs.HasKey(key),"Test slot already exists; refusing to overwrite");
        var source=Create("Persistence test source");var restored=Create("Persistence test restored");
        try{
            var tech=(ForgeTechnology)Get(source,"technology");
            tech.Restore(new[]{BlueprintFeature.Polish,BlueprintFeature.Plate},new[]{"speed-3","material-7"});
            var shop=(ForgeShop)Get(source,"shop");
            var data=new ForgeItemData{handle=2,hardness=7,polished=true,plated=true,star=true,charge=3,magicGrade=2,smithSaleMultiplier=1.25};
            data.runes.AddRange(new[]{1,5,9});data.materials[7]=1;data.materials[2]=1;
            shop.Add(new ForgeProduct("검 제작 도면",0,true,ForgeTraits.AirCooled,17,0,7,data,12.75),4);
            shop.Add(new ForgeProduct("철 검",0,true,ForgeTraits.None,0,2,1,new ForgeItemData{handle=1},8.5),9);
            shop.RestoreNextOrder(15);
            string nodes=string.Join(",",tech.ExportNodes()),products=Products(shop);
            source.OnBeforeSerialize();
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source),restored);restored.OnAfterDeserialize();
            Require(string.Join(",",((ForgeTechnology)Get(restored,"technology")).ExportNodes())==nodes,"Research lost on reload");
            Require(Products((ForgeShop)Get(restored,"shop"))==products,"Product details lost on reload");
            // Exercise the actual save -> title -> continue path after the reload.
            Set(restored,"activeSaveSlot",slot);Set(restored,"progressLoaded",true);Set(restored,"showTitle",false);
            Call(restored,"ReturnToTitle");
            Require(PlayerPrefs.HasKey(key),"Save was not written");
            ((ForgeTechnology)Get(restored,"technology")).Restore(Array.Empty<BlueprintFeature>());
            ((ForgeShop)Get(restored,"shop")).Clear();
            Call(restored,"EnterSavedSlot",slot);
            Require((bool)Get(restored,"progressLoaded"),"Load failed: "+Get(restored,"saveMessage"));
            Require(!(bool)Get(restored,"showTitle"),"Continue did not enter game");
            Require(string.Join(",",((ForgeTechnology)Get(restored,"technology")).ExportNodes())==nodes,"Research lost on continue");
            Require(Products((ForgeShop)Get(restored,"shop"))==products,"Products lost on continue");
            // Empty snapshots must remain empty (no resurrection after selling/resetting).
            ((ForgeShop)Get(restored,"shop")).Clear();
            ((ForgeTechnology)Get(restored,"technology")).Restore(Array.Empty<BlueprintFeature>());
            restored.OnBeforeSerialize();EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(restored),source);source.OnAfterDeserialize();
            Require(((ForgeShop)Get(source,"shop")).Count==0&&((ForgeTechnology)Get(source,"technology")).ExportNodes().Length==0,"Stale snapshot restored");
            return "PASS reload + save/title/continue: research levels, material unlocks, two swords, all item data, order, time and empty snapshots. User slots untouched.";
        }finally{
            Set(source,"progressLoaded",false);Set(restored,"progressLoaded",false);
            UnityEngine.Object.DestroyImmediate(source.gameObject);UnityEngine.Object.DestroyImmediate(restored.gameObject);
            PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
        }
    }
}
