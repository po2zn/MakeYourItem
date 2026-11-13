using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum BlueprintPatternKind { Literal, Capture, Wildcard, Value, Or, As, Sequence, Star, Mapping }
public sealed class BlueprintPattern
{
    public BlueprintPatternKind Kind;
    public BlueprintToken Token;
    public object Value;
    public string Name;
    public BlueprintExpr Expression;
    public readonly List<BlueprintPattern> Parts=new List<BlueprintPattern>();
    public readonly List<BlueprintExpr> Keys=new List<BlueprintExpr>();
    public readonly HashSet<string> Bindings=new HashSet<string>(StringComparer.Ordinal);
    public bool Irrefutable=>Kind==BlueprintPatternKind.Capture||Kind==BlueprintPatternKind.Wildcard||Kind==BlueprintPatternKind.As&&Parts[0].Irrefutable||Kind==BlueprintPatternKind.Or&&Parts.Any(p=>p.Irrefutable);
}
public sealed partial class BlueprintParser
{
    int patternDepth;
    BlueprintStatement MatchStatement()
    {
        var statement=new BlueprintStatement{Kind=BlueprintStatementKind.Match,Token=Take()};
        statement.Expr=Expression();
        if(Match(",")){
            var tuple=Node(BlueprintExprKind.Tuple,statement.Token,statement.Expr);
            if(Current.Raw!=":")do{tuple.Args.Add(Expression());}while(Match(",")&&Current.Raw!=":");
            statement.Expr=tuple;
        }
        Need(":");EndLine();Lines();
        if(Current.Kind!=BlueprintTokenKind.Indent)Fail("match/696 안에 case/6? 분기를 들여써 주세요.");Take();Lines();
        bool exhaustive=false;
        while(Current.Kind!=BlueprintTokenKind.Dedent&&Current.Kind!=BlueprintTokenKind.End){
            if(exhaustive)Fail("항상 맞는 분기(case _ / 9? 또는 이름)는 마지막에 작성하세요.");
            if(Current.Feature!=BlueprintFeature.Case&&Current.Feature!=BlueprintFeature.Default)Fail("match/696 바로 안에는 case/6? 또는 9?만 작성할 수 있습니다.");
            var branch=new BlueprintStatement{Kind=BlueprintStatementKind.Case,Token=Take()};
            branch.Pattern=branch.Token.Feature==BlueprintFeature.Default?NewPattern(BlueprintPatternKind.Wildcard,branch.Token):Patterns();
            if(Current.Feature==BlueprintFeature.If){Take();branch.Expr=Expression();}
            exhaustive=branch.Pattern.Irrefutable&&branch.Expr==null;
            branch.Body=Block();statement.Body.Add(branch);Lines();
        }
        if(statement.Body.Count==0)Fail("match/696에는 하나 이상의 분기가 필요합니다.");
        if(Current.Kind!=BlueprintTokenKind.Dedent)Fail("match 블록이 끝나지 않았습니다.");Take();return statement;
    }
    BlueprintPattern NewPattern(BlueprintPatternKind kind,BlueprintToken token)=>new BlueprintPattern{Kind=kind,Token=token};
    void AddPattern(BlueprintPattern parent,BlueprintPattern child)
    {
        foreach(string name in child.Bindings)if(!parent.Bindings.Add(name))Fail("패턴에서 같은 이름을 두 번 받을 수 없습니다: "+name);
        parent.Parts.Add(child);
    }
    BlueprintPattern Patterns()
    {
        var first=SequencePart();if(!Match(",")){if(first.Kind==BlueprintPatternKind.Star)Fail("*패턴은 목록·튜플 패턴 안에서만 사용할 수 있습니다.");return first;}
        var sequence=NewPattern(BlueprintPatternKind.Sequence,first.Token);AddPattern(sequence,first);
        while(Current.Raw!=":"&&Current.Feature!=BlueprintFeature.If){AddPattern(sequence,SequencePart());if(!Match(","))break;}
        CheckStars(sequence);return sequence;
    }
    BlueprintPattern Pattern()
    {
        if(++patternDepth>32)Fail("패턴 중첩은 32단계 이하여야 합니다.");
        try{
            var first=ClosedPattern();
            if(Match("|")){
                var alternatives=NewPattern(BlueprintPatternKind.Or,first.Token);alternatives.Parts.Add(first);alternatives.Bindings.UnionWith(first.Bindings);
                do{
                    if(alternatives.Parts[alternatives.Parts.Count-1].Irrefutable)Fail("항상 맞는 패턴은 |의 마지막에 작성하세요.");
                    var next=ClosedPattern();if(!first.Bindings.SetEquals(next.Bindings))Fail("|로 연결한 패턴은 같은 이름들을 받아야 합니다.");alternatives.Parts.Add(next);
                }while(Match("|"));first=alternatives;
            }
            if(Match("as")){
                var result=NewPattern(BlueprintPatternKind.As,first.Token);AddPattern(result,first);result.Name=Identifier();
                if(result.Name=="_"||!result.Bindings.Add(result.Name))Fail("as 뒤에는 중복되지 않는 변수명이 필요합니다.");first=result;
            }
            return first;
        }finally{patternDepth--;}
    }
    BlueprintPattern SequencePart()
    {
        if(Current.Raw!="*")return Pattern();
        var part=NewPattern(BlueprintPatternKind.Star,Take());part.Name=Identifier();if(part.Name!="_")part.Bindings.Add(part.Name);return part;
    }
    void CheckStars(BlueprintPattern pattern){if(pattern.Parts.Count(p=>p.Kind==BlueprintPatternKind.Star)>1)Fail("목록 패턴에는 *이름을 하나만 사용할 수 있습니다.");}
    BlueprintPattern ClosedPattern()
    {
        var token=Current;
        if(Match("[")||Match("(")){
            string close=token.Raw=="["?"]":")";
            var sequence=NewPattern(BlueprintPatternKind.Sequence,token);
            if(Match(close))return sequence;
            var first=SequencePart();
            if(token.Raw=="("&&Current.Raw==")"&&first.Kind!=BlueprintPatternKind.Star){Take();return first;}
            if(token.Raw=="("&&first.Kind==BlueprintPatternKind.Star&&Current.Raw!=",")Fail("괄호 안의 *패턴 뒤에는 쉼표가 필요합니다.");
            AddPattern(sequence,first);
            while(Match(",")&&Current.Raw!=close)AddPattern(sequence,SequencePart());
            Need(close);CheckStars(sequence);return sequence;
        }
        if(Match("{")){
            var mapping=NewPattern(BlueprintPatternKind.Mapping,token);var literalKeys=new HashSet<object>();
            if(Match("}"))return mapping;
            do{
                if(Match("*")){
                    Need("*");mapping.Name=Identifier();if(mapping.Name=="_"||!mapping.Bindings.Add(mapping.Name))Fail("** 뒤에는 중복되지 않는 변수명이 필요합니다.");
                    Match(",");Need("}");return mapping;
                }
                var key=ClosedPattern();if(key.Kind!=BlueprintPatternKind.Literal&&key.Kind!=BlueprintPatternKind.Value)Fail("딕셔너리 패턴 키에는 상수나 모듈의 값을 사용하세요.");
                if(key.Kind==BlueprintPatternKind.Literal){if(key.Value==null||!literalKeys.Add(key.Value))Fail("중복되거나 지원하지 않는 딕셔너리 패턴 키입니다.");}
                mapping.Keys.Add(key.Expression??new BlueprintExpr{Kind=BlueprintExprKind.Literal,Token=key.Token,Value=key.Value});Need(":");AddPattern(mapping,Pattern());
            }while(Match(",")&&Current.Raw!="}");Need("}");return mapping;
        }
        if(token.Kind==BlueprintTokenKind.Number||token.Kind==BlueprintTokenKind.String||token.Raw=="-"||token.Raw=="True"||token.Raw=="False"||token.Raw=="None"){
            object value;
            if(Match("-")){var number=Take();if(number.Kind!=BlueprintTokenKind.Number||!long.TryParse(number.Raw,out var n)||n>2147483648L)Fail("음수 패턴에는 정수가 필요합니다.");value=(int)-long.Parse(number.Raw);}
            else {Take();if(token.Kind==BlueprintTokenKind.String)value=Decode(token);else if(token.Kind==BlueprintTokenKind.Number){if(!int.TryParse(token.Raw,out var n))Fail("정수 범위 초과");value=int.Parse(token.Raw);}else value=token.Raw=="None"?null:(object)(token.Raw=="True");}
            var literal=NewPattern(BlueprintPatternKind.Literal,token);literal.Value=value;return literal;
        }
        if(token.Kind==BlueprintTokenKind.Name&&token.Feature==BlueprintFeature.None){
            string name=Identifier();
            if(Current.Raw=="."){
                var expression=new BlueprintExpr{Kind=BlueprintExprKind.Name,Token=token,Name=name};
                while(Match(".")){var member=Current;string field=Identifier();expression=Node(BlueprintExprKind.Attribute,member,expression);expression.Name=field;}
                var result=NewPattern(BlueprintPatternKind.Value,token);result.Expression=expression;return result;
            }
            var capture=NewPattern(name=="_"?BlueprintPatternKind.Wildcard:BlueprintPatternKind.Capture,token);capture.Name=name;if(name!="_")capture.Bindings.Add(name);return capture;
        }
        Fail("지원하는 패턴: 값, _, 이름, |, as, 목록·튜플, 딕셔너리. 클래스 패턴은 지원하지 않습니다.");return null;
    }
}
public sealed partial class BlueprintRuntime
{
    bool PatternEqual(object actual,object expected,bool singleton=false)
    {
        if(singleton||expected==null)return Equals(actual,expected);
        if(actual is bool b&&expected is int n)return (b?1:0)==n;
        if(actual is int i&&expected is bool flag)return i==(flag?1:0);
        return Equal(actual,expected);
    }
    bool GuardTruth(object value)=>value==null?false:value is bool b?b:value is int n?n!=0:value is string text?text.Length>0:value is ICollection<int> numbers?numbers.Count>0:value is System.Collections.ICollection items?items.Count>0:true;
    async Task<bool> MatchPattern(BlueprintPattern pattern,object subject,Dictionary<string,object> env,Dictionary<string,object> bindings)
    {
        Tick(pattern.Token);
        switch(pattern.Kind){
            case BlueprintPatternKind.Wildcard:return true;
            case BlueprintPatternKind.Capture:bindings[pattern.Name]=subject;return true;
            case BlueprintPatternKind.Literal:return PatternEqual(subject,pattern.Value,pattern.Value is bool);
            case BlueprintPatternKind.Value:return PatternEqual(subject,await Eval(pattern.Expression,env));
            case BlueprintPatternKind.As:
                if(!await MatchPattern(pattern.Parts[0],subject,env,bindings))return false;bindings[pattern.Name]=subject;return true;
            case BlueprintPatternKind.Or:
                foreach(var part in pattern.Parts){var attempt=new Dictionary<string,object>();if(await MatchPattern(part,subject,env,attempt)){foreach(var item in attempt)bindings[item.Key]=item.Value;return true;}}return false;
            case BlueprintPatternKind.Sequence:
                var sequence=subject as IList<int>;if(sequence==null)return false;
                int star=pattern.Parts.FindIndex(p=>p.Kind==BlueprintPatternKind.Star),required=pattern.Parts.Count-(star>=0?1:0);
                if(star<0?sequence.Count!=required:sequence.Count<required)return false;
                for(int index=0;index<pattern.Parts.Count;index++){
                    var part=pattern.Parts[index];
                    if(index==star){if(part.Name!="_"){var rest=new List<int>();for(int j=index;j<sequence.Count-(pattern.Parts.Count-index-1);j++)rest.Add(sequence[j]);Allocate(rest.Count);bindings[part.Name]=rest;}}
                    else {int position=star>=0&&index>star?sequence.Count-(pattern.Parts.Count-index):index;if(!await MatchPattern(part,sequence[position],env,bindings))return false;}
                }return true;
            case BlueprintPatternKind.Mapping:
                var map=subject as Dictionary<int,int>;if(map==null)return false;var used=new HashSet<int>();
                for(int i=0;i<pattern.Parts.Count;i++){
                    object key=await Eval(pattern.Keys[i],env);if(!(key is int)&&!(key is bool))return false;int number=key is bool boolean?(boolean?1:0):(int)key;
                    if(!used.Add(number))Fail("딕셔너리 패턴 키가 중복됩니다.");if(!map.TryGetValue(number,out int value)||!await MatchPattern(pattern.Parts[i],value,env,bindings))return false;
                }
                if(pattern.Name!=null){var rest=new Dictionary<int,int>();foreach(var pair in map)if(!used.Contains(pair.Key))rest.Add(pair.Key,pair.Value);Allocate(rest.Count);bindings[pattern.Name]=rest;}return true;
        }
        return false;
    }
    async Task<Signal> MatchBlock(BlueprintStatement statement,Dictionary<string,object> env,bool inLoop)
    {
        var subject=await Eval(statement.Expr,env);
        foreach(var branch in statement.Body){
            await BeforeStatement(branch.Token,env);Tick(branch.Token);
            if(branch.Error!=null)throw new BlueprintException(branch.Error.Line,branch.Error.Message){ModuleName=currentModule.IsMain?null:currentModule.Name};
            var bindings=new Dictionary<string,object>();bool matches=await MatchPattern(branch.Pattern,subject,env,bindings);if(Watching)Trace(branch.Token.Line,"분기 패턴 일치 → "+BlueprintObservation.Format(matches));if(!matches)continue;
            foreach(var binding in bindings)env[binding.Key]=Copy(binding.Value);
            if(branch.Expr!=null&&!GuardTruth(await Eval(branch.Expr,env)))continue;
            return await Block(branch.Body,env,inLoop);
        }
        return new Signal(Flow.Next);
    }
}
