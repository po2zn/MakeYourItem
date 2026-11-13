using System.Collections.Generic;
// Central tuning values; legacy purchased-node IDs are migrated by ForgeTechnology.
public static class ForgeBalance {
 public const int DefaultFurnaceTemperature=900, MinimumForgeTemperature=850, MaximumIdealTemperature=1100, QuenchBonus=19, PerfectQuenchBonus=81;
 public const int PolishBonus=75,PlateBonus=250,StarBonus=90,StarlightBonus=250,StarlightEngravingBonus=100,FrostBonus=80,ChargeBonus=150,DragonHandleBonus=380,DragonPulseBonus=120,ResonanceBonus=650,SealLayerBonus=140,MagicGradeBonus=25,RuneGradeBonus=20,TripleRuneBonus=60,CurseMultiplier=3;
 static readonly Dictionary<string,int> Research=new Dictionary<string,int>{
 {"automation-root",500},{"research-import",500},{"research-english-keywords",150000},{"function-Heat",500},{"function-Polish",800},{"material-5",3000},{"function-Plate",20000},{"material-7",50000},{"material-8",60000},{"function-Refine",10000},
 {"function-Inspect",1200},{"function-Charge",15000},{"function-Rune",30000},{"function-Magic",16000},{"function-Resonate",50000},{"function-Protect",300000},{"function-Curse",180000},
 {"function-Fuel",2400},{"function-Recover",1800},{"function-Wrap",8000},{"speed-1",1200},{"speed-2",6000},{"speed-3",30000},{"speed-4",80000},{"speed-5",150000},
 {"function-Lucky",500},{"function-IsDay",800},{"function-Abs",300},{"function-Min",5000},{"function-Len",300},{"function-Set",600},{"function-Random",1000},{"function-Match",5000},{"master-commission",500000}};
 public static int HandleBonus(int material)=>material==0?0:material==1?6:material==5?57:material==9?DragonHandleBonus:22;
 public static int ResearchPrice(string id,int fallback)=>Research.TryGetValue(id,out int value)?value:fallback;
}
