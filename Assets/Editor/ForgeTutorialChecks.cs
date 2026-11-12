using System;

public static class ForgeTutorialChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main()
    {
        var lesson=new ForgeTutorialLesson();string message;
        Require(lesson.RemainingIron==10,"initial practice iron");
        Require(!lesson.AdvanceSale(out message)&&lesson.Stage==0,"cannot sell before crafting");
        Require(!lesson.Submit("Material = 1",out message)&&lesson.Stage==0,"cannot skip material selection");
        for(int id=0;id<=9;id++)if(id!=1)Require(!lesson.SelectMaterial(id,out message)&&lesson.Stage==0,"incorrect material "+id);
        Require(lesson.SelectMaterial(1,out message)&&lesson.Stage==1,"iron selection");
        Require(!lesson.Submit("Material = 2",out message)&&lesson.Code=="","wrong variable value");
        Require(!lesson.Submit("Material = 1\nprint(1)",out message)&&lesson.Stage==1,"extra command rejected");
        Require(!lesson.Submit("Material =",out message)&&lesson.Stage==1,"syntax error is recoverable");
        for(int stage=1;stage<=7;stage++){
            Require(lesson.Stage==stage,"stage order");
            Require(!lesson.Submit(stage==7?"!Material":"!Blade",out message)&&lesson.Stage==stage,"cannot skip current action");
            Require(lesson.Submit(ForgeTutorialLesson.Hints[stage]+"  # practice",out message),"stage "+stage+": "+message);
            var restored=ForgeTutorialLesson.Restore(lesson.Stage,lesson.Code);
            Require(restored.Stage==lesson.Stage&&restored.Code==lesson.Code,"checkpoint "+stage);
            int expectedIron=stage<2?10:stage<6?9:8;
            Require(lesson.RemainingIron==expectedIron&&restored.RemainingIron==expectedIron,"practice stock checkpoint "+stage);
            Require(lesson.Evaluation.error==null,"runtime success "+stage);
        }
        Require(lesson.Crafted&&!lesson.Complete&&lesson.Evaluation.product.Kind==ForgeProductKind.Sword,"complete sword");
        Require(lesson.Evaluation.product.IronConsumed==2,"blade and handle use two iron");
        Require(lesson.Evaluation.steps.Count==6,"only five processes and submission");
        for(int i=0;i<5;i++)Require(lesson.Evaluation.steps[i].process==i+1,"process sequence");
        Require(!lesson.Submit("!Blade",out message),"no duplicate completion");
        Require(ForgeTutorialLesson.Restore(5,"print(1)").Stage==0,"bad checkpoint reset");
        Require(ForgeTutorialLesson.Restore(1,null).Stage==1,"selected material checkpoint");
        Require(ForgeTutorialLesson.Restore(99,lesson.Code).Stage==0,"unknown checkpoint reset");
        int price=ForgePricing.Quote(lesson.Evaluation.product).SalePrice;
        Require(lesson.AdvanceSale(out message)&&lesson.Stage==9&&!lesson.Complete&&lesson.PracticeGold==0,"select before sale");
        var selected=ForgeTutorialLesson.Restore(lesson.Stage,lesson.Code);
        Require(selected.Stage==9&&selected.AdvanceSale(out message)&&selected.PracticeGold==price,"resume selected sale");
        Require(lesson.AdvanceSale(out message)&&lesson.Complete&&lesson.PracticeGold==price,"sell once for quoted gold");
        Require(!lesson.AdvanceSale(out message)&&lesson.PracticeGold==price,"no duplicate sale");
        Require(ForgeTutorialLesson.Restore(10,lesson.Code).Complete&&ForgeTutorialLesson.Restore(10,lesson.Code).PracticeGold==price,"completed sale checkpoint");
        Require(lesson.Previous()&&lesson.Stage==9&&lesson.PracticeGold==0,"rewind sale");
        Require(lesson.Previous()&&lesson.Stage==8,"rewind selection");
        for(int target=7;target>=0;target--){
            Require(lesson.Previous()&&lesson.Stage==target,"previous stage "+target);
            Require(lesson.RemainingIron==(target<=2?10:target<=6?9:8),"rewind stock "+target);
            Require(ForgeTutorialLesson.Restore(lesson.Stage,lesson.Code).Stage==target,"rewind save "+target);
            if(target>0){
                var retry=ForgeTutorialLesson.Restore(lesson.Stage,lesson.Code);
                Require(retry.Submit(ForgeTutorialLesson.Hints[target],out message)&&retry.Stage==target+1,"relearn stage "+target);
            }
        }
        Require(!lesson.Previous()&&lesson.Code==""&&lesson.Evaluation==null,"first step boundary");
        lesson.SelectMaterial(1,out _);
        string document="";
        for(int stage=1;stage<=7;stage++){
            document+=ForgeTutorialLesson.Hints[stage]+"\n";
            Require(!lesson.SubmitDocument(document+"print(1)\n",out message)&&lesson.Stage==stage,"editor cannot skip a step");
            if(stage>1)Require(!lesson.SubmitDocument(document.Replace("Material = 1","Material = 2"),out message)&&lesson.Stage==stage,"invalid edit does not change checkpoint");
            Require(lesson.SubmitDocument(document,out message)&&lesson.Code==document,"editable document stage "+stage);
        }
        Require(lesson.Crafted&&!lesson.Complete&&lesson.RemainingIron==8,"editor completion without duplicate resource use");
        Require(lesson.AdvanceSale(out message)&&lesson.AdvanceSale(out message)&&lesson.Complete,"document then sale then enter game");
        for(int stage=2;stage<=6;stage++)Require(ForgeTutorialLesson.Instructions[stage].Contains("\n\n"),"blank line after function explanation "+stage);
        Require(ForgeTutorialLesson.Instructions[2].Contains("숫자 언어 못 들어보셨나요?"),"numeric language dialogue");
        Console.WriteLine("PASS tutorial: material choice, variable, five processes, sword submission, mistakes/retries, sale selection/payment, checkpoint restore, rewind and duplicate prevention");
    }
}
