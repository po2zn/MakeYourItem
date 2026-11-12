using System.Collections.Generic;
// Central tuning values. Material stock prices and existing purchased-node save IDs stay stable.
public static class ForgeBalance {
 public const int DefaultFurnaceTemperature=900, MinimumForgeTemperature=850, MaximumIdealTemperature=1100, QuenchBonus=19, PerfectQuenchBonus=81;
 public const int PolishBonus=25,PlateBonus=85,StarBonus=90,FrostBonus=80,ChargeBonus=150,DragonHandleBonus=380,DragonPulseBonus=120,ResonanceBonus=650,SealLayerBonus=140,MagicGradeBonus=25,RuneGradeBonus=20,TripleRuneBonus=60,CurseMultiplier=3;
 static readonly Dictionary<string,int> Research=new Dictionary<string,int>{
 {"automation-root",500},{"research-import",500},{"function-Heat",500},{"function-Polish",800},{"material-5",1800},{"function-Plate",2400},{"material-7",8000},{"material-8",18000},{"material-9",25000},{"function-Refine",50000},
 {"material-6",3200},{"function-Inspect",1200},{"function-Charge",6000},{"function-Rune",30000},{"function-Magic",16000},{"function-Resonate",50000},{"function-Protect",24000},{"function-Curse",180000},
 {"function-Fuel",2400},{"function-Recover",1800},{"function-Wrap",8000},{"speed-1",1200},{"speed-2",6000},{"speed-3",18000},{"speed-4",60000},{"speed-5",180000},
 {"function-Lucky",500},{"function-IsDay",800},{"function-Abs",300},{"function-Min",600},{"function-Max",1000},{"function-Len",300},{"function-Set",600},{"function-Random",1000},{"master-commission",90000}};
 public static int ResearchPrice(string id,int fallback)=>Research.TryGetValue(id,out int value)?value:fallback;
}
