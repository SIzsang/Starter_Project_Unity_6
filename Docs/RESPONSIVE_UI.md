# 가로형 PC·모바일 예제 UI

기준: 2026-09-23. 범위는 Boot·Title·Main 예제 화면과 공통 로딩 화면이다. 게임별 HUD와 콘텐츠 UI는 새 게임에서 설계한다.

## 현재 설정

- Canvas Render Mode: `Screen Space - Overlay`. 카메라 거리나 투영 방식과 독립적인 화면 UI다.
- Canvas Scaler: `Scale With Screen Size`, 기준 `1280×720`, `Match Width Or Height = 0.5`.
- 모바일 Player Settings: 자동 회전에서 세로·역세로를 제외하고 가로 좌·우만 허용한다. PC 창의 가로·세로 크기 변경에도 레이아웃을 다시 계산한다.
- `StarterCanvasLayout`이 화면 전체 배경·입력 차단막은 Canvas에 두고, 버튼·문구·진행률은 `Safe Area/Content`에 넣는다. `UnityEngine.Device.Screen.safeArea`로 노치·둥근 모서리 등의 여백을 반영하며 Device Simulator의 화면 값도 사용할 수 있다.
- 콘텐츠는 1280×720 배치를 유지한 채 안전 영역에 들어갈 만큼만 축소한다. 화면이 넓어졌다는 이유로 임의 확대하지 않는다. 해상도·안전 영역·Canvas 배율이 바뀌면 다시 맞춘다.

세 씬은 이미 Screen Space Overlay와 같은 기준 Canvas Scaler를 사용했다. 이번 보완은 그 계약을 런타임에서 일관되게 적용하고 `Validate Setup`에서 Boot·Title·Main의 Canvas 설정을 검사하며, 안전 영역에 맞게 콘텐츠를 배치하는 것이다. 기존 버튼 참조와 입력 연결은 유지한다.

## 검증과 남은 확인

- Unity 6000.3.16f1 격리 복제본: EditMode 46개·PlayMode 29개 통과. 새 PlayMode 테스트는 가로형 모바일의 양쪽 안전 여백, PC 해상도 변경, 전체 배경 유지, 중복 계층 생성을 확인한다.
- Windows x64 Development 빌드 성공(172,177,793 bytes). 검증용 제품명 `StarterProjectResponsiveValidation`의 서로 다른 Player 프로세스에서 `CREATE_PASS`·`RESUME_PASS`를 확인했다. `Logs/ResponsiveEditModeValidated.xml`, `Logs/ResponsivePlayModeValidated.xml`, `Logs/ResponsiveWindowsBuildValidated.log`와 복제본 `Builds/Windows/ResponsivePlayerCreate.log`·`ResponsivePlayerResume.log`가 근거다.
- 현재 Unity 설치에 Android·iOS 빌드 모듈이 없어 모바일 앱 패키지 생성은 수행하지 않았다. 실제 기기의 시각·터치·노치 검증도 남아 있다.
- 실제 창/기기에서 긴 오류 문구와 버튼 터치 크기, 좁은 가로 비율의 가독성을 확인해야 한다. 이 확인 전에는 모든 모바일 해상도에서 완성됐다고 판정하지 않는다.

참고: [Unity Canvas Scaler](https://docs.unity3d.com/cn/2023.2/Manual/script-CanvasScaler.html), [Unity Screen.safeArea](https://docs.unity3d.com/ja/current/ScriptReference/Screen-safeArea.html), [Unity Device.Screen](https://docs.unity3d.com/cn/6000.0/ScriptReference/Device.Screen.html).
