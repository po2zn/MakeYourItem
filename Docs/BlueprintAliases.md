> **공정 비용 변경 (2026-11-09):** 재료 올리기와 가열은 골드를 소비하지 않습니다. 재료 올리기는 공기를 제외한 해당 재료 1개만 소비하며, 가열은 추가 재료 소비도 없습니다. 전체 실행·한 줄 실행·import·재귀에 동일하게 적용됩니다. 상점 구매 가격은 유지합니다. 아래 과거 기록의 10G/2G 공정 비용보다 이 규칙을 우선합니다.

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
- 함수는 최상위에서 정의하며 최대8개 매개변수를 받는다. 호출별 지역 변수와 정의 모듈의 전역 변수 조회, 함수값, 직접·상호 재귀를 지원한다. 실제 동시 호출 깊이32와 전체 실행10000명령 한도를 실행기에서 검사한다. 실행하지 않은 재귀 정의나 긴 호출 체인은 컴파일 오류가 아니다.
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

정적 구문/이름/인자 수/알 수 있는 타입 검사는 실제 제작 전에 전체 도면에 적용한다. 함수 매개변수처럼 동적 타입은 실행 시 다시 검사한다. 컴파일 오류는 기존 코드 창의 빨간 밑줄과 독립 오류 창으로 전달하고 상점을 건드리지 않는다. 런타임 실패는 기존 제작 결과에 남는다. 변수의 존재와 현재 타입은 실제로 읽는 시점에 검사한다. 조건/반복 블록은 별도 변수 범위를 만들지 않으며 실행된 대입은 현재 실행 범위에 남는다.

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
| Assets/Scripts/BlueprintValidator.cs | Validate/Statements/Expr, 정적 오류·공정 효과 검사 |
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

## 코드 창 들여쓰기 편집 지원

공백4칸 기준으로 Enter/Tab/Shift+Tab/Backspace를 처리한다. Enter는 현재 줄의 선행 공백을 이어받고, def/333, if/1?, elif/01?, else/0?, while/888, for/123의 블록 헤더가 콜론으로 끝나면4칸을 더한다. 뒤쪽 주석은 무시하며 주석/문자열/딕셔너리의 콜론은 블록으로 취급하지 않는다. 미완성 def(): 입력도 편집 편의상 자동 들여쓰기하지만 실제 실행에는 함수 이름이 필요하다.

Tab은 다음4칸 위치까지 공백을 넣는다. 여러 줄 선택 상태에서는 전체 선택 줄을4칸 들여쓰고, Shift+Tab은 선택 줄 또는 현재 줄의 들여쓰기를 한 단계 줄인다. 다음 줄 시작점까지만 선택한 경우 그 다음 줄은 제외한다. 선행 공백에서 Backspace는 이전4칸 위치까지 지우고, 일반 글자/선택 영역 삭제는 기본 TextArea에 맡긴다. 문자열 내용이나 붙여넣은 소스 전체를 자동 재정렬하지 않는다. 기존 잘못된 들여쓰기의 컴파일 오류 검사는 유지한다.

ForgeMvp.DrawCodeWindow → BlueprintIndentation.Enter/Tab/Backspace → ApplyCodeEdit로 소스와 커서/선택을 함께 갱신한다. 기존 창별 Undo에 한 번의 편집으로 기록되며, 새 줄의 크기를 계산한 뒤 커서 자동 스크롤을 적용한다. Input System의 IME 조합 중 및 조합 변경 프레임에는 사용자 정의 들여쓰기 키 처리를 건너뛰어 기본 입력 처리에 맡긴다.

검증: ENABLE_INPUT_SYSTEM/UNITY_EDITOR를 포함한 C# 컴파일 및 standalone Mono 자동 검사53개(들여쓰기10+기존43) 통과. 한국어 원문/CRLF/정방향·역방향 선택/중첩 블록/주석·문자열 보호/새 문법으로 제작 실행을 검사했다. Play Mode 키보드·IME·Undo·스크롤의 직접 조작 검증은 미수행이다.

직접 확인: SampleScene Play → 코드 창에333(warm, p): 입력 후 Enter → 공백4칸과 커서 확인 → 내부1?(True): 뒤 Enter → 공백8칸 확인 → Shift+Tab/Backspace로 한 단계 복귀 → 여러 줄 선택 후 Tab/Shift+Tab → Ctrl+Z → 긴 도면 마지막 줄 Enter 시 스크롤 확인. 한국어 조합 확정 Enter가 추가 줄/들여쓰기를 만들지 않는지도 확인한다. Tools > MakeYourItem > Validate Code Indentation에서 편집 모델10개 검사를 실행할 수 있다.

## Enter 중복 입력 및 커서 스크롤 원인 경로 수정

