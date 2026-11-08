# 숫자·기호 예약어 구현과 검증

작성: 2026-11-08 · 현재 작업 프로젝트: D:/Project/MakeYourItem · Unity 6000.4.5f1

## 적용 범위

SampleScene의 기존 Forge MVP와 IMGUI 코드 창/시험/제작/상점을 재사용했다. 요청의 2026Unity-GameJam 이름과 현재 폴더명은 다르며, 실제 수정 대상은 위 경로다. 별도 담당 에이전트 없이 단일 작업으로 진행했다.

기존 기획 BlueprintLanguagePlan.md의 정수·블록·함수·컬렉션 정의를 실행 가능하게 구현했다. 아래 문서가 이전 문서의 ‘확장 미구현’ 상태를 대체한다. GameDesign.pdf의 과거 숫자 절댓값 후보10이나 조건 표기 후보보다 이번 확정 대응표를 우선한다. 기존 공정1~4, ! 완성품, PythonIdentifier 식별자 정규화, 비용 원장과 가격 산식을 유지했다.

## 공통 대응 데이터

| 내부 식별자 | 영문 | 숫자·기호 | 종류 / 실제 형식 |
|---|---|---|---|
| While | while | 888 | 블록: 888(condition): |
| For | for | 123 | 블록: 123(item in values): |
| Break | break | !! | 문장: !! |
| Continue | continue | ~~ | 문장: ~~ |
| If | if | 1? | 블록: 1?(condition): |
| Elif | elif | 01? | 분기: 01?(condition): |
| Else | else | 0? | 분기: 0?: |
| Def | def | 333 | 정의: 333 name(args): 또는 333(name, args): |
| Return | return | << | 문장: << value 또는 << |
| Heat | heat | 100 | 작업물1개 → 정수0~9 |
| Abs | abs | 101 | Int32 정수1개 → 절댓값, 범위 초과 오류 |
| Len | len | 1111 | 리스트/튜플/집합/딕셔너리1개 → 원소 수 |
| Min | min | 000 | 비어 있지 않은 정수 리스트/튜플1개 → 최솟값 |
| Max | max | 999 | 비어 있지 않은 정수 리스트/튜플1개 → 최댓값 |
| Random | random | 66 | 정수 lower, upper → 양쪽 경계 포함 정수 |
| Set | set | 1010 | 인자0개 → 빈 정수 집합 |

BlueprintAliases.All이 식별자, 양쪽 표기, 문법 종류, 설명, 서명, 인자 수를 관리한다. BlueprintLexer가 원문 Raw와 UTF-16 Start/Length, Line, Feature를 기록한다. 파서·구문 강조·툴팁이 이 Feature를 사용한다. 실행기는 영문 문자열 치환 없이 Feature로 분기하므로 검사·비용·반환값·오류가 동일하다. 기존 해금 시스템은 없으며 별칭만의 해금 조건도 없다.

향후 변환은 Feature가 있는 토큰만 변경해야 한다. Target = 100, 문자열, 주석은 Feature=None이다. 영어 for x in values:를 숫자로 변환할 때는 블록 헤더 괄호를 추가해야 하므로 단순 전체 문자열 Replace를 사용하지 않는다. 이번에는 변환 UI를 만들지 않았다.

## 문법과 실행 결정

