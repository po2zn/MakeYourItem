# 실제 게임 연동

SampleScene에서 Play 후 기존 게임을 시작하면 새 3D 모델이 자동 적용된다. 씬에 수동으로 프리팹을 붙일 필요가 없다. 기존 장면 파일과 패키지/프로젝트 설정은 직접 변경하지 않았다.

## 동작

`ForgeMvp.Execute / RunExecution → ForgeCraftMotion.Play → ForgeWorkshopVisuals.Play` 경로로 성공한 제작 명령만 모델에 반영한다. 튜토리얼의 AnimateTutorial 및 추가 고용 대장장이의 RunSmith도 동일한 ForgeCraftMotion을 사용한다. 품질·재료·돈·상점 계산은 기존 게임 로직이 담당한다. 물리 Collider 접촉으로 중복 제작을 발생시키지 않는다.

- 대기: 팔의 작은 호흡 동작. 이동: 작업 위치로 이동하며 양다리 본 회전.
- 가열: 실제 화로 앞 작업 위치로 이동, 금속을 숯 위에 놓고 칼날 전용 발광을 열 0~9에 맞춰 변경.
- 성형: 모루 작업면에서 오른손 본을 움직여 두 번 망치질. 망치는 RightHandSocket에 부모 연결되어 움직인다.
- 물(3)/서리수(8) 냉각: 검을 아래로 돌려 양동이 내부에 넣고 발광을 끈다. 검 끝이 바닥 아래로 내려가지 않도록 위치 조정.
- 공기(0) 냉각: 모루에서 검을 들어 좌우로 흔들며 식힌다. 작업 구간에서 열 발광이 점차 사라지고 옅은 열기 입자가 발생한다.
- 작업물 ID마다 별도 프리팹/칼날 재질을 사용한다. 합쳐진 부품·파괴·회수 상태는 숨긴다. 성형 전에는 저장된 칼날 메시를 짧은 금속 조각으로 표시한다.
- 도금/별무늬/서리 색상, 손잡이 재료 색상, 룬/봉인/충전 장식도 데이터에 따라 반영한다.
- 중단·새 실행·컴포넌트 비활성 시 본 자세와 진행 상태를 초기화한다. 이전 실행은 새 실행 완료 횟수를 증가시키지 않는다.

## 에셋 연결

`Prefabs/Resources/BlacksmithWorkshopAssets.asset`가 여섯 최종 프리팹을 참조한다. Resources 의존성을 통해 플레이어 빌드에 포함되는 구조이며, 런타임 AssetDatabase 호출은 없다. 저장된 메시를 인스턴스화하며 매 프레임 메시를 만들지 않는다.
`Source/ForgeWorkshopAssets.cs`: 참조 데이터.
`Source/ForgeWorkshopVisuals.cs`: 새 모델 배치와 본/도구 동작, 재질 상태.
`Assets/Editor/Blacksmith/BlacksmithInstallIntegration.cs`: 참조 에셋 재구성 도구.

기존 모델 원본과 프리팹은 보존한다. 캐릭터 Animator/Animation 컴포넌트는 인스턴스에서 비활성화하며, 검증된 본 회전과 손 소켓 CCD 보정을 사용한다. 따라서 이전 FBX Idle 샘플링의 NaN 문제를 재현하는 경로를 실행하지 않는다. 원본 Idle/Walk/Run 클립 자체가 수정된 것은 아니다.

## 검증

