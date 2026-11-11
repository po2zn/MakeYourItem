# 사용자 제공 모루 교체

- 원본: Assets/Art/Blacksmith/Interiror/모루/Meshy_AI_Anvil_on_Wooden_Stand_20261111_texture.fbx 및 PNG. 원본 파일과 GUID 보존.
- 출처: 사용자가 제공한 모델. 라이선스는 별도로 제공되지 않았으며 Polyfork 모델이 아님.
- 게임용: Assets/Art/Blacksmith/Prefabs/Anvil_UserModel.prefab.
- URP Lit 재질: Assets/Art/Blacksmith/Materials/Anvil_UserTexture.mat.
- 1,832 삼각형, 재질 1개, 전체 높이 0.83m, 바닥 중앙 피벗. 목재 받침 포함 원본 메시를 그대로 사용.
- 바닥 받침과 작업면에 간단한 BoxCollider 2개. StrikeTarget 위치 (0.06, 0.835, 0).
- Resources/BlacksmithWorkshopAssets의 anvil 참조를 교체하여 기존 게임 및 고용 대장장이에 적용. 기존 모루 Prefab 보존. 재설치 도구가 교체된 참조를 덮어쓰지 않도록 수정.
- Play 모드 제작 회귀 검증 통과. Integration_Process3.png에서 새 모루 및 망치질 작업면 시각 확인.
