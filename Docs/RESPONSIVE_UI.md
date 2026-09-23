# 가로형 PC·모바일 예제 UI

기준: 2026-09-23. 범위는 Boot·Title·Main 예제 화면과 공통 로딩 화면이다. 게임별 HUD와 콘텐츠 UI는 새 게임에서 설계한다.

## 현재 설정

- Canvas Render Mode: `Screen Space - Overlay`. 카메라 거리나 투영 방식과 독립적인 화면 UI다.
- Canvas Scaler: `Scale With Screen Size`, 기준 `1920×1080`, `Match Width Or Height = 0.5`. 이것은 UI 좌표의 설계 기준이며 실제 화면 출력 해상도의 상한은 아니다.
- 모바일 Player Settings: 자동 회전에서 세로·역세로를 제외하고 가로 좌·우만 허용한다. PC 창의 가로·세로 크기 변경에도 레이아웃을 다시 계산한다.
- `StarterCanvasLayout`이 화면 전체 배경·입력 차단막은 Canvas에 두고, 버튼·문구·진행률은 `Safe Area/Content`에 넣는다. `UnityEngine.Device.Screen.safeArea`로 노치·둥근 모서리 등의 여백을 반영하며 Device Simulator의 화면 값도 사용할 수 있다.
- 기존 예제 콘텐츠의 좌표·크기·글꼴을 1.5배로 옮겨 씬 자체를 1920×1080 디자인 좌표로 작성한다. 실행 중 별도로 확대하지 않으며 더 작은 화면이나 안전 영역에서는 들어갈 만큼 축소한다. 해상도·안전 영역·Canvas 배율이 바뀌면 다시 맞춘다.

같은 16:9 비율에서는 1920×1080의 버튼 1개가 1280×720에서 가로·세로 각각 2/3, 960×540에서 1/2 크기로 표시된다. **UI 요소가 원래 픽셀 크기 그대로 유지되는 것은 아니다.** 상대 위치와 비율이 유지된다. 화면 비율이 달라지면 배경은 화면을 채우지만 조작 콘텐츠는 안전 영역에 들어가도록 축소되어 여백이 생길 수 있다. 새 게임의 HUD나 긴 목록에는 개별 앵커·레이아웃 그룹·스크롤 설계가 필요하다. 실행 중 새로 만드는 조작 UI는 `StarterCanvasLayout.ContentRoot` 아래에 둔다.

세 씬은 원래 Screen Space Overlay와 1280×720 Canvas Scaler를 사용했다. 세 씬의 직렬화 설정과 UI 요소, 생성 도구·런타임 적용·`Validate Setup`을 1920×1080으로 통일하되 기존 화면의 상대 비율과 버튼 참조·입력 연결은 유지한다.

## 검증과 남은 확인

- Unity 6000.3.16f1 격리 복제본: EditMode 46개·PlayMode 29개 통과. 새 PlayMode 테스트는 가로형 모바일의 양쪽 안전 여백, PC 해상도 변경, 전체 배경 유지, 중복 계층 생성을 확인한다.
- Windows x64 Development 빌드 성공(172,177,793 bytes). 검증용 제품명 `StarterProjectResponsiveValidation`의 서로 다른 Player 프로세스에서 `CREATE_PASS`·`RESUME_PASS`를 확인했다. `Logs/ResponsiveEditModeValidated.xml`, `Logs/ResponsivePlayModeValidated.xml`, `Logs/ResponsiveWindowsBuildValidated.log`와 복제본 `Builds/Windows/ResponsivePlayerCreate.log`·`ResponsivePlayerResume.log`가 근거다.
- 1920×1080 디자인 좌표 이관 후 EditMode 46개·PlayMode 29개가 다시 통과했다. `Logs/FullHdMigratedEditMode.xml`·`Logs/FullHdFinalPlayMode.xml`에 결과가 있다. 격리 제품명 `StarterProjectFullHdValidation`의 Windows x64 Development 빌드(172,177,785 bytes)와 서로 다른 헤드리스 Player 프로세스의 `CREATE_PASS`·`RESUME_PASS`도 확인했다. 근거는 `Logs/FullHdIsolatedWindowsBuild.log`와 복제본 `Builds/Windows/FullHdIsolatedcreate.log`·`FullHdIsolatedresume.log`다. 헤드리스 결과는 실제 화면 가독성의 증거가 아니다.
- 현재 Unity 설치에 Android·iOS 빌드 모듈이 없어 모바일 앱 패키지 생성은 수행하지 않았다. 실제 기기의 시각·터치·노치 검증도 남아 있다.
- 실제 창/기기에서 긴 오류 문구와 버튼 터치 크기, 좁은 가로 비율의 가독성을 확인해야 한다. 이 확인 전에는 모든 모바일 해상도에서 완성됐다고 판정하지 않는다.

참고: [Unity Canvas Scaler](https://docs.unity3d.com/cn/2023.2/Manual/script-CanvasScaler.html), [Unity Screen.safeArea](https://docs.unity3d.com/ja/current/ScriptReference/Screen-safeArea.html), [Unity Device.Screen](https://docs.unity3d.com/cn/6000.0/ScriptReference/Device.Screen.html).