- Unity C# 컴파일 성공.
- 기존 BlueprintFreeForgeChecks 12개 통과.
- 실제 SampleScene 부팅이 새 에셋을 선택하고 임시 작업대가 생성되지 않는 것 확인.
- 해석기 제작 결과 1/2/3/4/제출을 새 모델에서 실행: 유한한 본 좌표, 발광 가열/냉각, 망치 손 이동, 완료 횟수 통과.
- 별도 오프셋 공방, 두 작업물 ID, 공기 냉각, 중단 검증 통과.
- **실제 ForgeMvp 실행 경로**에서 철 10→9, 물 2→1, 제작 동작 4회, 게임 상점 완제품 1개 등록 통과. 이 검사는 저장 슬롯 없는 타이틀 상태에서만 시작하고 메모리상의 테스트 진행은 타이틀로 초기화한다. 테스트의 대성공 확률은 일반 성공으로 고정하여 환불/2개 생산이 재고 검증에 섞이지 않도록 했다.
- `IntegrationChecks.txt`, `ActualGameChecks.txt`에 결과 기록.
- `Integration_Process2.png`, `Integration_Process3.png`, `Integration_Process4.png`: 플레이 중 가열/타격/담금질 실제 렌더를 육안 확인. `Integration_ActualGameCompleted.png`: 실제 게임 카메라의 완성 상태(UI 제외). 1280×720 렌더 검사이며 UI 텍스트를 변경하지 않았으므로 UI 해상도별 가독성 재검사는 수행하지 않았다.

재실행: SampleScene을 저장 슬롯을 열지 않은 Play 상태로 두고 `unity command script eval 'return await BlacksmithIntegrationChecks.Run();' --project-path D:/Project/MakeYourItem --timeout 60 --format json`, 실제 게임 검사에는 `BlacksmithIntegrationChecks.MainGameRun()`을 사용한다. 에디터 테스트용 코드다.

## 범위와 제한

기존 파일 변경: ForgeCraftMotion.cs(모델 경로 선택), ForgeMvp.cs(임시 작업대 조건), ForgeExecution.cs/ForgeSmithWorkshop.cs(중단 전달). 원래 있던 ForgeMvp/ForgeGameUI의 미커밋 UI 수정은 보존했다. 에셋/코드/검증 파일은 Blacksmith 폴더 안에 추가했다.

작업 애니메이션은 간단한 본 기반 표현이다. 장애물 회피·보행 발 고정·손가락 파지는 없으며 불티 파티클은 아직 없다. 물리 타격 시뮬레이션이 아니라 게임 명령과 동기화한 시각화다. 고용 공방은 동일 코드/오프셋 구조로 연결했으며 실제 고용 UI를 통한 다수 병렬 작업과 모든 고급 제작 조합은 별도 전수 검사하지 않았다. 플레이어 빌드 실행은 미검증이다.

## 냉각 가시성 수정

튜토리얼은 `4(Blade, 0)` 공기 냉각을 사용한다. 기존 구현은 모루 위에서 색상만 바뀌어 별도 동작을 알아보기 어려웠다. 이제 공기 냉각은 검을 들어 두 차례 좌우로 식히며, 물(3)/서리수(8)는 수면 위 접근 → 담그기 → 유지 → 인출로 구분한다. 열 감소는 이동 중이 아닌 냉각 작업 구간에 적용한다. 냉각 중 망치를 감추고 종료/취소 시 복구한다.

뜨거운 검에만 공기 열기 10개/수증기 24개의 로우폴리 메시 입자를 방출한다. 공방마다 재사용하는 ParticleSystem 1개(최대 40개 입자), 기존 저장 메시와 URP 재질을 사용한다. 냉각 길이/속도 및 재료·품질 계산은 기존 step.duration과 게임 규칙을 유지한다. 가속 배율이 높으면 애니메이션도 짧아진다.

`BlacksmithIntegrationChecks.CoolingRun()`으로 0/3/8 모두 기본 게임 시간에 검사: 공기 중 이동 폭, 물속 진입 깊이, 양동이 바닥 비관통, 검 인출, 입자 발생, 발광 종료 및 완료 횟수 통과. `CoolingChecks.txt`에 결과 보존. `Integration_Cooling0_Active.png`, `Integration_Cooling3_Active.png`, `Integration_Cooling3_Withdraw.png`를 1280×720 실제 렌더로 육안 확인했다. MainGameRun 재검증에서 철 1개/물 1개 소비와 완제품 1개 등록도 통과했다. UI 텍스트 변경 없음. 변경 코드는 ForgeWorkshopVisuals.cs와 에디터 검증 코드이며 기존 장면/설정/원본 에셋은 직접 수정하지 않았다.
