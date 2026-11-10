using System;
using System.Collections.Generic;

// Pure lesson state: only validated, single-step exercises reach the interpreter.
public sealed class ForgeTutorialLesson
{
    public const int StartingIron=10;
    public int RemainingIron => StartingIron-(Evaluation==null?0:Evaluation.ironConsumed);
    public int Stage { get; private set; }
    public string Code { get; private set; }="";
    public bool Complete => Stage==8;
    public ForgeMvp.Evaluation Evaluation { get; private set; }
    public static readonly string[] Titles={"재료 번호 알아보기","변수에 이름 붙이기","1() · 작업대에 올리기","2() · 가열하기","3() · 검날 성형","4() · 냉각하기","5() · 손잡이 만들기","완성 검 제출"};
    public static readonly string[] Instructions={
        "재료는 0~9, 총 10종입니다. 번호는 개수가 아니라 재료의 종류를 뜻합니다. 아래 목록에서 기본 검날에 사용할 철을 골라 보세요.",
        "변수는 값을 기억하는 이름입니다. = 오른쪽의 값을 왼쪽 이름에 넣습니다. Material이라는 변수에 철의 번호 1을 저장하세요.",
        "1(재료 번호)은 재료를 작업대에 올리고 작업물을 돌려줍니다. Material을 사용해 올린 작업물을 Blade라는 변수에 담으세요.",
        "2(작업물, 열 단계)는 작업물을 가열합니다. Blade의 열을 7로 올리세요. 성형하려면 열이 6 이상이어야 합니다.",
        "3(작업물)은 뜨거운 재료를 두드려 검날로 만듭니다. 앞에서 가열한 Blade를 성형하세요.",
        "4(작업물, 냉각 재료)는 검날을 식힙니다. 공기의 번호는 0입니다. 공기로 Blade를 식혀 열을 0으로 만드세요.",
        "5(작업물, 손잡이 재료)는 식힌 검날에 손잡이를 만듭니다. 이번에는 철(1)로 손잡이를 만드세요. 검날과 손잡이에 철을 각각 1개씩 사용합니다.",
        "손잡이까지 달린 검이 준비됐습니다! ! 뒤에 작업물 변수 이름을 적어 완성품으로 제출하세요. !는 함수가 아닌 제출 기호입니다."};
    public static readonly string[] Hints={"철의 번호는 1입니다.","Material = 1","Blade = 1(Material)","2(Blade, 7)","3(Blade)","4(Blade, 0)","5(Blade, 1)","!Blade"};
    public bool SelectMaterial(int id,out string message)
    {
        if(Stage!=0){message="이미 재료를 선택했습니다.";return false;}
        if(id!=1){message="이번 검날은 철로 만듭니다. 번호 1을 찾아보세요.";return false;}
        Stage=1;message="철 선택 완료! 이제 번호를 변수에 담아 봅시다.";return true;
    }
    static bool Same(BlueprintExpr a,BlueprintExpr b)
    {
        if(a==null||b==null)return a==b;
        if(a.Kind!=b.Kind||a.Name!=b.Name||a.Feature!=b.Feature||a.Process!=b.Process||!Equals(a.Value,b.Value)||a.Args.Count!=b.Args.Count)return false;
        for(int i=0;i<a.Args.Count;i++)if(!Same(a.Args[i],b.Args[i]))return false;
        return true;
    }
    public bool Submit(string input,out string message)
    {
        message="현재 단계의 코드를 한 줄 작성해 주세요.";
        if(Stage<1||Complete||string.IsNullOrWhiteSpace(input)||input.Length>512)return false;
        input=input.Trim();
        if(input.IndexOf('\n')>=0||input.IndexOf('\r')>=0)return false;
        try {
            var actual=new BlueprintParser(input).Parse();
            var expected=new BlueprintParser(Hints[Stage]).Parse()[0];
            if(actual.Count!=1||actual[0].Kind!=expected.Kind||actual[0].Body.Count!=0||!Same(actual[0].Expr,expected.Expr)||!Same(actual[0].Target,expected.Target)){
                message="아직 목표와 다릅니다. 변수 이름과 인자를 확인하세요. 막히면 ‘힌트 보기’를 눌러 주세요.";return false;
            }
            string candidate=Code+input.Trim()+"\n";
            var result=ForgeMvp.Evaluate(candidate,100,StartingIron,"튜토리얼",materialStocks:new Dictionary<int,long>());
            if(result.error!=null){message=result.error;return false;}
            if(Stage==7&&(result.product==null||result.product.Kind!=ForgeProductKind.Sword)){message="손잡이까지 만든 검을 제출하세요.";return false;}
            Code=candidate;Evaluation=result;Stage++;
            message=Complete?"첫 검 완성! 변수와 1()~5()를 모두 사용했습니다.":"성공! 다음 단계로 진행합니다.";
            return true;
        }catch(BlueprintException error){message=error.Message;return false;}
    }
    public static ForgeTutorialLesson Restore(int stage,string code)
    {
        var lesson=new ForgeTutorialLesson();
        if(stage<=0||stage>8)return lesson;
        lesson.SelectMaterial(1,out _);
        foreach(string line in (code??"").Split('\n'))if(!string.IsNullOrWhiteSpace(line)&&!lesson.Submit(line,out _))return new ForgeTutorialLesson();
        return lesson.Stage==stage?lesson:new ForgeTutorialLesson();
    }
}