코드 분석: 이전 구현은 Return/KeypadEnter KeyDown에서 직접 개행을 추가했다. Windows IMGUI는 키 코드 이벤트와 문자 이벤트를 분리해 전달할 수 있으며, 별도 newline 문자 이벤트는 GUI.TextArea가 다시 삽입한다. 키 이벤트에서 Use()해도 뒤의 별도 문자 이벤트를 소비하지 않는다. 스크롤 예약 역시 키 코드에 의존해 기본 문자 입력으로 바뀐 최종 커서를 놓칠 수 있었다. Unity 공식 GUI.cs의 HandleTextFieldEventForDesktop(KeyDown/character 삽입)과 BeginScrollView(캐시 scrollPosition 적용) 경로를 참고했다: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/IMGUI/GUI.cs . 실제 사용자 키 이벤트 로그/Play Mode 재현은 수집하지 못했으므로 코드 경로 분석과 실측을 구분한다.

변경: 줄바꿈 삽입은 TextArea에만 맡긴다. BlueprintIndentation.IndentNativeNewline은 이전 소스/선택을 기준으로 정확히 개행 한 개가 삽입된 경우만 감지하여 들여쓰기를 덧붙인다. Return 키 코드만 받은 경우 소스를 직접 변경하지 않는다. 모든 실제 텍스트 변경에서 최종 커서 추적을 예약한다. BlueprintViewport는 마지막 빈 줄을 포함한 원본 줄 수로 스크롤 높이/세로 커서 위치를 계산하며, 원래 글자 측정 높이보다 작아지지 않도록 한다. 예약은 실제 Repaint에서 요청한 스크롤이 반영될 때까지 유지한다.

검증: ENABLE_INPUT_SYSTEM/UNITY_EDITOR 포함 C# 컴파일, standalone Mono 총56개(입력·스크롤 모델13+언어24+진단5+경제14) 통과. 개행 없는 키 이벤트→문자 개행→들여쓰기 모델,50회 연속 Enter의 정확한 줄 수/커서 표시 범위,CRLF,IME 글자 확정이 개행으로 오인되지 않는 경우를 검사했다. OS 이벤트 주입 또는 실제 Play Mode에서의 원인/해결 실측을 대체하지 않는다.

직접 재검증: SampleScene 코드 창에 def warm(p): 입력 후 Enter 한 번에 다음 줄4칸으로 이동하는지, 빈 줄에서 Enter50회 시 정확히50줄 추가되면서 커서가 보이는지, 키패드 Enter/한글 조합 확정/Shift 선택 후 Enter/Undo/위아래 이동/마우스 휠 후 Enter를 확인한다.

## print 문자열 출력과 독립 디버그 창

사용 예:

~~~text
print("안녕하세요")
Message = "제작을 시작합니다"
print(Message)
~~~

문자열 인자1개를 받아 디버그 창에 출력하고 None을 반환한다. 작은따옴표/큰따옴표, 빈 문자열, 문자열 변수와 기존 이스케이프를 지원한다. print() / print(1) / print("a", "b")는 인자 오류다. 숫자 별칭은 지정하지 않았으며 기존16개 숫자 대응에 영문 내장 함수print가 추가되어 공통 데이터 항목은17개다. print는 다른 함수와 같은 연노랑색이며 문자열 인자/반환값 설명 툴팁을 제공한다.

출력 전용 코드는 ! 없이 실행할 수 있다. 제작 공정을 포함한 코드의 ! 요구는 유지한다. 컴파일 실패 시 출력은 실행되지 않는다. 실행 중 실패하면 이전 성공 출력만 남는다. 조건·반복·사용자 함수 안에서 실제로 실행된 호출만 출력하고, 실제 제작은 공정 Step 사이의 출력 Step을 순서대로 재생한다. 출력은 비용/재료 소비/공정 애니메이션 지연/상품 생성/난수 시드 소비를 추가하지 않는다. print 전용 실행은 상점 결과를 변경하지 않는다. 시험은 같은 실행기의 출력만 미리 보여주며 실제 재고/자금을 변경하지 않는다.

첫 출력 시 별도 ‘디버그 출력’ 창이 열리며 도면 이름과 시험/실제 실행 구분 뒤에 메시지가 누적된다. 상단 ‘디버그 창’ 버튼으로 다시 열거나 숨길 수 있다. 제목 드래그 이동, 좌우·아래 크기 조절(최소320×160), 내부 스크롤, 새 출력 따라가기, ‘지우기’와 닫기를 제공한다. 이 창은 화면 좌표의 독립 읽기 전용 출력 창이며 작업 공간 확대와 분리된다. 출력 창 위 마우스 입력은 뒤 코드 창/상점/작업 공간에 전달하지 않는다. richText=false이므로 출력 문자열의 <color> 등은 태그로 실행되지 않는다.

