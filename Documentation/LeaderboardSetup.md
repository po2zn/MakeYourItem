# 타이틀 리더보드 연결

게임은 판매 때 최고 기록만 PlayerPrefs에 저장합니다. 타이틀의 리더보드를 열거나 그 안의 새로고침 버튼을 누를 때만 HTTPS 인증·동기화·조회합니다. 닫으면 진행 중 요청을 취소합니다. 최고값은 저장 슬롯 공통이며 새 게임으로 지워지지 않습니다. 기존 저장에는 과거 최대 판매 거래가 없어 소급 복원하지 않습니다.

## Unity Cloud 설정
1. 현재 프로젝트 MakeYourItem (Project ID cb1fbf8a-2e62-412a-9f01-fb1b4426efb7)을 선택합니다.
2. Authentication의 익명 인증과 Leaderboards 서비스를 설정합니다.
3. production 환경에 highest-sword-sale 리더보드를 만듭니다.
4. Sort order: Descending, Update type: Keep Best. 버킷과 정기 초기화는 사용하지 않습니다. 플레이어 자신의 점수 제출과 순위 읽기를 허용해야 합니다.
5. Assets/Resources/ForgeLeaderboardSettings.asset에서 ID를 확인하고 Enabled For Players를 켭니다. 환경을 바꾼 경우 Environment에는 환경 이름을 넣습니다. 관리자 키나 비밀키는 게임에 넣지 않습니다.
6. WebGL 빌드를 itch.io 비공개 테스트 페이지에 올려 HTTPS에서 두 브라우저로 등록·조회·재접속을 확인합니다. 개발자 콘솔의 CORS 오류와 네트워크 차단 여부도 확인합니다.

## 확인할 흐름
- 첫 로딩과 일반 제작·판매에는 리더보드 API 요청 없음.
- 타이틀에서 이름 입력 후 동기화: 실제 판매 완료된 검 한 자루의 최고 대금 등록.
- 낮거나 같은 금액 판매: 점수 POST 없음. 더 높은 금액 판매 후 타이틀에서 열면 갱신.
- 인터넷 차단 시 게임 계속 가능. 이전 순위와 내 기록 보존. 창을 다시 열거나 새로고침하면 재시도.
- 창 닫기: 요청 취소, 이후 백그라운드 갱신 없음.
- 브라우저 저장 삭제 시 익명 사용자 연결이 사라짐. 등록된 이름은 고정. 이름은 점수 메타데이터로 저장되며 동일 이름은 허용.

## 범위
게임잼용 클라이언트 제출 방식입니다. 로컬 데이터 조작을 서버에서 증명하거나 방지하지 않습니다. production 환경에서 익명 인증, 테스트 점수 등록, 상위 점수 갱신, 낮은 점수 제출 시 최고값 유지, 이름 메타데이터 및 순위 조회를 REST로 확인했습니다. Unity 실행 화면과 itch.io WebGL 브라우저의 실제 연동 검증은 별도로 필요합니다. 제작 서버를 별도로 운영하거나 PC를 켜둘 필요는 없습니다.

공식 문서: https://docs.unity.com/en-us/services-web-apis/client-auth
https://docs.unity.com/en-us/oas-leaderboards/1.0.0

## 리더보드 UI 편집
- `Assets/Resources/ForgeLeaderboardUI.prefab`을 Prefab Mode로 열면 실행하지 않아도 전체 UI를 편집할 수 있습니다.
- Panel/Layout/Scroll/Viewport/Content 아래에서 제목, 안내문, 이름 입력, 상태, Rank01~Rank20을 선택합니다. 순위 행은 기록이 없으면 비활성 상태이며 프리팹 모드에서 활성화하여 편집할 수 있습니다.
- Text에서 폰트·크기·색상, Vertical Layout Group에서 간격·여백, 버튼 Layout Element에서 최소 높이를 조정합니다. 문단 높이는 Text의 preferred height로 계산하므로 고정 높이를 강제로 지정하지 마세요.
- 루트 ForgeLeaderboardView에서 동적 안내 문구와 UI 참조를 편집합니다. 런타임은 저장된 프리팹을 표시하고 내용만 갱신하며 UI 요소를 조립하지 않습니다.
- 640×480, 800×600, 1280×720, 1920×1080, 3840×2160에서 긴 한글 이름·최대 금액·안내문 줄바꿈의 높이/겹침 검사 및 오프스크린 렌더링을 확인했습니다. 실제 마우스 입력 및 itch.io 브라우저 검증은 별도입니다.
