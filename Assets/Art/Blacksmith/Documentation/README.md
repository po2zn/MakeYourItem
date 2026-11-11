# Blacksmith asset set — Unity 6000.4.5f1 / URP 17.4

최종 사용 세트: **Set_20261111_171955**. 2026-11-11 제작.

**현재 게임 캐릭터:** 사용자 제공 MeshCharacter 모델로 교체한 `Prefabs/Blacksmith_Copperbeard.prefab` 사용. 기존 소품 세트와 작업 애니메이션은 유지한다. [교체 내역](CopperbeardReplacement.md).

**후속 게임 연동 완료:** 실제 SampleScene의 제작 경로에서 자동으로 이 세트를 로드한다. 이동·가열·망치질·담금질은 새 절차적 본 애니메이션으로 실행되며, 원본 FBX 클립은 사용하지 않는다. 상세 구현/실행 검증은 [Integration.md](Integration.md)를 참조한다. 아래 애니메이션 제한은 원본 FBX 클립에 관한 기록이다.

## 사용

`Assets/Art/Blacksmith/Prefabs/Set_20261111_171955/`의 Blacksmith, Forge, QuenchBucket, Anvil, Sword, Hammer 프리팹을 장면에 드래그한다.
별도 검증 장면: `Documentation/Set_20261111_171955/BlacksmithPreview.unity`.
최종 이미지: 같은 폴더의 `Preview_800x600.png`, `Preview_1280x720.png`, `Preview_1920x1080.png`, `Preview_3840x2160.png`.
`Preview.png`는 캐릭터를 뒤에서 본 초기 이미지이고, `Set_20261111_171823`은 애니메이션 샘플링 실패를 남긴 초기 세트다. 최종 세트만 사용한다.

Unity 1 unit = 1 m. 지면 배치 모델 루트는 바닥 기준이다. 대장장이 약 1.75 m, 화로 약 1.30×1.30×1.07 m, 양동이 직경 약 0.67 m/높이 0.57 m, 모루 약 0.81×0.82×0.76 m, 검 길이 1.095 m, 망치 약 0.41 m. 검과 망치는 바닥 대신 손잡이 중심 원점이다.

## 출처 / 실측

|모델|제작|삼각형|재질|리깅 / 형식|
|---|---|---:|---:|---|
|Blacksmith|Polyfork Tradesman 개조|3651|15|31개 본, 스킨 렌더러 15개; FBX + 보존 GLB|
|Forge|직접 절차 제작|592|6|없음; Unity Mesh .asset / Prefab|
|QuenchBucket|직접 절차 제작|336|3|없음; Unity Mesh .asset / Prefab|
|Anvil|Polyfork Anvil 재질 개조|686|4|없음; FBX|
|Sword|직접 절차 제작|50|3|없음; Unity Mesh .asset / Prefab|
|Hammer|직접 절차 제작|24|2|없음; Unity Mesh .asset / Prefab|

총 5339 삼각형. 재질 수는 모델별 고유 재질 수이며 공유 재질을 중복 집계한다. 소품의 개별 메시가 많아 드로콜은 삼각형 수에 비해 높다. 런타임 메시 생성 코드는 없다. 직접 제작물은 Blender 미사용이므로 .blend/FBX가 없으며 Unity 네이티브 메시가 원본이다.

카탈로그 원본: Tradesman 3919 triangles / GLB 재질 1개, Anvil 686 triangles / GLB 재질 1개. FBX는 색 영역별 재질로 변환된다. 대장장이는 모자 제거, 갈색 앞치마 재질, 본에 부착된 별도 가죽 장갑 추가. 모루는 철 색 #353b43, 목재 #805636을 프로젝트용 URP 재질에 적용했다. 원본 FBX/GLB 바이트는 보존했다.

## 라이선스