안전한 출력 범위: 실행당 최대256회/65536문자이며 초과 호출 줄에서 실행 오류가 난다. 창은 최근256항목/65536문자 이내만 보관하고 오래된 내용을 지운다. 출력 기록은 세션 중에만 유지한다.

주요 파일: BlueprintAliases(Print 등록/서명), BlueprintValidator(문자열 검사/출력 전용 판정), BlueprintRuntime.Call(출력 Step), ForgeMvp.Execute/Craft/DrawDebugWindow/DrawDebugContents(시험·실제 출력과 창), ForgeDebugOutput(누적 버퍼), BlueprintPrintChecks(9개 새 검사).

검증: Unity 참조 및 ENABLE_INPUT_SYSTEM/UNITY_EDITOR C# 컴파일 통과. standalone Mono에서 print9+기존56=65개 검사 통과. 한글/빈 문자열/이스케이프, 함수·조건·반복, 공정 사이 출력 순서, 기존12G 제작비/28G 판매가, 시험 결과 일치, 잘못된 인자, 실패 이전 출력 유지, ! 규칙, 색상/툴팁, 출력 한도/버퍼 지우기를 확인했다. Temp/BlueprintPrintChecks.txt에 결과를 기록했다. Unity 창 캡처가 두 번 window capture timed out으로 실패하여 Play Mode 버튼 클릭/출력 창 레이아웃·이동·리사이즈는 직접 확인하지 못했다.

직접 확인: SampleScene Play → 코드 창에 위 예제 입력 → ▷/▶ 실행 → 디버그 창의 출력 순서와 시험/실제 표기 확인. 창을 이동/좌우·아래 리사이즈하고 지우기/닫기/상단 버튼 재열기를 확인한다. 기본 제작 도면의 각 공정 사이에 print("가열 후") 등을 넣어 실제 제작 애니메이션과 출력 순서를 확인한다. Tools > MakeYourItem > Validate Print Output으로 출력 검사를 재실행할 수 있다.

## 조건·반복문 변수의 실행 시점 검사

변수 읽기를 ‘모든 정적 경로에서 미리 대입되어야 함’ 검사에서 실제 실행 시점 검사로 변경했다. BlueprintValidator.ExprCore는 변수 참조의 타입을 Unknown으로 두어 이전 회차/선택 분기에서 생성되거나 타입이 바뀐 변수를 차단하지 않는다. 문법, 식별자 형식, 함수 존재/인자 수, 리터럴에서 확정할 수 있는 타입, 잘못된 break/return 및 실행 한도 검사는 유지한다.

BlueprintRuntime.Block은 원래부터 if/elif/else와 for/while 본문에 동일한 환경 딕셔너리를 전달한다. 실행된 대입은 다음 회차와 블록 밖에서 보이며, 존재하지 않는 이름을 Eval에서 실제로 읽으면 그 원본 줄에서 ‘변수 '이름'에 아직 값이 없습니다. 이 줄 실행 전에 값을 대입하세요.’ 실행 오류가 발생한다. 실행하지 않은 분기나 단락 평가로 건너뛴 피연산자의 미정의 변수는 오류가 아니다. 반복0회면 새 변수가 생기지 않는다. 함수의 기존 지역 환경과 호출 간 분리는 유지하며 Python 전체 글로벌 이름 탐색을 추가한 것은 아니다.

이전3(Missing) 사례는 이제 컴파일 오류가 아니라 해당 호출 줄의 실행 오류다. 이미 성공한 철 올리기/가열의12G와 철1개는 기존 제작 실패 규칙대로 남고 완성품은 등록되지 않는다. 시험 실행은 여전히 실제 자원을 변경하지 않는다. 컴파일 오류 무소비 회귀 검사는 명백한 잘못된 인자 수3(Blade,1)로 유지하고, 미정의 변수의 실행 중 실패는 별도 회귀 검사로 확인한다.

확인: 사용자가 제공한 a=1 → while(True) → 첫 회차res 생성 → 후속 회차 가열/타격/냉각 → break → !res 도면을 그대로 실행해 철1개/제작비12G/판매가28G/순이익16G 확인. 영문 대응, 분기 밖 사용, for 변수의 블록 밖 사용, 실행하지 않은 분기, 반복0회, 선사용 후대입, None에서 작업물로 재대입, 함수 지역 변수와 호출 격리, 실패 전 비용 유지도 확인했다. Unity 참조 C# 컴파일 및 standalone Mono 변수12+기존65=77개 검사 통과. 실제 Play Mode UI 조작 검증은 미수행이다. Tools > MakeYourItem > Validate Runtime Variables로 새12개 검사를 실행할 수 있다. 결과: Temp/BlueprintVariableChecks.txt.
