# Phase 2 실행 기반

앱 초기화는 AppState, 실행 상태는 GameState로 구분한다. AppRoot는 앱 소유권과 요청 검사를 담당하고, AppBootstrapper는 초기화 순서, SceneFlow는 씬 생명주기, AppServices는 타입별 공통 서비스의 소유권을 담당한다. Core는 UI와 Game Layer를 참조하지 않는다.

## 이 문서의 순서

- [초기화와 소유권](#초기화와-소유권)
- [SceneRoot](#sceneroot)
- [비동기 수명](#비동기-수명)
- [Definition / Runtime / 저장 경계](#definition--runtime--저장-경계)
- [입력](#입력)
- [Pause와 Audio](#pause와-audio)
- [검증과 다음 작업](#검증과-다음-작업)

## 초기화와 소유권

1. AppConfig와 선택형 DataCatalog의 ID·참조·게임별 규칙 검증
2. 씬 경로·기본 설정의 RuntimeAppConfig 사본과 Definition 조회 목록 준비
3. SettingsService와 GameSessionService 생성·기존 저장 읽기
4. InputContextService / PauseService / AudioService 조립
5. 기존 IRuntimeSettings로 음량·화면 적용
6. 활성 SceneRoot의 Initialize → Enter 완료 후 Ready 공개

AppRoot.Services의 Settings / Game / Data / Input / Pause / Audio를 직접 조회한다. 기존 AppRoot.Settings / Game API도 유지한다. 서비스 등록·검색 컨테이너나 추가 Singleton은 없다. 루트 종료·실패 시 서비스는 역순으로 정리한다. 제공한 IRuntimeSettings는 성공·실패 모두 앱이 소유한다. ITextFileStore는 빌려 쓰며 주입자가 그 수명을 소유한다. AppServices.IsDisposed 및 AppRoot.LifetimeToken으로 종료 여부를 확인한다.

## SceneRoot

작은 UI 예제 씬은 별도 SceneRoot 없이 동작한다. SceneFlow가 UI 컨텍스트의 기본 루트를 생성한다. 실제 Gameplay 씬에는 활성 SceneRoot 파생 컴포넌트를 하나만 둔다. Inspector의 Initial Input Context 기본값은 Gameplay다.

게임 코드는 protected OnInitializeAsync / OnEnterAsync / OnExitAsync / OnDispose를 구현한다. Initialize에서 서비스와 게임 데이터를 연결하고 Enter에서 실행을 시작한다. 정상 씬 전환은 Exit 후 Dispose한다. 강제 파괴·실패에는 Dispose만 보장한다. Dispose는 여러 번 호출해도 한 번만 실행된다. 훅은 메인 스레드에서 실행하며 Unity Awaitable은 한 번만 await한다.

SceneFlow는 AppRoot의 기존 Title/Main 전환 API를 통해 사용한다. 게임이 SceneManager로 직접 씬을 교체하면 이 정상 Exit 계약을 거치지 않는다. Core가 임의의 씬을 Gameplay라고 추측하지 않는다.

## 비동기 수명

- 앱 범위: AppRoot.LifetimeToken. 실패와 파괴 시 취소한다.
- 씬 범위: SceneRoot.LifetimeToken. Exit 시작 또는 Dispose 시 취소한다.
- 객체 범위: 아래처럼 씬 토큰과 destroyCancellationToken을 함께 연결한다.

```csharp
// Awake 등 객체가 살아 있을 때 만들고 OnDestroy에서 Dispose한다.
lifetime = new AsyncLifetime(sceneRoot.LifetimeToken, destroyCancellationToken);
await Awaitable.NextFrameAsync(lifetime.Token);
lifetime.Token.ThrowIfCancellationRequested();
// 이 검사를 통과한 뒤에만 Unity 객체/데이터에 결과를 적용한다.
```

Exit 훅은 이미 취소된 씬 토큰 대신 전달된 앱 토큰으로 종료를 기다린다. Dispose는 동기 해제만 수행한다. 취소할 수 없는 Unity 씬 로드는 끝까지 관찰하고 종료된 앱에 완료 결과를 적용하지 않는다. Game Layer도 전달된 토큰을 사용하고 await 이후 결과 적용 전에 취소를 검사해야 한다.

## Definition / Runtime / 저장 경계

DataDefinition은 ID와 필수 참조 ID를 가진 읽기 전용 SO 원본이다. 게임별 고정 필드는 파생 SO에 둔다. DataCatalog는 선택형 목록이며 비어 있어도 앱이 동작한다. AppConfig의 Data Catalog 슬롯에 연결하면 기존 Validate Setup과 Boot에서 ID 누락·중복·잘못된 참조를 검출한다. Validate 재정의는 원본 변경·I/O 없이 반복 호출 가능해야 한다. 대규모 비동기 데이터 로딩은 이번 기반에 포함하지 않는다.

DataService.Definitions / TryGetDefinition으로 원본을 읽고 수정하지 않는다. 새 게임·이어하기는 GameSession의 JSON에서 독립 RuntimeSessionData를 생성한다.

```csharp
var root = AppRoot.Instance;
root.Data.Runtime.Payload["stage"] = 3; // 플레이 중 작업 데이터
bool saved = root.TrySaveGame();       // 호출 시 JSON 스냅샷을 저장
```

GameSession.Current의 저장 스냅샷과 파일은 작업 데이터 변경만으로 바뀌지 않는다. 저장 실패 시 작업 데이터와 기존 저장은 보존한다. 명시적 JSON을 TrySaveGame(json)에 전달해 성공하면 작업 Payload 객체에 그 스냅샷을 반영한다. Title 복귀는 이전 씬의 Exit/Dispose 완료 뒤, Title Initialize 전에 세션과 Runtime을 해제하며 자동 저장하지 않는다. 저장 버전·슬롯·백업·미래 버전 보호 정책은 기존 형식을 유지한다. SnapshotPayload는 기존 저장 크기 제한과 검증의 입력이다. Unity 객체를 자동 스캔해 저장하지 않는다.

## 입력

Core는 None / Gameplay / UI / Debug 컨텍스트와 수명만 관리한다. Input System 구현은 UI 어셈블리의 StarterInputContext 어댑터에 둔다. 기본 StarterScreen과 StarterTitleMenu가 앱에 한 번 생성한다. 사용자 UI는 StarterInputContext.EnsureCreated(root)를 호출하거나 같은 Core 입력 계약에 맞는 어댑터를 제공한다.

어댑터는 첫 EventSystem의 InputActionAsset을 앱 전용으로 복제한다. 공유 원본을 수정하지 않으며 이후 씬의 UI 모듈을 같은 복제본에 연결한다. 게임 입력도 어댑터의 RuntimeActions에서 읽는다. Player / UI 이름을 사용하고 선택형 Debug 맵을 지원한다. 이후 씬도 같은 액션 이름·맵 계약을 사용해야 한다.

| 컨텍스트 | 활성 맵 |
| --- | --- |
| None | 없음, UI 모듈 비활성 |
| Gameplay | Player |
| UI | UI |
| Debug | UI와 선택형 Debug |

Boot / Loading / Failed는 강제 None, Menu는 UI, Gameplay는 SceneRoot의 초기 컨텍스트다. 예제 Main의 기본 루트는 UI이므로 기존 버튼이 동작한다. 팝업·디버그 화면은 임시 범위를 사용한다.

```csharp
IDisposable inputScope = root.Input.Push(InputContext.UI, sceneRoot.LifetimeToken);
// 닫힐 때 inputScope.Dispose(); 씬 종료 시에도 자동 해제
```

메인 스레드에서 사용한다. 마지막 유효 범위가 우선하며 중간 범위를 먼저 해제해도 최상위 범위가 유지된다. Loading과 Pause는 임시 범위를 보존한 채 강제 컨텍스트를 적용하고 해제 시 복구한다. 전역 키 매핑·Rebinding·UI Navigation 스택은 별도 후속 모듈이다.

## Pause와 Audio

root.TryChangeGameState(GameState.Pause) / Gameplay를 사용한다. 활성 게임 세션에서만 허용하며 상태 알림 중 재진입은 거절한다. Pause 진입 전 Time.timeScale을 저장해 0으로 만들고 Resume / Loading / Failed / Dispose에서 저장한 값으로 복구한다. UI 및 종료 작업은 unscaled 시간과 앱 토큰을 사용한다. 예제의 StarterPauseOverlay는 Main에서 Pause 버튼과 Resume 패널을 표시한다. 게임별 UI도 같은 상태 API를 사용할 수 있다.

AudioService는 앱 아래에 2D BGM / SFX / UI AudioSource를 소유한다.

```csharp
root.Audio.PlayBgm(music, loop: true);
root.Audio.PlaySfx(hit);
root.Audio.PlayUI(click);
root.Audio.StopBgm();
```

BGM은 교체/Stop/앱 종료까지 유지된다. SFX는 OneShot으로 중첩 재생되며 씬 전환 시작 시 중지한다. Pause는 BGM/SFX를 일시정지하고 신규 SFX 요청을 거절한다. UI 채널은 Pause 중에도 재생한다. Master volume은 기존 Settings → UnityRuntimeSettings → AudioListener.volume을 재사용하며 중복 곱하지 않는다. 채널별 믹서·풀·콘텐츠 로딩은 필요가 생길 때 확장한다. 실제 클립 콘텐츠와 물리 오디오 청취는 새 게임에서 확인한다.

## 검증과 다음 작업

Phase Gate의 실제 결과는 [통합 검증 기록](INTEGRATION_VALIDATION.md)에 기록한다. Phase 2·3와 선정 Phase 4 Pool 범위는 완료했다. 다음 작업은 [후속 계획](NEXT_STEPS.md)을 따르며 이 문서는 실행 API 계약을 유지한다.