- 블록 끝에 콜론, 들여쓰기는 공백4칸. 빈 블록, 탭, 잘못된 깊이는 컴파일 오류다. 기존 블록 없는 도면에 한해 이전 Trim 동작과 호환되도록 선행 공백을 무시한다.
- 영문 if condition:, while condition:, for x in values:, def name(args):와 괄호 조건을 지원한다. 숫자 반복문은 888(condition):, 123(x in values):를 사용한다. else/0?에는 조건 괄호가 없다.
- 비교 == != < <= > >= in, 논리 and/or/not, True/False/None, Int32 + - * // %를 지원한다. 조건은 bool이다. //와 %는 Python 음수 내림 규칙이다. 연쇄 비교 대신 and를 사용한다.
- !!와 ~~는 가장 가까운 반복문에만 작용한다. 함수 안의 break/continue로 호출자 반복문을 제어할 수 없다. <<는 중첩된 루프를 벗어나 현재 함수만 종료한다. 반환 생략/본문 끝은 None이다.
- 함수는 최상위 정의, 최대8개 매개변수, 지역 변수만 사용한다. 글로벌 변수 캡처, 재귀(직접/간접), 함수값은 지원하지 않는다. 호출 깊이32 초과는 정적 검사와 실행기에서 제한한다.
- 공정을 포함한 함수는 독립 호출 또는 변수 대입의 전체 우변에서만 호출한다. 조건식/공정 인자/중첩 수식에 숨겨서 실행할 수 없다. 숫자1 반환은 변수에 저장하며2~4의 반환값은 없다. !는 최상위 또는 그 if 안에서만 허용하며 도면 전체를 종료한다. 각 실행에서 최대1개 완성품이다.
- 리스트 [9,7,5], 튜플 (6,7), 집합 {0,1}, 딕셔너리 {1:3}은 평면 정수 컬렉션이다. 빈 집합은 set()/1010(), {}는 딕셔너리다. 집합 중복은 제거하고 딕셔너리 중복 키는 뒤 값이 덮어쓴다.
- 리스트/딕셔너리의 name[index] = integer만 변경할 수 있다. 리스트/튜플 인덱스는0부터 시작하고 음수는 오류다. 없는 딕셔너리 키를 읽으면 해당 접근 줄에서 오류, 대입은 키를 추가한다. for는 리스트/튜플의 시작 시점 복사본을 순회한다. 대입·인자·반환 시 컬렉션은 복사하고 작업물은 같은 참조를 공유한다.
- min/max는 빈 목록 오류, len은 빈 컬렉션0, random은 반드시 두 인자와 lower≤upper, set은 인자0개다. 문자열 len, range, append/remove, set(iterable)은 지원하지 않는다.
- 요청의 문자열 문맥을 위해 따옴표 문자열을 스칼라 값으로 추가했다. 주석/예약어는 문자열 내부에서 해석하지 않는다. 역슬래시/따옴표 및 n/r/t 이스케이프, 문자열 동등 비교만 허용하며 보간·여러 줄 문자열은 없다.
- 확정이 필요한 미결정 문법은 남기지 않았다. continue와 문자열 스칼라는 이번 요청을 충족하기 위해 위처럼 판단했고, 인자 없는 random()은 기존 정수 중심 기획에 따라 제외했다. 물/손잡이 공정과 보너스는 여전히 후속 범위다.

## 그대로 붙여 넣을 수 있는 완성 도면

열 후보9,7,5에서 첫 적합값7을 고른 뒤 한 번만 가열한다. 적합값이 없으면 공정을 전혀 실행하지 않는다.

~~~text
333 choose(values, limit):
    selected = None
    123(candidate in values):
        1?(candidate <= limit):
            selected = candidate
            !!
    << selected

Target = choose([9, 7, 5], 7)
1?(Target != None):
    Blade = 1(1)
    2(Blade, Target)
    3(Blade)
    4(Blade, 0)
    !Blade
0?:
    skipped = True
~~~

333을 def, 123(candidate in values)를 for candidate in values, 1?를 if, !!를 break, <<를 return, 0?를 else로 바꿔도 같은 결과다. 모든 블록의 콜론/들여쓰기는 유지한다.

추가 문법 예시(위 완성 도면 앞에 삽입 가능):

~~~text
Stock = {1: 3}
Stock[1] = Stock[1] - 1
Allowed = {0, 0}
CanCool = 0 in Allowed
Bounds = (6, 7)
Lowest = 000(Bounds)
Highest = 999(Bounds)
Distance = 101(Lowest - Highest)
Count = 1111([9, 7, 5])
Roll = 66(1, 7)
Empty = 1010()
i = 0
888(i < 3):
    i = i + 1
    1?(i == 2):
        ~~
~~~

## 실행 한도·난수·경제

도면 UTF-8 최대64KiB, 블록/표현식 중첩32, AST 연산 깊이64, 실행 명령10000, 호출32, 컬렉션1024개, 실행 중 생성·복사 누적16384개를 제한한다. 명령 수는 문장/식/반복 회차/난수 재추출을 센다. 제한을 넘으려던 토큰의 원본 줄에서 멈추며 이전 성공 공정의 비용/철은 남고 실패 호출은 지불하지 않는다. ! 미성공 시 상품은 등록하지 않는다. 동기 해석기가10000명령으로 제한되며 기획의 프레임당256 분할 실행은 아직 적용하지 않았다.

정적 구문/이름/인자 수/알 수 있는 타입 검사는 실제 제작 전에 전체 도면에 적용한다. 함수 매개변수처럼 동적 타입은 실행 시 다시 검사한다. 컴파일 오류는 기존 코드 창의 빨간 밑줄과 독립 오류 창으로 전달하고 상점을 건드리지 않는다. 런타임 실패는 기존 제작 결과에 남는다. 모든 분기에서 선언이 보장된 변수만 뒤에서 읽도록 보수적으로 검사한다.

