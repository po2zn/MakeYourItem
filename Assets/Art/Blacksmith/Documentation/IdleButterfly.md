# 대기 동작과 나비 방문

현재 게임의 ForgeWorkshopVisuals가 ForgeSmithIdle을 초기화합니다. 대장장이의 Spine2와 Head에 작은 호흡·흔들림·고개 움직임을 적용하며, 제작 시작 전에 기준 자세로 복원합니다. 기존 제작 모션을 교체하지 않습니다.

대기 시간이 25~55초 누적되면 나비가 3초 동안 접근하고, 코 위에서 4초 머문 뒤 2.5초 동안 날아갑니다. 제작을 시작하면 즉시 이탈합니다. 방문 간격은 각 대장장이마다 독립적이며 제작 확률의 난수 상태를 변경하지 않습니다. ForgeSmithIdle의 visitInterval, approachSeconds, perchSeconds, departureSeconds를 조절할 수 있습니다.

## 출처와 라이선스

Butterfly by Polyfork (https://polyfork.dev/asset/butterfly-base), CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/).

Polyfork MCP search_assets 및 get_asset으로 외형·라이선스를 확인한 무료 모델입니다. 원본 FBX와 GLB는 Source/Butterfly에 보존했습니다. 3,611 삼각형, 스킨 리그 포함. GLB는 재질 1개이며 FBX 내보내기는 색상별 재질 8개로 분리됩니다. Unity에는 8개 URP Lit 재질을 저장하여 원본 팔레트를 유지했습니다. 단위는 미터, 모델 크기는 약 3.4×6.9×6.1cm입니다. 유료 구매 및 신규 모델 생성은 없습니다.

원본 FBX에 Idle(7초), Fly(2.8초), Fly fast(1.65초) 클립이 포함되어 있습니다. 게임에서는 이 클립을 재생하지 않고, 원본이 명시한 날개 회전 범위(앞날개 0~12도, 뒷날개 0~6도) 안에서 절차적으로 날갯짓합니다. 대장장이 Idle 역시 런타임 뼈대 애니메이션이며 별도 .anim 클립은 아닙니다.

변경: URP 재질 변환, 게임용 Butterfly.prefab, Copperbeard Prefab의 Head 아래 ButterflyNoseAnchor 추가, Resources 카탈로그 연결. 원본 모델은 변경하지 않았습니다. 기존 장면·ProjectSettings·Packages 변경은 없습니다.

## 사용 및 검증

기존 인게임을 실행하고 제작하지 않은 채 25~55초 기다리면 자동으로 방문합니다. 기존/고용 대장장이 모두 같은 에셋 카탈로그를 사용합니다. Butterfly Prefab이 없는 카탈로그에서는 대기 동작만 작동합니다. 다른 캐릭터로 변경할 경우 Head 아래 ButterflyNoseAnchor를 해당 코 위치에 배치해야 합니다.

BlacksmithIdleChecks.Run은 Play 모드의 실제 프레임 진행과 고개 움직임, 착지 위치, 정상 이탈, 작업 중단 및 취소를 확인합니다. IdleChecks.txt에 결과, IdleButterfly.png에 실제 렌더를 저장했습니다. 검증에서는 대기 시간을 단축하여 상태 전환을 확인했으며 전체 25~55초 구간을 수동으로 기다린 검증은 아닙니다.

## 후속 개선 후보

1. 현재 작은 실물 크기 나비가 기본 게임 카메라에서 더 잘 보이도록 가독성 조정.
2. 날개를 넓게 펼치는 전용 비행 클립 제작 및 리그 범위 검증.
3. 코에 앉을 때 눈동자 추적이나 표정 반응 추가.
4. 여러 대장장이가 있을 때 동시 방문 수 제한.
5. 긴 대기 중 스트레칭 등 추가 Idle 변형.
