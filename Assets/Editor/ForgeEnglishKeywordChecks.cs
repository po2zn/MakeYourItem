using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class ForgeEnglishKeywordChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<string> Run(){
        var tech=new ForgeTechnology();tech.Restore(Array.Empty<BlueprintFeature>(),new[]{ForgeTechnology.AutomationRoot,ForgeTechnology.ImportResearch});
        var examples=new[]{"while 1:\n    !!", "for n in [1]:\n    print(n)","if True:\n    print(1)","1?(False):\n    print(1)\nelif True:\n    print(2)","1?(False):\n    print(1)\nelse:\n    print(2)","888(1):\n    break","123(n in [1]):\n    continue","def test():\n    << 1\nprint(test())","333 test():\n    return 1\nprint(test())"};
        foreach(var source in examples){
            var denied=BlueprintRuntime.Evaluate(source,100,10,"test",1,technology:tech);
            Require(denied.compileError&&denied.error.Contains("영어 예약어"),"English bypass: "+source);
            using(var session=BlueprintRuntime.StartSession(source,100,10,technology:tech,deferErrors:true))Require(session.Completed&&session.Evaluation.compileError,"Deferred bypass");
        }
        var numeric="n = 0\n888(1):\n    n = n + 1\n    1?(n == 3):\n        !!\n888(0):\n    n = 999\nprint(n)";
        var result=BlueprintRuntime.Evaluate(numeric,100,10,"test",1,technology:tech);
        Require(result.error==null&&(int)result.values["n"]==3,"Numeric while failed: "+result.error);
        foreach(var value in new[]{"-1","2","True"})Require(BlueprintRuntime.Evaluate("888("+value+"):\n    !!\nprint(1)",100,10,"test",1,technology:tech).error==null,"Truthy condition failed");
        Require(BlueprintRuntime.Evaluate("# while return\nprint(\"for while return\")",100,10,"test",1,technology:tech).error==null,"Comments/strings blocked");
        var modules=new Dictionary<string,string>{{"other","while 1:\n    break"}};
        Require(BlueprintRuntime.Evaluate("import other",100,10,"test",1,technology:tech,modules:modules).error.Contains("영어 예약어"),"Import bypass");
        int gold=1000,iron=10;var stocks=Array.Empty<ForgeMaterialStock>();
        Require(tech.TryPurchase(ForgeTechnology.Find(ForgeTechnology.EnglishResearch),ref gold,ref iron,ref stocks,false,out var message),message);
        Require(gold==500,"Wrong research cost");
        foreach(var source in examples)Require(BlueprintRuntime.Evaluate(source,100,10,"test",1,technology:tech).error==null,"Unlocked failed: "+source);
        var saved=JsonUtility.FromJson<ForgeMvp.SavedProgress>(JsonUtility.ToJson(new ForgeMvp.SavedProgress{technologyNodes=tech.ExportNodes(),features=tech.Export()}));
        var restored=new ForgeTechnology();restored.Restore(saved.features,saved.technologyNodes);Require(restored.Owns(ForgeTechnology.EnglishResearch),"Research lost on save");
        using(var session=BlueprintRuntime.StartSession("888(1):\n    n = 1",100,10,technology:tech)){
            for(int i=0;i<20;i++)await session.Advance();Require(!session.Completed,"Loop ended prematurely");session.Dispose();Require(session.Cancelled,"Stop failed");
        }
        var limit=BlueprintRuntime.Evaluate("888(1):\n    n = 1",100,10,"test",1,technology:tech);Require(!limit.compileError&&limit.error!=null,"Missing loop safety limit");
        return "PASS 9 English controls locked/unlocked, numeric aliases, while integer truth, strings/comments, module and deferred checks, purchase/save, continuous loop cancellation and safety limit";
    }
}