난수 v1은 명시적인32비트 LCG(state×1664525+1013904223, uint wrap), 편향 방지를 위한 범위 밖 표본 거부, 양쪽 경계 포함 정수 매핑이다. UnityEngine.Random 전역 상태는 사용하지 않는다. 코드 창별 초기 시드1, 시험/편집/컴파일 실패/공정 없는 건너뛰기는 시드를 소비하지 않는다. 실제 첫 성공 공정에서 한 번만 다음 시드로 넘어간다. 동일 창·시작 상태에서 시험 후 실제 실행은 같은 결과다. 창 삭제/새 창/새 세션은 초기 시드로 돌아가며 영구 저장 시스템은 아직 없다.

계산·분기 자체는0G, 별도 애니메이션 대기 없음. 공정/완성 판정만 기존 Craft의0.65초 단계 흐름으로 전달한다. 영어·숫자 양쪽의 실행 단계와 시간이 같다. 비용은 성공한 호출만 집계하고 시험은 지역 원장을 사용한다. 실제 Craft는 성공한 단계만 실제 원장에 적용한다.

- 기본 도면: 철1개, 제작비12G, 판매가28G, 순이익16G.
- 같은 완성 상태에 재가열1회 추가: 제작비14G, 판매가28G, 순이익14G.
- 계산/조건/함수/반복을 추가해도 실제 공정 호출이 같으면 견적이 같다.
- 완성품의 소비 재료/제작비/특성 스냅샷, 상점 중복 판매 방지는 기존 ForgeEconomy를 공유한다.

## 코드 창과 툴팁

Forge MVP Inspector의 Reserved Color 기본값은 #C586C0이다. 제어문과 정의 키워드의 영문·숫자·기호 토큰은 보라색이다. 내장 함수와 사용자 함수 이름(정의 위치 포함)은 기존 공정 함수1~4와 같은 연노랑색이다. 주석 초록, 변수 흰색, 재료 분홍, 공정 함수1~4 연노랑, 그 외 Python 키워드도 보라색이며 !의 주황은 유지한다. a = 1의1은 기존 분홍색 규칙을 유지한다.

이 프로젝트의 입력기는 TMP가 아닌 IMGUI TextArea이다. 실제 source는 태그 없는 원문이며 richText=false인 별도 글자 레이어로 색상을 그린다. 따라서 <<, <, >를 표시용 태그로 치환하거나 소스에 삽입하지 않는다. tolerant lexer는 미완성 코드에서도 문자열/주석과 토큰 범위를 유지한다. TextEditor 커서·선택·Undo·창별 스크롤 처리 경로를 유지했다.

마우스0.5초 정지 시 공통 대응 데이터의 ‘숫자 ↔ 영문 / 설명 / 서명’ 툴팁을 표시한다. 2(Blade,7)의7은 목표 열 단계, random(1,7)/66(1,7)의7은 양쪽 경계를 포함한 난수 최댓값이다. 문자열/주석 내부에는 예약어 툴팁을 만들지 않는다.

## 수정 파일과 연결 함수

| 파일 | 주요 연결점 |
|---|---|
| Assets/Scripts/BlueprintAliases.cs | All/Find, 공통16개 매핑과 Tooltip |
| Assets/Scripts/BlueprintLexer.cs | Scan, 원문/줄/범위/Feature와 최장 복합 토큰 우선 |
| Assets/Scripts/BlueprintParser.cs | Parse/Statement/Expression/Block, AST 생성 |
| Assets/Scripts/BlueprintValidator.cs | Validate/Statements/Expr/VisitCalls, 정적 오류·재귀/효과 검사 |
| Assets/Scripts/BlueprintRuntime.cs | Evaluate/Block/Eval/Call/Process, 공통 실행·제한·난수·원장 |
| Assets/Scripts/BlueprintHelp.cs | Describe, 토큰과 호출 인자 문맥 툴팁 |
| Assets/Scripts/ForgeSyntax.cs | Scan, 동일 lexer 기반 토큰 색상 분류 |
| Assets/Scripts/ForgeMvp.cs | Evaluate→새 실행기, Execute/Craft 시드, DrawSyntax/DrawCodeWindow 강조·툴팁, Reserved Color |
| Assets/Editor/BlueprintLanguageChecks.cs | Run/ValidateInEditor, 언어 검사22개 |

새 스크립트의 .meta를 포함한다. 씬/패키지 설정을 변경하지 않았다. ForgeEconomyChecks와 ForgeDiagnosticChecks는 기존 회귀 검사로 재사용했다.

## 확인 결과와 직접 확인 절차

