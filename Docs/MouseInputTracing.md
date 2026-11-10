# 마우스 입력 진단 로그

## 테스트 시작
1. Unity 컴파일이 끝난 뒤 Play를 시작합니다. ForgeMvp의 Trace Mouse Input 기본값이 켜져 있어 자동 기록합니다.
2. Console에서 [MouseTrace] 기록 시작 메시지와 실제 파일 경로를 확인합니다. 메시지가 없으면 Game View에 포커스를 주고 F10을 누르면 기록을 시작하고 표시를 남깁니다.
3. 평소처럼 마우스로 테스트합니다. 문제가 발생하면 즉시 F10을 누르세요. USER_MARKER가 기록되고 바로 파일에 반영됩니다.
4. F9를 누르면 기록을 중지하고 파일을 닫습니다. 다시 누르면 새 세션으로 시작합니다. Play 종료·앱 종료에도 파일을 닫습니다.

## 로그 위치
- Editor: 프로젝트 Logs/InputTrace/mouse-날짜-세션-part001.jsonl
- 빌드: Application.persistentDataPath/InputTrace/ (Console 시작 메시지에 정확한 경로 표시)
- 한 줄이 JSON 레코드 하나입니다. 32MB를 넘으면 다음 part로 분할하며 이전 파일은 삭제하지 않습니다. 동일 세션의 모든 part 파일을 함께 확인하세요.
- 기록 중에도 파일 읽기가 가능하며 기본 0.5초마다, MouseUp·F10·포커스 변경·종료 시 즉시 flush합니다.

## 수집 범위
Unity가 해당 게임으로 전달한 마우스 입력만 수집합니다. 바탕화면이나 다른 앱의 전역 입력은 수집하지 않습니다.
- RAW_MOUSE: Input System의 원본 상태/부분 상태 이벤트, 장치·이벤트 ID, 입력 시각, 좌표, 이동량, 휠, 좌/우/가운데/뒤로/앞으로 버튼, 클릭 횟수. unchanged는 해당 부분 이벤트에 그 제어값이 없다는 뜻입니다.
- GUI_BEGIN / GUI_END: UI 처리 전후 이벤트 타입, rawType, 버튼, 클릭 횟수, modifier, hotControl, keyboardControl, 포커스, 창별 위치·스크롤·접힘 상태.
- HIT_TEST / TITLE_HIT_TEST / DRAG_START_GATE: 클릭한 창, 제목·실행 버튼 영역 판정, 툴바 경계, 캡처 때문에 이동 시작이 막혔는지.
- DRAG_ACQUIRE / DRAG_APPLY / END_WORKSPACE_DRAG: 이동 시작, 이동 적용, 종료.
- OVERLAY_MASKS / GUIDE_MASK / BEFORE_* / AFTER_*: 안내서·툴바·상점·디버그·오류창·텍스트 입력·스크롤바 전후에 이벤트가 Ignore/Used로 바뀌었는지.
- WINDOW_HANDLER / WINDOW_ACQUIRE / WINDOW_RELEASE: 재사용 창 처리기의 소유권 상태.
- CAPTURE_REMAINS_AFTER_UP: 이 화면의 MouseUp 처리 뒤 hotControl이 남은 조사 후보. 이것만으로 버그 확정은 아닙니다.
- APPLICATION_FOCUS / APPLICATION_PAUSE / MOUSE_DEVICE / HEARTBEAT: 포커스 이탈, 장치 변화, 1초 간격 최종 버튼 유지 상태.
- USER_MARKER: F10 재현 시점. 코드 본문이나 일반 키 입력 내용은 기록하지 않습니다.

GUI 좌표는 왼쪽 위가 원점입니다. Input System 원본 position은 왼쪽 아래가 원점입니다. frame/seq/seconds로 시점을 맞추세요.

## 권장 재현 순서
1. 코드창 제목을 잡아 이동 → 해제 → 곧바로 같은 창/다른 창 제목을 다시 잡기.
2. 코드 텍스트 선택 드래그 → 창 밖에서 해제 → 제목 이동.
3. 세로·가로 스크롤바를 잡고 안내서/상점/툴바/빈 배경 위에서 해제 → 제목 이동.
4. 배경 좌클릭·우클릭 드래그 → 즉시 코드창 이동.
5. 휠 확대·축소 → 드래그 → 클릭, 빠른 반복 클릭·더블클릭, 좌우 버튼을 번갈아 누르거나 함께 누르기.
6. 버튼을 누른 채 Game View 밖으로 이동해 해제 → 돌아와 제목 이동.
7. 테크 트리·대장장이·안내서·메뉴를 왕복한 후 같은 패턴 반복.
문제가 생긴 순간 F10, 정상 복구 뒤 다시 F10을 누르면 비교하기 쉽습니다.

## 검증 상태
전체 게임 스크립트 컴파일 통과. 로그 저장부는 별도 .NET I/O 하네스에서 JSONL 읽기, 한글/개행 이스케이프, 순서 번호, flush, 32MB 분할과 기존 파일 보존, 종료, 저장 실패 시 예외 격리를 통과했습니다.
이 검사는 Unity 마우스 입력 콜백의 실제 재현 검사가 아닙니다. 현재 드래그 버그의 원인은 수집할 사용자 재현 로그로 확인해야 합니다.
