using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ForgeBalanceRevisionChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void GuideBatch(){try{Require(BlueprintResearchGuide.FormatGold("999G 1,000G 150000G 1000000G 1000000000골드 90901()") == "999G 1KG 150KG 1MG 1B골드 90901()","guide KMB gold format");BlueprintGuideChecks.Main();ForgeTextLayoutChecks.Run();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void GiftBatch(){try{Debug.Log(ForgeBlacksmithChecks.Run());ForgeTextLayoutChecks.Run();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void AudioBatch(){try{
        var root=new GameObject("Audio test");var target=new GameObject("Smith test");
        try{var audio=root.AddComponent<ForgeActionAudio>();audio.SendMessage("Awake");target.transform.position=new Vector3(12,0,7);audio.Follow(target.transform);
            var sources=root.GetComponentsInChildren<AudioSource>();Require(sources.Length>=3,"three smith voices");
            foreach(var source in sources)Require(source.spatialBlend==1&&source.rolloffMode==AudioRolloffMode.Linear&&source.minDistance==3&&source.maxDistance==40&&source.dopplerLevel==0&&source.transform.position==target.transform.position+Vector3.up,"3D attenuation and smith position");
            target.transform.position=new Vector3(-5,0,20);audio.SendMessage("LateUpdate");foreach(var source in sources)Require(source.transform.position==target.transform.position+Vector3.up,"moving smith audio follows");
            audio.PlayDestruction();var clip=(AudioClip)typeof(ForgeActionAudio).GetField("destructionClip",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(audio);var pcm=new float[clip.samples];Require(clip.GetData(pcm,0)&&pcm.Any(v=>Math.Abs(v)>.05f)&&pcm.All(v=>!float.IsNaN(v)&&Math.Abs(v)<=1),"audible finite destruction sound");
            var motion=root.AddComponent<ForgeCraftMotion>();var reaction=motion.DestructionReaction();Require(reaction.MoveNext()&&motion.IsStunned,"destruction blocks work");var deadline=typeof(ForgeCraftMotion).GetField("stunnedUntil",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);Require(Math.Abs((float)deadline.GetValue(motion)-Time.time-2)<.1f,"two second stun");deadline.SetValue(motion,Time.time-1);Require(!reaction.MoveNext()&&!motion.IsStunned&&motion.ActiveProcess==0,"stun recovery");
            Debug.Log("PASS smith audio and destruction: spatial voices, movement tracking, audible shatter, two-second stun and recovery");
        }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(target);}
        EditorApplication.Exit(0);
    }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Run()
    {
        foreach(string spelling in new[]{"for i in ","123(i in "})foreach(int n in new[]{-3,0,1,3}){string loop=spelling+n+(spelling.StartsWith("123")?"):":":");var result=ForgeMvp.Evaluate("sum=0\n"+loop+"\n    sum=sum+i\nprint(sum)");Require(result.error==null&&(int)result.values["sum"]==(n>0?n*(n-1)/2:0),"integer for "+n);}
        var interrupted=ForgeMvp.Evaluate("sum=0\n123(i in 2147483647):\n    1?(i==3):\n        !!\n    sum=sum+i\nprint(sum)");Require(interrupted.error==null&&(int)interrupted.values["sum"]==3,"large integer loop breaks without allocating range");
        Require(ForgeMvp.Evaluate("123(i in True):\n    print(i)").error!=null,"boolean is not a repeat count");
        var rowOffer=ForgeTechnology.Find(ForgeTechnology.RowSaleResearch);Require(rowOffer.Price==22000&&rowOffer.Parents.SequenceEqual(new[]{"speed-3"}),"row price and gate");
        var rowShop=new ForgeShop();for(int i=0;i<14;i++)rowShop.Add(new ForgeProduct("row",0,true,ForgeTraits.AirCooled,0,0));var rowIds=rowShop.RowIds(rowShop.Items[8].Id,6);Require(rowIds.SequenceEqual(rowShop.Items.Skip(6).Take(6).Select(e=>e.Id)),"middle row");int rowGold=0;foreach(var id in rowIds)Require(rowShop.TrySell(id,ref rowGold,out _),"sale");Require(rowGold==168&&rowShop.Count==8&&rowShop.RowIds(rowShop.Items[7].Id,6).Length==2&&rowShop.RowIds(-1,6).Length==0,"row totals and partial/stale rows");
        var prices=new Dictionary<string,int>{{"speed-3",30000},{"speed-4",80000},{"speed-5",150000},{ForgeTechnology.EnglishResearch,150000},{"function-Min",5000},{"material-5",3000},{"function-Plate",20000},{"material-7",50000},{"material-8",60000},{"material-9",10000},{"material-6",15000},{ForgeTechnology.MasterCommission,500000},{"stock-4",20},{"stock-5",30}};
        foreach(var pair in prices)Require(ForgeTechnology.Find(pair.Key).Price==pair.Value,"price "+pair.Key);
        foreach(var pair in new[]{new[]{"function-Min","function-Max"},new[]{"function-Charge","material-6"},new[]{"function-Refine","material-9"}}){
            var offer=ForgeTechnology.Find(pair[0]);Require(offer==ForgeTechnology.Find(pair[1]),"single offer "+pair[0]);
            var tech=new ForgeTechnology();tech.Restore(Array.Empty<BlueprintFeature>(),offer.Parents.ToArray());
            int gold=offer.Price-1,iron=0;ForgeMaterialStock[] stocks=null;
            Require(!tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out _)&&!tech.Owns(pair[0])&&!tech.Owns(pair[1]),"atomic rejection");
            gold=offer.Price;Require(tech.TryPurchase(offer,ref gold,ref iron,ref stocks,false,out _)&&gold==0&&tech.Owns(pair[0])&&tech.Owns(pair[1]),"single payment unlocks both");
            Require(offer.Features.All(tech.Owns)&&offer.Features.All(ForgeTechnology.RequiresPurchase),"feature gates");
            if(offer.Material>0)Require(tech.MaterialAvailable(offer.Material)&&stocks.Single(s=>s.material==offer.Material).count==0,"supply without free stock");
            gold=offer.Price;Require(!tech.TryPurchase(ForgeTechnology.Find(pair[1]),ref gold,ref iron,ref stocks,false,out _)&&gold==offer.Price,"no duplicate payment");
            foreach(var oldId in pair){var restored=new ForgeTechnology();restored.Restore(Array.Empty<BlueprintFeature>(),new[]{oldId});Require(restored.Owns(pair[0])&&restored.Owns(pair[1]),"legacy purchase "+oldId);var again=new ForgeTechnology();again.Restore(restored.Export(),restored.ExportNodes());Require(again.ExportNodes().SequenceEqual(restored.ExportNodes()),"save roundtrip");}
        }
        foreach(var pair in new[]{new[]{0,0},new[]{1,2},new[]{2,22},new[]{5,57},new[]{7,22},new[]{9,380}}){
            var product=new ForgeProduct("handle",0,true,ForgeTraits.AirCooled,0,0,1,new ForgeItemData{handle=pair[0]});
            Require(ForgePricing.Quote(product).SalePrice==28+pair[1],"handle sale "+pair[0]);
            var shop=new ForgeShop();int id=shop.Add(product),gold=0;Require(shop.TrySell(id,ref gold,out _)&&gold==28+pair[1],"handle payout");
        }
        const string blade="p=1(1)\n2(p)\n3(p)\n4(p,0)\n";
        foreach(var pair in new[]{Tuple.Create("6(p)\n!p",103),Tuple.Create("6(p)\n7(p)\n!p",353),Tuple.Create("6(p)\n6(p)\n7(p)\n!p",353)}){
            var result=ForgeMvp.Evaluate(blade+pair.Item1);Require(result.error==null&&ForgePricing.Quote(result.product).SalePrice==pair.Item2,"polish/plate actual crafting payout");
        }
        var all=new ForgeTechnology();int balance=100000000,stock=0;ForgeMaterialStock[] inventory=null;
        ForgeTechnologyChecks.BuyAllNodes(all,ref balance,ref stock,ref inventory);
        Debug.Log("PASS balance revision: 14 prices, bundled purchases, atomicity, stock, legacy saves, 6 handle sale/payout cases, reachable purchase graph");
    }
    public static void UpdatePrefab()
    {
        const string path="Assets/UI/ForgeResearchUI.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var panel=root.GetComponent<ForgeTreePanel>();
            var cards=panel.cards.ToList();
            if(!cards.Any(c=>c.offerId==ForgeTechnology.RowSaleResearch)){var source=cards.Single(c=>c.offerId=="speed-3");var card=UnityEngine.Object.Instantiate(source,source.transform.parent);card.name=card.offerId=ForgeTechnology.RowSaleResearch;((RectTransform)card.transform).anchoredPosition=((RectTransform)source.transform).anchoredPosition+new Vector2(340,-200);cards.Add(card);}
            foreach(var pair in new[]{new[]{"function-Min","function-Max"},new[]{"function-Charge","material-6"},new[]{"function-Refine","material-9"}}){
                var keep=cards.Single(c=>c.offerId==pair[0]);var remove=cards.FirstOrDefault(c=>c.offerId==pair[1]);
                if(remove==null)continue;
                if(pair[1].StartsWith("material-"))((RectTransform)keep.transform).anchoredPosition=((RectTransform)remove.transform).anchoredPosition;
                cards.Remove(remove);UnityEngine.Object.DestroyImmediate(remove.gameObject);
            }
            panel.cards=cards.ToArray();
            foreach(var card in cards.Where(c=>!string.IsNullOrEmpty(c.offerId))){var offer=ForgeTechnology.Find(card.offerId);card.title.text=offer.Title;card.price.text=ForgeMaterialHud.FormatCount(offer.Price)+(offer.Kind==ForgePurchaseKind.Material?"G/개":"G");}
            var growth=panel.maps[0].content;var connections=growth.Find("Connections");
            var edges=panel.edges.Where(e=>!e.lines.Any(l=>l&&l.transform.IsChildOf(connections))).ToList();
            for(int i=connections.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(connections.GetChild(i).gameObject);
            var edgeMethod=typeof(ForgeResearchUIBuilder).GetMethod("Edge",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var positions=cards.Where(c=>!string.IsNullOrEmpty(c.offerId)&&ForgeTechnology.Find(c.offerId).Kind!=ForgePurchaseKind.Material).ToDictionary(c=>c.offerId,c=>CardRect((RectTransform)c.transform));
            var basic=CardRect((RectTransform)growth.Find("Cards/BaseWorkshop"));
            foreach(var offer in ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material)){
                if(offer.Parents.Count==0)edges.Add((ForgeTreePanel.Edge)edgeMethod.Invoke(null,new object[]{connections,basic,positions[offer.Id],null,-1}));
                foreach(var parent in offer.Parents)edges.Add((ForgeTreePanel.Edge)edgeMethod.Invoke(null,new object[]{connections,positions[parent],positions[offer.Id],parent,-1}));
            }
            panel.edges=edges.ToArray();PrefabUtility.SaveAsPrefabAsset(root,path);
            Debug.Log("PASS tree prefab: merged cards removed and canonical prerequisite edges rebuilt");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static Rect CardRect(RectTransform r)=>new Rect(r.anchoredPosition.x,-r.anchoredPosition.y,r.rect.width,r.rect.height);
    public static void Batch(){try{Run();ForgeTechnologyChecks.Main();Debug.Log(ForgeEconomyChecks.Run());Debug.Log(ForgeExpansionChecks.Run());UpdatePrefab();ForgeTextLayoutChecks.Run();AssetDatabase.SaveAssets();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