실제로 수행한 검증: Unity 설치의 참조 어셈블리를 사용한 전체 런타임 및 UNITY_EDITOR C# 컴파일 통과. standalone Mono에서 언어22 + 진단5 + 경제14 = **41개 검사 통과**. 결과는 Temp/BlueprintLanguageChecks.txt(임시 산출물)에 기록했다.

검사에는16개 별칭 결과/오류 일치, 중첩 break/continue/return, 후보7 선택·단일 가열/후보 없음, 함수 내 공정 원장/참조, 재귀·호출 깊이,10000명령 중단, 없는 키/잘못된 인자, 문자열/주석/일반 변수 보호, 토큰별 보라색 범위, 실제 UTF-8 임시 파일 저장·읽기 후000/복합기호·CRLF·한글 보존, 결정적 난수와 경계, 기존 견적·판매 회귀가 포함된다.

**미검증:** 실제 Play Mode에서의 버튼 클릭, Craft 코루틴 애니메이션, 보라색/툴팁 시각 배치, 입력/삭제/붙여넣기/선택/스크롤/한글 IME. Unity 창은 실행 중이지만 Computer Use 화면 캡처가 두 번 window capture timed out으로 실패했다. Unity 내부 자동 검사 프로브도 실행 결과가 나오지 않아 제거했다. 따라서 Unity 에디터 내 실행 통과로 보고하지 않는다. 저장/불러오기 UI 자체가 기존에 없으므로 파일 왕복 검사와 실제 UI 저장 검증을 구분한다.

1. Assets/Scenes/SampleScene.unity를 열고 재컴파일 완료 후 Console 오류가 없는지 확인한다.
2. Tools > MakeYourItem > Validate Blueprint Language로 동일41개 검사를 Unity 내부에서 실행한다.
3. Play 후 코드 창에 위 완성 도면을 붙여 넣는다. ▷ 시험에서12/28/16과 철1개, ▶ 제작 후 같은 견적을 확인하고 판매한다.
4. 영문 대응으로 바꿔 같은 결과인지, [9,8]로 바꾸면 비용/상품 추가 없이 건너뛰는지 확인한다.
5. Inspector의 Forge MVP > Reserved Color를 바꿔 양쪽 표기 모두 같은 색인지 확인한다. 000/<<와 미완성 1?(를 입력하고 주석/문자열은 바뀌지 않는지 본다.
6. 100/heat, !!/break 등에 마우스를 올려 대응 툴팁,2(Blade,7)와66(1,7)의7에 다른 설명을 확인한다.
7. 긴 도면을 넣고 방향키/Shift 선택/Undo/복사/붙여넣기, 줄바꿈·공백4칸, 한글 조합,40/100/200% 확대와 창 리사이즈를 확인한다. 복사 원문에 색상 태그가 없어야 한다.
8. 잘못된 들여쓰기나 루프 밖!!를 넣어 원본 오류 줄의 빨간 밑줄/독립 설명 창과 상점 유지 동작을 확인한다.

## elif / 01? 추가

if/1? 뒤 같은 들여쓰기에서 elif/01?를 여러 개 연결할 수 있다. 위에서부터 검사해 첫 참인 분기 하나만 실행하며, 선택 이후의 조건은 평가하지 않는다. 마지막 else/0?는 선택 사항이다. 단독 elif, else 뒤 elif, 콜론 누락은 해당 원본 줄의 컴파일 오류다. 01?는3글자 복합 토큰으로 먼저 인식하고 전체를 보라색으로 표시한다. 기존 내부 식별자 값은 유지하며 Elif를 끝에 추가했다. 조건 분기 중첩(elif 연결 포함)은32단계로 제한한다.

~~~text
1?(Target > 7):
    Target = 7
01?(Target < 6):
    Target = 6
0?:
    Target = Target
~~~

영문 elif와 숫자01?의 혼합, 다중/중첩 분기, 후속 조건 미평가, 강조·툴팁·미완성 토큰, 잘못된 연결의 원본 오류 줄을 검사했다. Unity 참조 C# 컴파일 및 standalone Mono 총41개 검사 통과. 이번 추가 작업의 Play Mode 시각 검증은 수행하지 않았다.

## 함수 색상 분리 및 def 표기 유지
함수 호출(heat/100, abs/101 등)은 연노랑색, if/1?, elif/01?, def/333 등 제어·정의 키워드는 보라색이다. 333(함수명, 매개변수): 형태를 유지하며 333: 함수명(...)은 지원하지 않는다. 사용자 함수 이름은 정의와 호출 위치 모두 연노랑색이다. C# 컴파일과 standalone Mono 자동 검사43개 통과, Play Mode 시각 검증은 미수행.
