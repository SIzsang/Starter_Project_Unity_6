# 가로형 PC·모바일 예제 UI

최종 사용법 갱신: 2026-10-05. 범위는 Boot·Title·Main 예제 화면과 공통 로딩 화면이다. 게임별 HUD와 콘텐츠 UI는 새 게임에서 설계한다.

## 현재 설정

- Canvas Render Mode: `Screen Space - Overlay`. 카메라 거리나 투영 방식과 독립적인 화면 UI다.
- Canvas Scaler: `Scale With Screen Size`, 기준 `1920×1080`, `Match Width Or Height = 0.5`. 이것은 UI 좌표의 설계 기준이며 실제 화면 출력 해상도의 상한은 아니다.
- 모바일 Player Settings: 자동 회전에서 세로·역세로를 제외하고 가로 좌·우만 허용한다. PC 창의 가로·세로 크기 변경에도 레이아웃을 다시 계산한다.
- 2026-10-04 최신 main은 PC Player의 `Resizable Window`를 활성화한다. v1.0.0은 레이아웃이 크기 변경에 대응하지만 Player 창의 사용자 크기 조절 옵션은 꺼져 있었다. [Unity 창 크기 조절 설정](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings-resizableWindow.html)
- `StarterCanvasLayout`이 화면 전체 배경·입력 차단막은 Canvas에 두고, 버튼·문구·진행률은 `Safe Area/Content`에 넣는다. `UnityEngine.Device.Screen.safeArea`로 노치·둥근 모서리 등의 여백을 반영하며 Device Simulator의 화면 값도 사용할 수 있다.
- 기존 예제 콘텐츠의 좌표·크기·글꼴을 1.5배로 옮겨 씬 자체를 1920×1080 디자인 좌표로 작성한다. 실행 중 별도로 확대하지 않으며 더 작은 화면이나 안전 영역에서는 들어갈 만큼 축소한다. 해상도·안전 영역·Canvas 배율이 바뀌면 다시 맞춘다.

같은 16:9 비율에서는 1920×1080의 버튼 1개가 1280×720에서 가로·세로 각각 2/3, 960×540에서 1/2 크기로 표시된다. **UI 요소가 원래 픽셀 크기 그대로 유지되는 것은 아니다.** 상대 위치와 비율이 유지된다. 화면 비율이 달라지면 배경은 화면을 채우지만 조작 콘텐츠는 안전 영역에 들어가도록 축소되어 여백이 생길 수 있다. 새 게임의 HUD나 긴 목록에는 개별 앵커·레이아웃 그룹·스크롤 설계가 필요하다. 실행 중 새로 만드는 조작 UI는 `StarterCanvasLayout.ContentRoot` 아래에 둔다.

세 씬은 원래 Screen Space Overlay와 1280×720 Canvas Scaler를 사용했다. 세 씬의 직렬화 설정과 UI 요소, 생성 도구·런타임 적용·`Validate Setup`을 1920×1080으로 통일하되 기존 화면의 상대 비율과 버튼 참조·입력 연결은 유지한다.

## Title 메인 메뉴 — 2026-10-05

Title은 좌측 New Game / Continue / Options / Credits / PC Quit와 별도 슬롯 선택·관리·삭제 확인 화면을 씬에 작성하고 `StarterTitleMenu`의 참조로 연결한다. 새 게임은 빈 슬롯, 이어하기는 저장 슬롯을 선택한다. Core가 저장·삭제를 처리하므로 배경·버튼·문구 에셋을 바꿔도 파일 접근 코드를 UI에 넣지 않는다. Canvas의 1920×1080 기준과 가로형 안전 영역 계약을 유지하며 게임별 초상화·진행률·플레이 시간을 필수 데이터로 요구하지 않는다.

**구현·관련 필수 자동 검증 완료.** 새 Title은 UI 구성·메뉴 흐름·합성 입력을 확인했으며 [실행 근거](INTEGRATION_VALIDATION.md)를 따른다. 기존 해상도 테스트는 아래 날짜별 근거로 보존하며 새 Title 슬롯 화면의 실기기 가독성을 검증한 것으로 재사용하지 않는다. [현재 메뉴·저장 사용법](SETTINGS_AND_SAVE.md) · [초기 배치 비교](MAIN_MENU_AND_SAVE_SLOTS_DRAFT.md)

## 새 게임 에셋으로 화면 교체

