using System;
using System.Linq;
using System.Text;
public static class BlueprintGuideChecks {
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static string Runnable(BlueprintGuide.Entry entry,bool unlocked){
 string code=entry.ExampleFor(unlocked);
 if(entry.Id.StartsWith("extension-"))return code;
 switch(entry.Id){
 case "1":return code+"\n2(Blade)\n3(Blade)\n4(Blade, 0)\n!Blade";
 case "2":return "Blade = 1(1)\n"+code+"\n3(Blade)\n4(Blade, 0)\n!Blade";
 case "3":return "Blade = 1(1)\n2(Blade)\n"+code+"\n4(Blade, 0)\n!Blade";
 case "4":return "Blade = 1(1)\n2(Blade)\n3(Blade)\n"+code+"\n!Blade";
 case "finish":return "Blade = 1(1)\n2(Blade)\n3(Blade)\n4(Blade, 0)\n"+code;
 case "heat":return "Blade = 1(1)\n2(Blade)\n"+code+"\n3(Blade)\n4(Blade, 0)\n!Blade";
 default:return entry.IsFunctionReference?code+"\nprint(\"검증\")":code;
 }
 }
 static ForgeMvp.Evaluation EvaluateGuide(string source)=>ForgeMvp.Evaluate(source,100,10,modules:new System.Collections.Generic.Dictionary<string,string>{{"smith",BlueprintImportChecks.Smith}},context:ForgeBlacksmithChecks.GuideContext());
 public static string Run(){var log=new StringBuilder();var entries=BlueprintGuide.Entries;
 Require(entries.Select(e=>e.Id).Distinct().Count()==entries.Count,"duplicate guide IDs");
 foreach(BlueprintFeature feature in Enum.GetValues(typeof(BlueprintFeature)))if(feature!=BlueprintFeature.None)Require(entries.Count(e=>e.Feature==feature)==1,"missing/duplicate feature "+feature);
 foreach(var e in entries){Require(e.Body.Length>0&&e.Expected.Length>0&&e.Example.Length>0,"empty guide "+e.Id);var r=EvaluateGuide(Runnable(e,true));Require(r.error==null,e.Id+": "+r.error);log.AppendLine("PASS guide "+e.Id);if(new[]{"start","stages","choose"}.Contains(e.Id)){var q=ForgePricing.Quote(r.product);Require(r.cost==0&&r.ironConsumed==1&&q.SalePrice==28&&q.Profit==28,"guide economy "+e.Id);}}
 var noCandidate=ForgeMvp.Evaluate(entries.Single(e=>e.Id=="choose").Example.Replace("choose([9, 7, 5], 7)","choose([9, 8], 7)"),100,10);Require(noCandidate.error==null&&noCandidate.cost==0&&noCandidate.ironConsumed==0&&noCandidate.product==null,"no candidate must not craft");
 Require(entries.Single(e=>e.Id=="min").Matches("000")&&entries.Single(e=>e.Id=="min").Matches("MIN")&&entries.Single(e=>e.Id=="2").Matches("가열")&&entries.All(e=>e.Matches(" ")),"guide search");
 foreach(var e in entries){
 var numeric=e.ExampleFor(false);var r=EvaluateGuide(Runnable(e,false));var original=EvaluateGuide(Runnable(e,true));
 Require(r.error==null,"numeric guide "+e.Id+": "+r.error);
 Require(r.cost==original.cost&&r.ironConsumed==original.ironConsumed&&r.steps.Select(x=>x.text).SequenceEqual(original.steps.Select(x=>x.text)),"numeric result mismatch "+e.Id);
 Require(e.HeadingFor(true)==e.Heading&&e.DescriptionFor(true)==e.Description&&e.ExampleFor(true)==e.Example&&e.NoteFor(true)==e.Note,"unlock restoration "+e.Id);
 string visible=e.HeadingFor(false)+"\n"+e.DescriptionFor(false)+"\n"+numeric+"\n"+e.Expected+"\n"+e.NoteFor(false);
 foreach(var alias in BlueprintAliases.All)if(alias.Symbol.Length>0)Require(!System.Text.RegularExpressions.Regex.IsMatch(visible,@"(?<![A-Za-z0-9_])"+alias.English+@"(?![A-Za-z0-9_])"),"English leak "+e.Id+" / "+alias.English);
 }
 Require(!entries.Single(e=>e.Id=="min").Matches("min",false)&&entries.Single(e=>e.Id=="min").Matches("min",true)&&entries.Single(e=>e.Id=="min").Matches("000",false),"locked/unlocked search");
 Require(entries.Single(e=>e.Id=="print").HeadingFor(false).Contains("print"),"print must remain documented");

 foreach(var e in entries){
   Require(e.ShowResearch?e.ResearchFor(true).Length>0&&e.ResearchFor(false).Length>0:e.ResearchFor(true).Length==0&&e.ResearchFor(false).Length==0,"research section visibility "+e.Id);
   foreach(var alias in BlueprintAliases.All.Where(a=>a.Symbol.Length>0))Require(!System.Text.RegularExpressions.Regex.IsMatch(e.ResearchFor(false),@"(?<![A-Za-z0-9_])"+alias.English+@"(?![A-Za-z0-9_])"),"research English leak "+e.Id+" / "+alias.English);
 }
 string treeGuide=BlueprintResearchGuide.Overview(true);
 foreach(var offer in ForgeTechnology.Offers)Require(treeGuide.Contains(offer.Kind==ForgePurchaseKind.Material?ForgeMaterialGuide.Find(offer.Material).Heading:offer.Title),"missing current tree offer "+offer.Id);
 Require(entries.Single(e=>e.Feature==BlueprintFeature.AutoSell).ResearchFor(false).Contains("자동판매 노드를 별도로 구매"),"auto-sell purchase metadata");
 Require(!entries.Single(e=>e.Feature==BlueprintFeature.SmithRun).ShowResearch&&entries.Single(e=>e.Feature==BlueprintFeature.SmithRun).ResearchFor(false)==""&&entries.Single(e=>e.Feature==BlueprintFeature.SmithRun).NoteFor(false)=="","smith run guide shows usage without research or extra notes");
 var master=EvaluateGuide(entries.Single(e=>e.Id=="masterwork-example").Example);
 Require(master.error==null&&master.product.Data.materials[7]==1&&master.product.Data.materials[5]==1&&master.product.Data.materials[9]==1&&master.product.Data.materials[3]==2&&master.product.Data.materials[6]==4,"masterwork example stock requirements");
 log.AppendLine("PASS current tree offer coverage, research metadata, speed reward, smith hiring and masterwork materials");
 log.AppendLine("PASS numeric-only examples, equal execution, hidden English, unlock restoration and search");
 log.AppendLine(BlueprintGuideUnlockChecks.Run());
 log.AppendLine("TOTAL guide "+entries.Count+" examples passed; registry, economy, no-candidate and search passed");return log.ToString();}
 public static void Main(){Console.WriteLine(Run());BlueprintVariableChecks.Main();}
#if UNITY_EDITOR
 [UnityEditor.MenuItem("Tools/MakeYourItem/Validate Guide Examples")]
 public static void ValidateInEditor(){UnityEngine.Debug.Log(Run());}
#endif
}
