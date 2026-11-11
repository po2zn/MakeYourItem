# 사용자 제공 화로 교체

- 원본: Interiror/화로 폴더의 Meshy_AI_Hearthstone_Forge_20261111_texture.fbx 및 PNG. 사용자 제공 파일이며 별도 라이선스 정보는 제공되지 않음. Polyfork 모델이 아님.
- 원본과 기존 화로 Prefab을 보존하고 Prefabs/Forge_UserModel.prefab 생성.
- Materials/Forge_UserTexture.mat: 원본 텍스처를 사용하는 URP Lit 재질 1개. 모델 2,062 삼각형.
- 높이 1.5m, 바닥 중앙 피벗. 입구가 카메라 쪽(-Z)을 향하도록 180도 회전.
- HeatTarget (0, 0.66, -0.2)에 가열 지점 배치. 별도 FireGlow 점광원을 기존 가열 연출에 연결. 입구를 막지 않는 받침·양쪽 벽·뒷벽·굴뚝 BoxCollider 구성.
- Resources/BlacksmithWorkshopAssets.forge 참조 교체로 게임에 자동 적용. 설치 도구가 교체된 화로 참조를 덮어쓰지 않도록 변경.
- 원본 불꽃과 숯은 통합 메시/텍스처에 포함되어 그대로 유지. 별도 제어 가능한 것은 FireGlow 조명이며 불꽃 메시를 분리하거나 새 불꽃 애니메이션을 제작하지는 않음.
- Play 모드 제작 회귀 검증 통과. Integration_Process2.png에서 화로 입구·가열 조명 시각 확인.