- Boot·Title·Main 씬의 `Starter UI` 아래 `Background`, `Accent`, 각 버튼에는 uGUI `Image`가 있다. 스프라이트를 `Source Image`에 넣고 색상·`Image Type`·`RectTransform`을 게임 에셋에 맞춘다. 버튼의 `Button.targetGraphic`, Boot/Main의 `StarterScreen`과 Title의 `StarterTitleMenu`에 직렬화된 버튼·페이지·상태 `Text` 참조는 유지한다. 버튼 자식 `Label`의 글꼴·문구·색상도 Inspector에서 교체할 수 있다.
- 전체 배경은 Canvas 직속에서 화면 끝까지 채운다. 자식이 없는 전체 화면 장식·입력 차단막은 그대로 유지하며 버튼·자식이 있는 조작 Panel은 안전 영역으로 이동한다. 장식 자식이 있는 배경은 Canvas에 `StarterCanvasLayout`을 미리 추가한 뒤 `Full Screen Roots`에 Canvas 직속 배경을 연결한다. 실행 중 추가하는 조작 UI는 `ContentRoot` 아래에 둔다.
- 로딩 화면을 바꿀 때는 `StarterLoadingOverlay`가 붙은 프리팹을 만들고 `Canvas`, `CanvasScaler`, `GraphicRaycaster`, `CanvasGroup`을 유지한다. 프리팹의 `Message`·`Progress Track`·`Progress Fill`·`Percentage` 참조에 각각 uGUI `Text`·`Image`를 연결하고, Boot 씬 `Starter UI`의 `StarterScreen > Loading Overlay Prefab`에 프리팹을 할당한다. 배경·막대·장식 이미지에는 원하는 스프라이트를 넣는다. 전환 중 입력을 막는 투명 전체 화면 이미지는 런타임이 추가한다. 프리팹 참조가 빠지면 경고를 남기고 기본 로딩 시각 요소를 생성한다.
- 로딩 프리팹의 일반 `Progress Fill` Image는 `Progress Track`의 자식으로 둔다. 왼쪽부터 채워지는 막대로 앵커·오프셋이 정규화된다. `Image Type = Filled`를 사용하면 작성한 RectTransform·Fill Method·Origin을 유지하고 `fillAmount`만 갱신하므로 원형·세로 진행률도 사용할 수 있다.
- 사용자 UI로 전면 교체하면 공통 `Validate Setup`만 적용한다. 기본 StarterScreen·StarterTitleMenu 화면을 계속 쓸 때에는 `Validate Example UI`로 화면·입력 연결도 검사한다.
- 현재 동적 문구는 uGUI `Text`를 갱신한다. TextMeshPro 등 다른 텍스트 컴포넌트로 교체하려면 해당 화면 표시 스크립트도 함께 조정한다. 게임별 HUD·레이아웃과 터치 목표 크기는 실제 사용 기기에서 다시 설계한다.

## 검증과 남은 확인

- Unity 6000.3.16f1 격리 복제본: EditMode 46개·PlayMode 29개 통과. 새 PlayMode 테스트는 가로형 모바일의 양쪽 안전 여백, PC 해상도 변경, 전체 배경 유지, 중복 계층 생성을 확인한다.
- Windows x64 Development 빌드 성공(172,177,793 bytes). 검증용 제품명 `StarterProjectResponsiveValidation`의 서로 다른 Player 프로세스에서 `CREATE_PASS`·`RESUME_PASS`를 확인했다. `Logs/ResponsiveEditModeValidated.xml`, `Logs/ResponsivePlayModeValidated.xml`, `Logs/ResponsiveWindowsBuildValidated.log`와 복제본 `Builds/Windows/ResponsivePlayerCreate.log`·`ResponsivePlayerResume.log`가 근거다.
- 1920×1080 디자인 좌표 이관 후 EditMode 46개·PlayMode 29개가 다시 통과했다. `Logs/FullHdMigratedEditMode.xml`·`Logs/FullHdFinalPlayMode.xml`에 결과가 있다. 격리 제품명 `StarterProjectFullHdValidation`의 Windows x64 Development 빌드(172,177,785 bytes)와 서로 다른 헤드리스 Player 프로세스의 `CREATE_PASS`·`RESUME_PASS`도 확인했다. 근거는 `Logs/FullHdIsolatedWindowsBuild.log`와 복제본 `Builds/Windows/FullHdIsolatedcreate.log`·`FullHdIsolatedresume.log`다. 헤드리스 결과는 실제 화면 가독성의 증거가 아니다.
- 현재 Unity 설치에 Android·iOS 빌드 모듈이 없어 모바일 앱 패키지 생성은 수행하지 않았다. 실제 기기의 시각·터치·노치 검증도 남아 있다.
- 실제 창/기기에서 긴 오류 문구와 버튼 터치 크기, 좁은 가로 비율의 가독성을 확인해야 한다. 이 확인 전에는 모든 모바일 해상도에서 완성됐다고 판정하지 않는다.

참고: [Unity Canvas Scaler](https://docs.unity3d.com/cn/2023.2/Manual/script-CanvasScaler.html), [Unity Screen.safeArea](https://docs.unity3d.com/ja/current/ScriptReference/Screen-safeArea.html), [Unity Device.Screen](https://docs.unity3d.com/cn/6000.0/ScriptReference/Device.Screen.html).