Anvil by Polyfork (https://polyfork.dev/asset/anvil-1286cb), CC BY 4.0

Tradesman by Polyfork (https://polyfork.dev/asset/tradesman-1707b9), CC BY 4.0

위 두 모델은 CC BY 4.0 옵션을 선택했다. https://creativecommons.org/licenses/by/4.0/ . 배포 시 이 저작자 표시와 라이선스 링크 및 위 변경 내역을 함께 유지한다. 제공자 조건: https://polyfork.dev/licensing . API 원문은 `anvil-1286cb-metadata.txt`, `tradesman-metadata.txt`에 보존했다.
직접 제작한 메시·장갑·제작 스크립트는 이 프로젝트를 위해 작성했으며 외부 모델이나 텍스처를 복제하지 않았다. 별도 외부 에셋 라이선스 의존성이 없다. Polyfork 기반 캐릭터와 모루의 CC BY 표시는 별도로 유지한다.

## 검색과 선택

실제 search_assets 검색어: blacksmith, forge, bucket, anvil, sword, hammer. get_asset로 후보 4종 상세 확인, find_matching으로 동일 Medieval Village 키트의 Tradesman 발견. 컨택트시트와 캐릭터 렌더를 육안 확인했다. 결제/설치는 하지 않았다.
Blacksmith Forge는 5 m 규모 건물이라 독립 개방형 화로 대신 쓰지 않았다. Wooden Teoke Bucket(526 tris, GLB 재질 1, 무리깅)은 작은 일본식 대나무 테두리/횡단 손잡이로, 큰 개방형 담금질 용기와 맞지 않아 사용하지 않았다. Arming Sword ab8738(662 tris, GLB 재질 1, 무리깅)은 외형은 적합하지만 분리 부품이 제공되지 않아 교체 가능한 네 부품 직검을 직접 제작했다. Hammer fa3e51은 못 빼는 장도리여서 별도 단조 망치를 제작했다.
get_variant에 모루 iron #353b43, wood #805636, hornLength 1.1 요청: confidence high였으나 반환문은 GLB에 기하 변경만 들어가고 색 변경은 들어가지 않는다고 명시했다. 따라서 해당 변형을 적용 완료로 취급하지 않았고 기본 비율 FBX를 채택했다. 색 변경은 저장된 Unity 재질에서 수행했으며 최종 렌더로 확인했다. 모루 비율 변경은 적용하지 않았다.

## 구성과 연결

- Forge: StoneBody / Charcoal / Fire 분리. Fire 아래 Embers, Flame, FireLight. HeatTarget 기준으로 금속 삽입. 몸체 BoxCollider, 앞/위 작업 공간 열림. 불은 정적 메시와 발광/포인트라이트이며 파티클이 아니다.
- QuenchBucket: HollowStaves는 실제 내벽·두께·입구가 있는 메시. WaterSurface 별도 메시, 12개 벽 BoxCollider와 바닥, QuenchVolume 트리거. 물 표면은 불투명 단색이며 굴절·물리 시뮬레이션 없음.
- Anvil: anvil과 TimberBase 분리. WorkFaceCollider, BodyCollider, 받침 충돌체 및 StrikeTarget. 돌출 뿔 끝에는 정밀 충돌체가 없어 후속 보완 가능.
- Sword: Blade / Guard / Grip / Pommel. Blade_HeatIndependent 재질의 _EmissionColor만 조절해 가열한다. _EMISSION 활성, 기본 검정. 색/부품 교체 가능.
- Hammer: Handle / Head 분리. 검과 망치의 GripAnchor는 루트 위치와 동일하고 +Y가 도구 길이 방향이다.
- Blacksmith: LeftHandSocket / RightHandSocket 제공. 도구 루트를 소켓 자식으로 넣고 localPosition을 0으로 설정한 뒤 작업 자세에 맞춰 localRotation을 조정한다. GripAnchor가 소켓에 일치하도록 배치한다. 양손 IK/파지 애니메이션은 미구현.

## 애니메이션 제한

원본 GLB 메타데이터와 실제 FBX 임포트에서 Idle, Walk, Run 클립 및 31개 본 확인. 클립은 Characters/Set_20261111_171955/*.anim에도 복사했다. 그러나 FBX Idle을 SampleAnimation으로 직접 적용하면 Unity 6000.4.5f1에서 본 좌표 NaN과 캐릭터 소실이 발생했다. 따라서 클립 정상 재생을 보증하지 않으며 AnimatorController는 연결하지 않았다. 미리보기는 본을 직접 회전한 유효한 정적 자세, 프리팹은 원본 기본 자세다. 리깅은 존재하나 걷기/달리기/단조 애니메이션 통합 완료 상태가 아니다.

## 제작 및 검증

Blender는 PATH와 확인한 표준 설치 위치에서 발견되지 않았다. 새 프로그램/패키지를 설치하지 않고 실행 중인 Unity Pipeline으로 제작했다.
재생성: Unity 메뉴 `Tools > Blacksmith > Build New Asset Set`. 매 실행 시 새 타임스탬프 폴더를 만들어 기존 파일을 덮어쓰지 않는다.
CLI: `unity command script run --file Assets/Editor/Blacksmith/BlacksmithAssetBuilder.cs --entry BlacksmithAssetBuilder.Build --project-path D:/Project/MakeYourItem --format json`
Play Mode를 먼저 종료한다. 소스 다운로드 FBX가 있어야 한다. 기존 장면을 저장/변경하지 않고 별도 additive 장면을 생성·저장·닫는다.

제작 스크립트 컴파일과 실행 성공. 최종 6개 프리팹의 메시/본 참조와 URP Lit 재질 검증 통과 (`Validation.txt`). 네 해상도 실제 렌더 생성; 800×600 및 1920×1080 이미지를 육안 확인했다. UI 텍스트는 추가/수정하지 않았으므로 숫자/영문 안내, 최소 UI 창 크기 검증은 해당하지 않는다. 충돌체는 구조 검사이며 실제 검 담금질/타격 플레이 테스트는 미실시.

## 변경 범위

추가 파일은 Art/Blacksmith와 Editor/Blacksmith 및 Unity가 생성한 부모 폴더 .meta. 원래 변경 중이던 ForgeGameUI.cs / ForgeMvp.cs는 편집하지 않았다. 기존 장면, Packages는 변경하지 않았다.
에디터 실행 후 ProjectSettings/ProjectSettings.asset 및 URPProjectSettings.asset가 Git에서 modified로 표시됐으나 git diff 및 ignore-space-at-eol 검사에 실질 내용 차이가 없고 LF/CRLF 경고만 있었다. 승인 범위 밖 파일을 직접 수정하거나 강제 복구하지 않았다. 플레이 모드는 에셋 저장을 위해 종료한 상태다.

## 다음 개선 작업 5가지

1. FBX 클립의 NaN 원인을 조사하고 Generic/Humanoid 리타게팅 또는 GLB 변환 경로로 Idle/Walk/Run 정상 재생 검증.
2. 망치질·검 집기·담금질 클립, 손 IK 및 도구 소켓 회전 프리셋 제작.
3. 불꽃 파티클·불티·연기·수증기와 칼날 온도에 따른 발광 제어 연결.
4. 모루 뿔 충돌체와 담금질 트리거를 실제 게임 검/망치로 플레이 테스트.
5. 캐릭터와 화로 메시/재질을 선택적으로 병합하고 LOD 및 드로콜 프로파일링.
