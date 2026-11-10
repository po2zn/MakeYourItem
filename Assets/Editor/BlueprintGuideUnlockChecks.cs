using System;
using System.Linq;
public static class BlueprintGuideUnlockChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static string Run(){
        var tech=new ForgeTechnology();var team=new ForgeBlacksmiths();
        Func<string,bool> visible=id=>BlueprintResearchGuide.EntryVisible(BlueprintGuide.Entries.First(e=>e.Id==id),tech,team);
        Check(visible("start")&&visible("1")&&visible("2")&&visible("3")&&visible("4"),"basic crafting must remain visible");
        Check(!visible("if")&&!visible("heat")&&!visible("abs")&&!visible("masterwork-example"),"locked entries leaked");
        Check(!tech.MaterialAvailable(4)&&tech.MaterialAvailable(3),"materials");
        foreach(var entry in BlueprintGuide.Entries.Where(e=>ForgeTechnology.RequiresPurchase(e.Feature)))Check(!BlueprintResearchGuide.EntryVisible(entry,tech,team),"locked feature "+entry.Id);
        tech.Restore(Array.Empty<BlueprintFeature>(),new[]{ForgeTechnology.AutomationRoot});
        Check(visible("if")&&visible("def")&&!visible("heat"),"automation bundle");
        tech.Restore(Array.Empty<BlueprintFeature>(),new[]{"function-IsDay","function-Lucky","speed-1"});
        Check(visible("is-day")&&visible("night-time"),"day bundle");
        Check(BlueprintResearchGuide.FeatureVisible(BlueprintFeature.LuckyCode,tech,team),"lucky bundle");
        Check(BlueprintResearchGuide.FeatureVisible(BlueprintFeature.Speed,tech,team)&&!BlueprintResearchGuide.FeatureVisible(BlueprintFeature.AutoSell,tech,team),"speed reward leaked");
        Check(!BlueprintResearchGuide.FeatureVisible(BlueprintFeature.SmithFavor,tech,team),"smith locked");team.favorUnlocked=true;
        Check(BlueprintResearchGuide.FeatureVisible(BlueprintFeature.SmithFavor,tech,team)&&!BlueprintResearchGuide.FeatureVisible(BlueprintFeature.SmithDemand,tech,team),"smith unlock isolation");
        tech.Restore(Array.Empty<BlueprintFeature>(),ForgeTechnology.Offers.Where(o=>o.Kind!=ForgePurchaseKind.Material).Select(o=>o.Id).ToArray());
        team.hireTier=5;team.positionUnlocked=true;team.demandUnlocked=true;
        foreach(var entry in BlueprintGuide.Entries)Check(BlueprintResearchGuide.EntryVisible(entry,tech,team),"fully unlocked entry hidden: "+entry.Id);
        tech.Restore(Array.Empty<BlueprintFeature>());
        Check(!BlueprintResearchGuide.UnlockedOverview(tech,false).Contains("스피드"),"overview leak");
        Check(!BlueprintResearchGuide.VisibleProse("기본 공정. 100(p)로 조회합니다. 별철을 사용합니다.",tech,team).Contains("100(p)"),"prose leak");
        return "PASS guide unlocks: basic crafting, locked features, bundles, materials, smith research, speed reward, all 77 entries, overview and prose.";
    }
}
