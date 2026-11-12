using System;
using System.Collections.Generic;
using UnityEngine;

public partial class ForgeMvp
{
    [SerializeField,HideInInspector] List<SavedItem> titleSwordHistory=new List<SavedItem>();
    GameObject titleSwordRack;
    static SavedItem CaptureTitleSword(ForgeProduct p)=>new SavedItem{owner=p.BlueprintName,material=p.Material,heat=p.FinalHeat,shaped=p.Shaped,traits=p.Traits,cost=p.CashCost,iron=p.IronConsumed,data=p.Data,seconds=p.ProductionSeconds};
    void RememberTitleSword(ForgeProduct product){
        if(product.Kind!=ForgeProductKind.Sword)return;
        titleSwordHistory.Add(CaptureTitleSword(product));
        while(titleSwordHistory.Count>3)titleSwordHistory.RemoveAt(0);
    }
    static List<SavedItem> DisplayHistory(SavedProgress saved){
        var result=new List<SavedItem>();
        foreach(var item in saved.titleSwords??saved.items??new List<SavedItem>()){
            if(item!=null&&RestoreProduct(item).Kind==ForgeProductKind.Sword)result.Add(item);
        }
        if(result.Count>3)result.RemoveRange(0,result.Count-3);
        return result;
    }
    void RefreshTitleDisplay(){
        if(!Application.isPlaying)return;
        if(titleSwordRack){titleSwordRack.SetActive(false);Destroy(titleSwordRack);}
        var prefab=Resources.Load<GameObject>("BlacksmithTitleRack");
        if(!prefab)return;
        titleSwordRack=Instantiate(prefab,transform);titleSwordRack.name="Title sword display";
        titleSwordRack.transform.position=new Vector3(.1f,0,.9f);
        titleSwordRack.transform.localScale=Vector3.one*.65f;
        SavedProgress newest=null;long newestTime=long.MinValue;
        for(int slot=1;slot<=SlotCount;slot++){
            if(!PlayerPrefs.HasKey(SlotKey(slot)))continue;
            try{
                var saved=JsonUtility.FromJson<SavedProgress>(PlayerPrefs.GetString(SlotKey(slot)));
                if(saved==null||saved.version!=1||saved.day<1)continue;
                long time=saved.savedAtUtcTicks;
                if(time==0&&DateTime.TryParse(saved.savedAt,out var date))time=date.ToUniversalTime().Ticks;
                if(time>=newestTime){newest=saved;newestTime=time;}
            }catch(ArgumentException){ }
        }
        var assets=Resources.Load<ForgeWorkshopAssets>("BlacksmithWorkshopAssets");
        if(newest==null||!assets||!assets.sword)return;
        var history=DisplayHistory(newest);
        for(int i=0;i<history.Count;i++){
            var item=history[i];var sword=Instantiate(assets.sword,titleSwordRack.transform.Find("Slot"+i));
            sword.name="Displayed sword - "+item.owner;
            sword.transform.localPosition=Vector3.zero;sword.transform.localRotation=Quaternion.Euler(0,0,180);
            foreach(var collider in sword.GetComponentsInChildren<Collider>())collider.enabled=false;
            var appearance=sword.GetComponent<ForgeSwordAppearance>()??sword.AddComponent<ForgeSwordAppearance>();
            appearance.Apply(item.material,item.shaped,item.data);
        }
    }
    void UpdateTitleDisplay(){if(titleSwordRack&&titleSwordRack.activeSelf!=showTitle)titleSwordRack.SetActive(showTitle);}
}
