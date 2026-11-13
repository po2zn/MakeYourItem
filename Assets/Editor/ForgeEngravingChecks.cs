using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
public static class ForgeEngravingChecks {
 static void Check(bool c,string text){if(!c)throw new Exception(text);}
 static ForgeMvp.Piece Piece()=>new ForgeMvp.Piece{id=1,material=7,shaped=true,data=new ForgeItemData{starlight=new ForgeStarlight(new[]{5,0,2,5,2})}};
 static ForgeMvp.Evaluation Run(ForgeMvp.Piece p,string code,double hour,ForgeTechnology tech=null)=>BlueprintRuntime.Evaluate(code,0,0,"각인",17,technology:tech,clockHour:()=>hour,context:new ForgeRunContext{PracticePiece=p});
 public static string Run(){
 foreach(double hour in new[]{0,0.999,1,2,4.999,5,12,23.999}){var p=Piece();var e=Run(p,"74(StarIron)\n!StarIron",hour);Check(e.error==null,e.error);Check(p.data.starlight.Engraved==(hour>=1&&hour<5),"time boundary "+hour);Check(ForgePricing.Bonuses(e.product).ContainsKey("별빛 각인")==p.data.starlight.Engraved,"sale bonus "+hour);Check(Math.Abs(e.product.ProductionSeconds-.8)<.0001,"one duration");}
 var target=Piece();var once=Run(target,"74(StarIron)",1);Check(once.error==null,once.error);Run(target,"74(StarIron)\n74(StarIron)",12);Check(target.data.starlight.EngravingBonus==100,"no duplicate/later expiry");Check(target.data.starlight.Values.SequenceEqual(target.data.starlight.Initial),"no automatic refinement");
 var refined=Run(target,ForgeStarlightChecks.RefineCode+"\n!StarIron",3);Check(refined.error==null,refined.error);var bonuses=ForgePricing.Bonuses(refined.product);Check(bonuses["별빛 각인"]==100&&bonuses["별빛 정제"]==250,"separate bonuses");
 var shop=new ForgeShop();int id=shop.Add(refined.product),gold=0;Check(shop.TrySell(id,ref gold,out var sold),"sale");int paid=gold;Check(!shop.TrySell(id,ref gold,out sold)&&gold==paid,"sale once");
 foreach(string code in new[]{"74()","74(StarIron, 1)","74(1)","74(True)"}){var p=Piece();var e=Run(p,"x = 1\n"+code,2);Check(e.error!=null&&e.errorLine==2&&!p.data.starlight.Engraved,"atomic invalid "+code);}
 foreach(int state in new[]{0,1,2}){var p=Piece();if(state==0)p.material=1;if(state==1)p.data.destroyed=true;if(state==2)p.data.recovered=true;Check(Run(p,"74(StarIron)",2).error!=null&&!p.data.starlight.Engraved,"invalid target");}
 var source=target.data.starlight;using(var practice=new ForgeStarlightPractice(source)){Check(!practice.Piece.data.starlight.Engraved&&source.Engraved,"practice isolated reset");}
 var protect=ForgeTechnology.Find("function-Protect");Check(protect.Price==300000&&protect.Parents.SequenceEqual(new[]{"function-Curse"}),"protect research");var tech=new ForgeTechnology();int money=300000,iron=0;ForgeMaterialStock[] stocks=Array.Empty<ForgeMaterialStock>();Check(!tech.TryPurchase(protect,ref money,ref iron,ref stocks,false,out _)&&money==300000,"requires curse");tech.Restore(null,new[]{"function-Curse"});Check(!tech.Owns(BlueprintFeature.Protect),"curse does not grant protect");Check(tech.BlockReason(protect,299999,0,stocks,false)!=null,"300K minimum");Check(tech.TryPurchase(protect,ref money,ref iron,ref stocks,false,out _)&&money==0,"purchased");var restored=new ForgeTechnology();restored.Restore(tech.Export(),tech.ExportNodes());Check(restored.Owns(BlueprintFeature.Protect),"saved research");
 Check(Run(Piece(),"74(StarIron)",2,new ForgeTechnology()).error!=null,"locked engraving");tech.Restore(null,new[]{"material-7"});Check(Run(Piece(),"74(StarIron)",2,tech).error==null,"star unlock engraving");
 foreach(var pair in new[]{(BlueprintFeature.Speed,"제작 속도"),(BlueprintFeature.AutoSell,"자동판매"),(BlueprintFeature.Refine,"용뼈 정제"),(BlueprintFeature.Curse,"저주 부여")})foreach(bool english in new[]{false,true})Check(BlueprintGuide.Entries.Single(e=>e.Feature==pair.Item1).HeadingFor(english).Contains(pair.Item2),"guide title "+pair.Item1);
 var entry=BlueprintGuide.Entries.Single(e=>e.Feature==BlueprintFeature.StarlightEngrave);Check(BlueprintResearchGuide.EntryVisible(entry,tech,null),"guide unlocked");Check(BlueprintRuntime.Evaluate(entry.ExampleFor(false),0,0,"guide",1,technology:tech,clockHour:()=>2).error==null,"guide example executes");
 return "PASS 74 time boundaries, atomic errors, unlock, independent +100G, sale once, practice reset, 667 research and guide titles";
 }
 public static void UnityChecks(){var light=new ForgeStarlight(new[]{5,0,2,5,2});light.Engrave(2);var copy=JsonUtility.FromJson<ForgeStarlight>(JsonUtility.ToJson(light));Check(copy.Engraved&&copy.Clone().EngravingBonus==100,"save roundtrip");var old=JsonUtility.FromJson<ForgeStarlight>("{\"initial\":[5,0,2,5,2],\"values\":[5,0,2,5,2],\"revealed\":[false,false,false,false,false]}");Check(!old.Engraved,"old save default");}
}
