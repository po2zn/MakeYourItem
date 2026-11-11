# Copperbeard 대장장이 교체

사용자가 제공한 `Characters/MeshCharacter/Meshy_AI_Copperbeard_the_Craftsman_20261111_texture.fbx` 및 동명 PNG를 게임에 적용했다. 제공된 원본은 본이 없는 MeshRenderer 메시(2351 vertices, 2072 triangles)였다. FBX/PNG와 기존 원본 프리팹은 변경하지 않았다.

게임 연결: `Prefabs/Resources/BlacksmithWorkshopAssets.asset`의 blacksmith 참조를 `Prefabs/Blacksmith_Copperbeard.prefab`으로 변경했다. SampleScene 재생 시 자동 적용되고 추가 공방도 같은 모델을 사용한다. 설치 도구가 기존 캐릭터 선택을 덮어쓰지 않도록 수정했다.

`Characters/CopperbeardRigged/CopperbeardMesh.asset`: 18개 변형 본과 위치 기반 스킨 가중치를 가진 별도 메시. 높이 1.75m, 지면 원점, 기존과 동일한 본 이름 및 좌우 손 소켓을 구성했다. 기존 Polyfork 기반 리그 계층을 호환 골격으로 사용하므로 기존 Polyfork 저작자 표시는 유지한다. 제공된 새 모델·텍스처의 출처/라이선스는 사용자 제공 자료이며 별도 권리를 추정하지 않았다.

`Characters/CopperbeardRigged/Copperbeard.mat`: 원본 PNG를 사용하는 URP Lit 재질. 캐릭터 메시가 하나이므로 기존 분리 앞치마를 찾지 못해 초기화에 실패하지 않도록 ForgeWorkshopVisuals에서 앞치마 색상 제어를 선택 사항으로 처리했다. 새 캐릭터 전체 텍스처를 앞치마 색으로 덮지 않는다.

이동·호흡·가열·망치질·공기 냉각·물/서리수 담금질의 기존 절차적 본 애니메이션과 시간은 유지했다. 새 FBX에 애니메이션 클립이 추가된 것은 아니다. 자동 가중치 기반 리깅으로, 손가락 개별 파지 및 전문 수작업 관절 변형 교정은 포함하지 않는다.

검증: Unity 컴파일 성공, 기존 제작 규칙 12개, 새 모델의 가열/타격/냉각/다중 작업물/중단 검사, 냉각 재료 0/3/8 검사 통과. 실제 게임에서 철 10→9, 물 2→1, 4회 작업 후 상점에 완제품 1개 등록 확인. 저장 슬롯은 사용하지 않았다. Integration_Process2/3/4.png를 새 모델로 렌더링하여 육안 확인했다. 테스트 후 편집 모드로 복귀했다.

변경 코드: `Assets/Editor/Blacksmith/CopperbeardSetup.cs`(별도 프리팹/리깅 제작), `BlacksmithInstallIntegration.cs`(선택 보존), `Source/ForgeWorkshopVisuals.cs`(앞치마 없는 모델 지원). 작업 애니메이션 곡선·위치·냉각 타이밍은 변경하지 않았다. 기존 장면·ProjectSettings·Packages를 직접 변경하지 않았다.
