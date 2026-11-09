using System.Collections.Generic;

public static class BlueprintHelp
{
    sealed class Frame { public string Opening; public BlueprintToken Callee; public int Argument=1; }
    public static string Describe(string source,BlueprintToken token)
    {
        if(token.Raw=="import")return "import 모듈명 [as 별칭] · 다른 코드창을 가져옵니다. 모듈명.함수명(...)으로 호출하세요.";
        if(token.Raw=="from")return "from 모듈명 import 함수명 [as 별칭] · 가져온 함수를 직접 호출합니다.";
        if(token.Raw=="as")return "가져온 모듈이나 함수에 이 코드창에서 사용할 별칭을 지정합니다.";
        var alias=BlueprintAliases.Find(token.Feature);if(alias!=null)return alias.Tooltip;
        if(token.Kind!=BlueprintTokenKind.Number)return "";
        var frames=new Stack<Frame>();BlueprintToken previous=null;
        foreach(var t in BlueprintLexer.Scan(source,true)){
            if(t.Start>=token.Start)break;
            if(t.Kind==BlueprintTokenKind.NewLine){frames.Clear();previous=null;continue;}
            if(t.Kind==BlueprintTokenKind.Comment)continue;
            if(t.Raw=="("||t.Raw=="["||t.Raw=="{")frames.Push(new Frame{Opening=t.Raw,Callee=t.Raw=="("?previous:null});
            else if(t.Raw==")"||t.Raw=="]"||t.Raw=="}"){if(frames.Count>0)frames.Pop();}
            else if(t.Raw==","&&frames.Count>0)frames.Peek().Argument++;
            previous=t;
        }
        int next=token.Start+token.Length;while(next<source.Length&&char.IsWhiteSpace(source[next])&&source[next]!='\n'&&source[next]!='\r')next++;
        if(next<source.Length&&source[next]=='('){switch(token.Raw){case "1":return "재료 올리기 · 등록 재료 0~9 · 10G · 공기 외 재료 1개";case "2":return "가열 · 작업물, 목표 열 단계1~9 · 성공당2G";case "3":return "타격 · 작업물 · 열6 이상일 때 성형, 낮으면 효과 없음 · 0G";case "4":return "냉각 · 작업물, 냉각 재료 번호 · 현재 공기(0) 지원 · 0G";}return "미지원 함수 "+token.Raw;}
        if(frames.Count>0){var f=frames.Peek();if(f.Opening=="("&&f.Callee!=null){
            if(f.Callee.Feature==BlueprintFeature.Random)return (f.Argument==1?"난수 범위의 최솟값":"난수 범위의 최댓값")+" · "+token.Raw+" · 양쪽 경계 포함";
            if(f.Callee.Raw=="2"&&f.Argument==2)return "목표 열 단계 "+token.Raw+" · 허용1~9";
            if(f.Callee.Raw=="1"&&f.Argument==1)return int.TryParse(token.Raw,out int material)&&ForgeMaterialGuide.Find(material)!=null?"재료 · "+ForgeMaterialGuide.Find(material).Name+" · 10G":"등록되지 않은 재료 번호";
            if(f.Callee.Raw=="4"&&f.Argument==2)return token.Raw=="0"?"냉각 재료 · 공기 · 비용0G":"냉각 재료 · MVP 미지원";
        }}return "정수 값 "+token.Raw;
    }
}
