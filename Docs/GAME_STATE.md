# GameState — Phase 2 실행 상태

`AppRoot.State`의 기존 `AppState`는 초기화 준비 여부를 나타낸다. `AppRoot.CurrentGameState`는 현재 앱/게임 실행 상태를 별도로 나타낸다. 초기화가 끝난 `AppState.Ready`에서도 씬 전환 중에는 `GameState.Loading`일 수 있다.

## 상태와 전환 계약

| 현재 상태 | 허용되는 다음 상태 |
| --- | --- |
| Boot | Loading, Menu, Gameplay, Failed |
| Menu | Loading, Failed |
| Loading | Menu, Gameplay, Failed |
| Gameplay | Pause, Loading, Failed |
| Pause | Gameplay, Loading, Failed |
| Failed | 없음 — 설정 수정 후 앱/Play 재시작 |

Boot는 초기화 전·중, Menu는 Title, Gameplay는 Main 진입 완료를 나타낸다. 같은 상태·미정의 enum 값·표에 없는 전환은 거절하며 알림도 발생시키지 않는다. Boot에서 Menu/Gameplay로 직접 가는 경로는 이미 해당 씬에서 초기화를 마친 경우다.

`GameStateController`는 일반 C# 객체로 이 규칙과 변경 알림만 소유한다. AppRoot가 실제 앱의 컨트롤러를 비공개로 소유한다. 개별 상태 클래스, DI Container, Service Locator, 전역 Event Bus는 추가하지 않는다.

## AppRoot API

```csharp
var root = AppRoot.Instance;
var current = root.CurrentGameState;
root.GameStateChanged += OnGameStateChanged; // (GameState previous, GameState current)
bool accepted = root.TryChangeGameState(GameState.Pause);
bool resumed = root.TryChangeGameState(GameState.Gameplay);
```

실제 앱에서 `TryChangeGameState`는 준비된 활성 게임 세션의 Gameplay ↔ Pause만 허용한다. Boot/Menu/Loading/Failed는 초기화·씬 전환·실패 흐름이 소유하며 외부에서 강제 변경할 수 없다. 씬 이동은 기존 `TryStartNewGame` / `TryContinueGame` / `TryReturnToTitle`을 사용한다.

**Pause는 시간·입력·Audio·UI를 함께 전환한다.** 기존 timeScale을 보관해 복구하며 입력을 UI로 강제하고 BGM/SFX를 일시정지한다. 예제 Pause/Resume 패널과 연결되며 자동 저장하지 않는다. [Phase 2 사용법](PHASE_2_RUNTIME.md)

## 씬 흐름과 알림

- 전환 잠금을 획득한 직후 Loading을 알린다. 진행률 알림마다 GameStateChanged를 반복하지 않는다.
- Unity 씬 로드·활성화가 완료되고 잠금이 해제되면 Menu/Gameplay를 알린다. 새 씬의 Awake/OnEnable 시점에는 아직 Loading일 수 있다.
- 초기화 또는 전환 실패 시 AppState와 GameState가 모두 Failed가 된다. 실패한 상태를 씬 로드 완료로 덮어쓰지 않는다.
- `GameStateChanged(previous, current)`는 값이 바뀐 뒤 동기 호출한다. 구독 시 현재 값을 한 번 읽어 초기 표시를 맞추고, 씬 소비자는 OnDisable/Dispose에서 구독을 해제한다.
- 알림 중 AppRoot 변경 요청은 거절한다. 알림은 조회·표시에 사용하며 후속 요청은 알림 종료 후 실행한다. 실패 처리는 재진입 중에도 진입 차단을 보장한다.
- AppRoot의 GameStateChanged 소비자가 예외를 던지면 경고를 기록하고 다른 소비자와 씬 흐름을 계속 처리한다.
- 루트 파괴 시 구독과 정적 참조를 정리한다. 새 루트는 Boot부터 시작한다.

현재 상태 연결은 AppRoot가 조정하는 Title/Main 흐름에 해당한다. SceneFlow가 SceneRoot의 Initialize/Enter/Exit/Dispose와 비동기 수명을 연결한다. 게임이 SceneManager로 직접 씬을 교체하면 정상 Exit 계약을 거치지 않는다. [서비스·수명 계약](PHASE_2_RUNTIME.md)

사용 가능한 저장·설정 API는 [설정·저장 가이드](SETTINGS_AND_SAVE.md), 실행 경로는 [Bootstrap 가이드](BOOTSTRAP.md), 검증 근거는 [통합 검증 기록](INTEGRATION_VALIDATION.md)을 따른다.
