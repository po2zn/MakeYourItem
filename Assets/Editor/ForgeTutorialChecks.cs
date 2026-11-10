using System;

public static class ForgeTutorialChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Main()
    {
        var lesson=new ForgeTutorialLesson();string message;
        Require(lesson.RemainingIron==10,"initial practice iron");
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
        Require(lesson.Complete&&lesson.Evaluation.product.Kind==ForgeProductKind.Sword,"complete sword");
        Require(lesson.Evaluation.product.IronConsumed==2,"blade and handle use two iron");
        Require(lesson.Evaluation.steps.Count==6,"only five processes and submission");
        for(int i=0;i<5;i++)Require(lesson.Evaluation.steps[i].process==i+1,"process sequence");
        Require(!lesson.Submit("!Blade",out message),"no duplicate completion");
        Require(ForgeTutorialLesson.Restore(5,"print(1)").Stage==0,"bad checkpoint reset");
        Require(ForgeTutorialLesson.Restore(1,null).Stage==1,"selected material checkpoint");
        Require(ForgeTutorialLesson.Restore(99,lesson.Code).Stage==0,"unknown checkpoint reset");
        Console.WriteLine("PASS tutorial: material choice, variable, five processes, sword submission, mistakes/retries, checkpoint restore and duplicate prevention");
    }
}
