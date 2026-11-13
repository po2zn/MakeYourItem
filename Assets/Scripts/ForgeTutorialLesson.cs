using System;
using System.Collections.Generic;

// Pure lesson state: only validated, single-step exercises reach the interpreter.
public sealed class ForgeTutorialLesson
{
    public const int StartingIron=10;
    public int RemainingIron => StartingIron-(Evaluation==null?0:Evaluation.ironConsumed);
    public int Stage { get; private set; }
    public string Code { get; private set; }="";
    public bool Crafted => Stage>=8;
    public bool Sold => Stage>=10;
    public bool Complete => Stage==11;
    ForgeShop practiceShop;
    public ForgeShop PracticeShop { get { if(practiceShop==null){practiceShop=new ForgeShop();if(Crafted&&!Sold)practiceShop.Add(Evaluation.product);}return practiceShop; } }
    public bool ReturnToForge(){if(Stage!=10)return false;Stage=11;return true;}
    public int PracticeGold { get; private set; }
    public string Heading => Crafted?(Complete?"튜토리얼 완료":"첫 검 판매 · "+(Stage-7)+" / 3"):"첫 검 만들기 · "+(Stage+1)+" / 8";
    public string SaleAction => Sold?"대장간으로 돌아가기":Stage==8?"완성한 검 선택":"선택한 검 판매 · +"+ForgePricing.Quote(Evaluation.product).SalePrice+"G";
    public string SaleInstructions => Complete?"첫 검의 제작과 판매를 모두 배웠습니다!\n\n이제 직접 도면을 작성하고, 판매한 골드로 재료와 테크 트리를 구매해 보세요.\n\n‘튜토리얼 마치기’를 누르면 본게임이 시작됩니다.":Sold?"판매 완료! 연습 골드 +"+PracticeGold+"G\n\n반짝이는 ‘대장간으로’ 버튼을 눌러 돌아가세요.":Stage==8?"!Blade로 제출한 검은 완성품이 됩니다.\n\n왼쪽 위의 반짝이는 ‘완성품’을 눌러 검을 확인하세요.":"선택한 검 아래의 반짝이는 ‘판매’ 버튼을 누르세요.\n\n판매가: "+ForgePricing.Quote(Evaluation.product).SalePrice+"G\n\n판매하면 검은 목록에서 사라지고 골드를 받습니다.";
    public bool AdvanceSale(out string message){
        message="먼저 검을 완성하세요.";
        if(Stage==8){Stage=9;message="완성한 검을 선택했습니다. 판매가를 확인하세요.";return true;}
        if(Stage!=9)return false;
        var practice=PracticeShop;int gold=0;
        if(practice.Count!=1||!practice.TrySell(practice.Items[0].Id,ref gold,out _))return false;
        PracticeGold=gold;Stage=10;message="판매 완료! 대장간으로 돌아가세요.";return true;
    }
    public ForgeMvp.Evaluation Evaluation { get; private set; }
    public static readonly string[] Titles={"재료 번호 알아보기","변수에 이름 붙이기","1() · 작업대에 올리기","2() · 가열하기","3() · 검날 성형","4() · 냉각하기","5() · 손잡이 만들기","완성 검 제출"};
    public static readonly string[] Instructions={
        "재료는 0~9, 총 10종입니다. 번호는 개수가 아니라 재료의 종류를 뜻합니다. 아래 목록에서 기본 검날에 사용할 철을 골라 보세요.",
        "변수는 값을 기억하는 이름입니다. = 오른쪽의 값을 왼쪽 이름에 넣습니다. Material이라는 변수에 철의 번호 1을 저장하세요.",
        "1(재료 번호)은 재료를 작업대에 올리고 작업물을 돌려줍니다.\n\n왜 함수 이름이 1()이냐고요? 그러게요. 전임자가 그렇게 하기로 결정했어요.\n\n함수명에 연연하지 마세요. 중요한 것은 당신이 시스템을 이해하고 도면을 작성하는 것입니다!\n\n숫자 언어 못 들어보셨나요?\n\nMaterial을 사용해 올린 작업물을 Blade라는 변수에 담으세요.",
        "2(작업물)은 현재 화로 온도로 가열합니다.\n\n2(Blade)로 기본 온도 900°C까지 달구세요. 850°C 이상이면 망치질로 성형할 수 있습니다.",
        "3(작업물)은 뜨거운 재료를 두드려 검날로 만듭니다.\n\n앞에서 가열한 Blade를 성형하세요.",
        "4(작업물, 냉각 재료)는 검날을 식힙니다.\n\n공기의 번호는 0입니다. 공기로 Blade를 식혀 열을 0으로 만드세요.",
        "5(작업물, 손잡이 재료)는 식힌 검날에 손잡이를 만듭니다.\n\n이번에는 철(1)로 손잡이를 만드세요. 검날과 손잡이에 철을 각각 1개씩 사용합니다.",
        "손잡이까지 달린 검이 준비됐습니다! ! 뒤에 작업물 변수 이름을 적어 완성품으로 제출하세요. !는 함수가 아닌 제출 기호입니다."};
    public string InstructionBody => Crafted?"":Stage==0?Instructions[0].Replace("철을 골라 보세요","<color=#D64C91>철</color>을 골라 보세요"):Instructions[Stage];
    public static readonly string[] Hints={"철의 번호는 1입니다.","Material = 1","Blade = 1(Material)","2(Blade)","3(Blade)","4(Blade, 0)","5(Blade, 1)","!Blade"};
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
        if(Stage<1||Crafted||string.IsNullOrWhiteSpace(input)||input.Length>512)return false;
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
            message=Crafted?"첫 검 완성! 변수와 1()~5()를 모두 사용했습니다.":"성공! 다음 단계로 진행합니다.";
            return true;
        }catch(BlueprintException error){message=error.Message;return false;}
    }
    public bool SubmitDocument(string source,out string message)
    {
        message="이전 코드에 이번 단계의 한 줄을 이어서 작성하세요.";
        if(Stage<1||Crafted||string.IsNullOrWhiteSpace(source)||source.Length>4096)return false;
        var lines=source.Replace("\r\n","\n").Split('\n');
        var replay=new ForgeTutorialLesson();replay.SelectMaterial(1,out _);
        int count=0;foreach(string line in lines)if(!string.IsNullOrWhiteSpace(line))count++;
        if(count!=Stage)return false;
        foreach(string line in lines){
            if(string.IsNullOrWhiteSpace(line))continue;
            int lineStage=replay.Stage;
            if(!replay.Submit(line,out message)){message=lineStage+"줄: "+message;return false;}
        }
        Stage=replay.Stage;Code=replay.Code;Evaluation=replay.Evaluation;
        message=Crafted?"첫 검 완성! 변수와 1()~5()를 모두 사용했습니다.":"성공! 다음 단계의 코드를 이어서 작성하세요.";
        return true;
    }
    public bool Previous()
    {
        if(Stage==0)return false;
        int target=Stage-1;
        var lines=Code.TrimEnd('\r','\n').Split('\n');
        string prefix="";
        for(int i=0;i<Math.Min(target-1,7);i++)prefix+=lines[i]+"\n";
        var previous=Restore(target,prefix);
        Stage=previous.Stage;Code=previous.Code;Evaluation=previous.Evaluation;PracticeGold=previous.PracticeGold;practiceShop=null;
        return true;
    }
    public static ForgeTutorialLesson Restore(int stage,string code)
    {
        var lesson=new ForgeTutorialLesson();
        if(stage<=0||stage>11)return lesson;
        lesson.SelectMaterial(1,out _);
        foreach(string line in (code??"").Split('\n'))if(!string.IsNullOrWhiteSpace(line)&&!lesson.Submit(line,out _))return new ForgeTutorialLesson();
        if(stage>=9&&lesson.Stage==8)lesson.AdvanceSale(out _);
        if(stage>=10&&lesson.Stage==9)lesson.AdvanceSale(out _);
        if(stage==11)lesson.ReturnToForge();
        return lesson.Stage==stage?lesson:new ForgeTutorialLesson();
    }
}
