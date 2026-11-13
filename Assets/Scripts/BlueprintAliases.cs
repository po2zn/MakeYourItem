using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum BlueprintFeature { None, While, For, Break, Continue, If, Else, Def, Return, Heat, Abs, Len, Min, Max, Random, Set, Elif, Print, IsDay, NightTime, Attach, Polish, Plate, Fuel, Charge, Refine, Inspect, Lucky, LuckyCode, Speed, AutoSell, Curse, Protect, Recover, Wrap, Unwrap, SealCount, Rune, RuneList, RuneFamily, RuneGrade, Slots, Append, Pop, SetAdd, Magic, Strength, Wait, Resonate, SmithDemand, SmithGift, SmithFavor, SmithPosition, SmithRun, SmithCount, SmithSelf, SmithBusy, GiantRune, Match, Case, Default, StarlightCount, StarlightRead, StarlightSwap, StarlightRemove, CoolingCount, CoolingStart, CoolingEnd, CoolingCost, CoolingRun, BoneLength, BoneCount, BonePosition, BoneCut, BoneIsCut, BoneCost, GiantRuneTime }
public enum BlueprintSyntaxKind { Block, Branch, Control, Builtin }
public sealed class BlueprintAlias
{
    public BlueprintFeature Id { get; }
    public string English { get; }
    public string Symbol { get; }
    public BlueprintSyntaxKind Syntax { get; }
    public string Description { get; }
    public string Signature { get; }
    public int Arity { get; }
    public string Tooltip => (Symbol.Length==0?English:Symbol + " ↔ " + English) + "\n" + Description + (Signature.Length == 0 ? "" : "\n" + Signature);
    public BlueprintAlias(BlueprintFeature id, string english, string symbol, BlueprintSyntaxKind syntax, string description, string signature = "", int arity = -1)
    { Id = id; English = english; Symbol = symbol; Syntax = syntax; Description = description; Signature = signature; Arity = arity; }
}
// One authoritative mapping for parser dispatch, editor colors, hover and future semantic translation.
public static class BlueprintAliases
{
    static readonly BlueprintAlias[] definitions = {
        new BlueprintAlias(BlueprintFeature.BoneLength,"bone_length","90",BlueprintSyntaxKind.Builtin,"최초 전체 길이","90(Bone) → 정수",1),
        new BlueprintAlias(BlueprintFeature.BoneCount,"bone_count","91",BlueprintSyntaxKind.Builtin,"최초 필수 마디 수","91(Bone) → 정수",1),
        new BlueprintAlias(BlueprintFeature.BonePosition,"bone_position","92",BlueprintSyntaxKind.Builtin,"0 기반 마디 번호의 고정 위치","92(Bone, Index) → 정수",2),
        new BlueprintAlias(BlueprintFeature.BoneCut,"bone_cut","93",BlueprintSyntaxKind.Builtin,"현재 조각 절단 · 완료 후 비용 반영","93(Bone, Position) → None",2),
        new BlueprintAlias(BlueprintFeature.BoneIsCut,"bone_is_cut","94",BlueprintSyntaxKind.Builtin,"필수 위치의 절단 완료 여부","94(Bone, Position) → 불리언",2),
        new BlueprintAlias(BlueprintFeature.BoneCost,"bone_cost","95",BlueprintSyntaxKind.Builtin,"완료된 절단의 누적 비용","95(Bone) → 정수",1),
        new BlueprintAlias(BlueprintFeature.CoolingCount,"cooling_count","80",BlueprintSyntaxKind.Builtin,"냉각 도면의 지점 수를 읽습니다.","80(Blade) → 정수",1),
        new BlueprintAlias(BlueprintFeature.CoolingStart,"cooling_start","81",BlueprintSyntaxKind.Builtin,"입구 지점 번호를 읽습니다.","81(Blade) → 정수",1),
        new BlueprintAlias(BlueprintFeature.CoolingEnd,"cooling_end","82",BlueprintSyntaxKind.Builtin,"출구 지점 번호를 읽습니다.","82(Blade) → 정수",1),
        new BlueprintAlias(BlueprintFeature.CoolingCost,"cooling_cost","83",BlueprintSyntaxKind.Builtin,"통로의 서리수 비용을 읽습니다. 연결 없으면 -1입니다.","83(Blade, A, B) → 정수 · 지점은 0부터",3),
        new BlueprintAlias(BlueprintFeature.CoolingRun,"cooling_run","84",BlueprintSyntaxKind.Builtin,"유효 경로의 총비용만큼 서리수를 소비해 냉각합니다.","84(Blade, 8, Path) → None · 경로 최대 128개 지점",3),
        new BlueprintAlias(BlueprintFeature.StarlightCount,"starlight_count","70",BlueprintSyntaxKind.Builtin,"현재 별빛 개수를 읽습니다.","70(StarIron) → 정수",1),
        new BlueprintAlias(BlueprintFeature.StarlightRead,"starlight_read","71",BlueprintSyntaxKind.Builtin,"0부터 시작하는 위치의 값을 읽고 공개합니다.","71(StarIron, Index) → 정수 0~9",2),
        new BlueprintAlias(BlueprintFeature.StarlightSwap,"starlight_swap","72",BlueprintSyntaxKind.Builtin,"두 위치의 별빛과 공개 상태를 교환합니다.","72(StarIron, A, B) → None",3),
        new BlueprintAlias(BlueprintFeature.StarlightRemove,"starlight_remove","73",BlueprintSyntaxKind.Builtin,"지정 위치를 제거합니다. 뒤의 값이 앞으로 이동합니다.","73(StarIron, Index) → None",2),
        new BlueprintAlias(BlueprintFeature.IsDay,"is_day","12",BlueprintSyntaxKind.Builtin,"낮(06:00 이상 18:00 미만)이면 정수 1, 밤이면 0을 반환합니다.","12() → 정수 0 또는 1 · 호출 비용 0G",0),
        new BlueprintAlias(BlueprintFeature.NightTime,"night_time","24",BlueprintSyntaxKind.Builtin,"24()는 밤이면 정수 1, 낮이면 0을 반환합니다. 24(60)은 자정부터 지난 정수 분을 반환합니다.","24() → 0 또는 1 · 24(60) → 0~1439분 · 호출 비용 0G",-1),
        new BlueprintAlias(BlueprintFeature.Print,"print","",BlueprintSyntaxKind.Builtin,"여러 값을 공백으로 구분해 디버그 출력 창에 출력합니다. print()는 빈 줄을 출력합니다.","print(값, ...) → None · 비용0G",-1),
        new BlueprintAlias(BlueprintFeature.Match,"match","696",BlueprintSyntaxKind.Block,"값에 맞는 첫 번째 분기만 실행합니다. Python match/case 문법입니다.","696 값: · match 값:"),
        new BlueprintAlias(BlueprintFeature.Case,"case","6?",BlueprintSyntaxKind.Branch,"값이나 패턴이 맞으면 실행합니다. |로 여러 후보를 연결할 수 있습니다.","6? 패턴: · case 패턴:"),
        new BlueprintAlias(BlueprintFeature.Default,"case _","9?",BlueprintSyntaxKind.Branch,"앞의 분기가 모두 맞지 않을 때 실행합니다. 마지막에 작성하세요.","9?: · case _:"),
        new BlueprintAlias(BlueprintFeature.While,"while","888",BlueprintSyntaxKind.Block,"조건이 참인 동안 반복합니다. 0은 거짓, 0이 아닌 정수는 참입니다.","888(1): · 계속 반복 · !!로 중단 · 실행 안전 제한 적용"),
        new BlueprintAlias(BlueprintFeature.For,"for","123",BlueprintSyntaxKind.Block,"정수 N은 0부터 N-1까지 반복하며, 0과 음수는 반복하지 않습니다. 리스트 또는 튜플은 원소를 순서대로 순회합니다.","123(이름 in 대상):"),
        new BlueprintAlias(BlueprintFeature.Break,"break","!!",BlueprintSyntaxKind.Control,"가장 가까운 반복문을 종료합니다."),
        new BlueprintAlias(BlueprintFeature.Continue,"continue","~~",BlueprintSyntaxKind.Control,"현재 반복의 남은 부분을 건너뛰고 다음 반복으로 이동합니다."),
        new BlueprintAlias(BlueprintFeature.If,"if","1?",BlueprintSyntaxKind.Block,"조건이 참이면 블록을 실행합니다.","1?(조건): · 조건은 bool"),
        new BlueprintAlias(BlueprintFeature.Elif,"elif","01?",BlueprintSyntaxKind.Branch,"앞선 분기가 선택되지 않았고 조건이 참이면 블록을 실행합니다.","01?(조건): · 조건은 bool"),
        new BlueprintAlias(BlueprintFeature.Else,"else","0?",BlueprintSyntaxKind.Branch,"연결된 앞 조건 분기에서 실행할 블록이 선택되지 않았을 때 실행합니다.","0?:"),
        new BlueprintAlias(BlueprintFeature.Def,"def","333",BlueprintSyntaxKind.Block,"재사용할 사용자 함수를 정의합니다.","333 이름(매개변수): · 최대8개 · 재귀 지원 · 호출 깊이32"),
        new BlueprintAlias(BlueprintFeature.Return,"return","<<",BlueprintSyntaxKind.Control,"현재 함수를 종료하고 호출한 위치로 결과를 돌려줍니다.","<< 값 · 생략하면 None"),
        new BlueprintAlias(BlueprintFeature.Heat,"heat","100",BlueprintSyntaxKind.Builtin,"100(작업물)은 현재 온도, 100(검날, 1)은 첫 가열 뒤 검마다 고정된 적정 온도(850~1100°C)를 읽습니다.","100(작업물[, 1]) → 정수 온도 · 비용0G",-1),
        new BlueprintAlias(BlueprintFeature.Abs,"abs","101",BlueprintSyntaxKind.Builtin,"정수의 절댓값을 구합니다.","정수1개 → 정수 · Int32 범위 초과 시 오류",1),
        new BlueprintAlias(BlueprintFeature.Len,"len","1111",BlueprintSyntaxKind.Builtin,"대상에 포함된 원소 개수를 구합니다.","리스트/튜플/집합/딕셔너리1개 → 정수 · 빈 대상은0",1),
        new BlueprintAlias(BlueprintFeature.Min,"min","000",BlueprintSyntaxKind.Builtin,"최솟값을 구합니다.","비어 있지 않은 정수 리스트/튜플1개 → 정수",1),
        new BlueprintAlias(BlueprintFeature.Max,"max","999",BlueprintSyntaxKind.Builtin,"최댓값을 구합니다.","비어 있지 않은 정수 리스트/튜플1개 → 정수",1),
        new BlueprintAlias(BlueprintFeature.Random,"random","66",BlueprintSyntaxKind.Builtin,"지정한 범위에서 무작위 정수를 구합니다.","정수 최솟값, 최댓값 → 정수 · 양쪽 경계 포함 · 최솟값 ≤ 최댓값",2),
        new BlueprintAlias(BlueprintFeature.Set,"set","1010",BlueprintSyntaxKind.Builtin,"중복 원소를 허용하지 않는 빈 정수 집합을 만듭니다.","인자 없음 → 빈 집합 · 원소는 {0, 3}으로 지정",0)
    };
    public static readonly ReadOnlyCollection<BlueprintAlias> All = Array.AsReadOnly(ForgeExpansion.Combine(definitions));
    public static BlueprintAlias Find(BlueprintFeature id) { foreach(var a in All) if(a.Id==id) return a; return null; }
    public static BlueprintAlias Find(string spelling) { if(spelling=="778")return Find(BlueprintFeature.LuckyCode);foreach(var a in All) if(a.English==spelling || a.Symbol.Length>0 && a.Symbol==spelling) return a; return null; }
}
