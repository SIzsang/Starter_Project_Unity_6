# 실행과 Bootstrap 사용 가이드

Boot에서 앱을 준비하고 Title/Main으로 진입하는 기본 흐름을 설명합니다. 씬 훅과 서비스의 상세 API는 [실행 기반](PHASE_2_RUNTIME.md)을 따릅니다.

## 시작과 사용자 흐름

`Assets/01_Scenes/Boot/00_StartScene.unity`를 열고 Play합니다.

```text
Boot → 공통 준비 → Title → New Game / Continue → 슬롯 선택 → Main
```

Title의 빈 슬롯으로 새 진행을 만들고 Main에서 명시적으로 저장합니다. Title 복귀 후 저장 슬롯을 Continue로 이어갑니다. 새 게임·Title 복귀 자체는 자동 저장하지 않습니다.

Main 직접 Play의 개발 경로는 [Editor 워크플로](EDITOR_WORKFLOW.md)를 따릅니다.

## 씬과 객체 책임

| 객체 | 책임 |
| --- | --- |
| AppBootstrap | Boot의 진입점. AppRoot 확보·Begin 요청 |
| AppRoot | 앱 수명·요청 검사·서비스/상태/씬 연결 |
| AppBootstrapper / AppServices | 준비 순서와 공통 서비스 소유·해제 |
| SceneFlow / SceneRoot | 씬 전환과 해당 씬의 초기화·진입·종료 |
| StarterTitleMenu | Title 메뉴·슬롯·관리·옵션·확인 화면 |
| StarterScreen | Boot/Main의 상태와 사용자 요청 표시 |
| EventSystem·Camera·UI | 각 씬이 소유하는 화면·입력 객체 |

AppRoot는 씬 전환 뒤에도 유지됩니다. 설정 에셋은 `Assets/04_Data/Config/SO_AppConfig.asset`입니다.

## 준비·전환·실패 계약

- AppConfig의 Boot·Title·Main은 서로 다른 활성 빌드 씬이어야 하며 Boot가 첫 진입점입니다.
- 설정/데이터 검증 → 서비스 조립·설정 적용 → Scene Initialize/Enter → Ready 순서를 지킵니다.
- 초기화 중 Begin과 전환 중 추가 요청은 거절합니다.
- 필수 실패는 Failed와 이유를 표시하고 게임 진입을 막습니다.
- 루트 종료는 앱·씬 작업과 서비스/구독을 정리합니다.
- 실패 뒤 인앱 재시도는 제공하지 않으며 설정 수정 후 앱/Play를 재시작합니다.

## 실행 상태와 확장 위치

AppState는 준비 상태, GameState는 Boot/Menu/Loading/Gameplay/Pause/Failed 실행 상황입니다. 활성 진행의 Pause/Gameplay 요청은 시간·입력·Audio와 UI 상태에 연결됩니다. [상태 API](GAME_STATE.md)

공통 서비스 조립은 AppBootstrapper/AppServices, 게임 조립·실행·종료는 SceneRoot 훅에 둡니다. 게임 규칙을 AppRoot에 추가하지 않습니다.

## 화면·검증 문서

- UI 에셋·로딩 프리팹 교체: [반응형 UI](RESPONSIVE_UI.md)
- 저장·이어하기·복구: [설정과 저장](SETTINGS_AND_SAVE.md)
- 현재 Gate와 실제 한계: [검증 현황](INTEGRATION_VALIDATION.md)
- 이전 Bootstrap 테스트·보완: [과거 기록](Archive/BOOTSTRAP_HISTORY.md)
