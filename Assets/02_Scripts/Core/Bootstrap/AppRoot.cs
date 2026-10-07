using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarterProject
{
    /// <summary>초기화가 끝난 뒤 처음 열 화면과 빈 게임 세션의 생성 여부입니다.</summary>
    public enum AppStartupDestination { Title, NewGameInMain }

    /// <summary>
    /// 씬 전환 후에도 유지되는 앱 단위 실행 루트입니다. 초기화 상태와 실패 정보,
    /// Title/Main 전환 잠금을 한곳에서 관리하며 <see cref="AppBootstrap"/>과
    /// 씬별 UI의 공통 연결 지점 역할을 합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppRoot : MonoBehaviour
    {
        /// <summary>현재 플레이 세션에서 유효한 단일 앱 루트입니다.</summary>
        public static AppRoot Instance { get; private set; }

        /// <summary>현재 앱 초기화 상태입니다.</summary>
        public AppState State { get; private set; }

        /// <summary>AppState와 독립적인 현재 실행 상태입니다. 씬 상태는 AppRoot가 전환합니다.</summary>
        public GameState CurrentGameState => gameState.Current;

        /// <summary>성공한 실행 상태 전환의 이전·현재 값을 알립니다. 씬 소비자는 해제 시 구독을 정리합니다.</summary>
        public event Action<GameState, GameState> GameStateChanged;

        private readonly GameStateController gameState = new GameStateController();
        private SceneFlow sceneFlow;
        public SceneRoot CurrentSceneRoot => sceneFlow?.Current;

        /// <summary>씬 비동기 로드가 진행 중이어서 추가 전환을 받을 수 없는지 나타냅니다.</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>씬 로드의 실제 진행률입니다. 씬 활성화까지 끝나야 1이 됩니다.</summary>
        public float LoadingProgress { get; private set; }

        /// <summary>화면과 입력이 상태 변경을 같은 프레임에 반영하도록 알립니다.</summary>
        public event Action StateChanged;

        /// <summary>화면에 표시할 현재 초기화 또는 전환 단계입니다.</summary>
        public string CurrentStep { get; private set; } = "Waiting to start";

        /// <summary>실패 시 사용자 또는 개발자 화면에 전달할 오류 메시지입니다.</summary>
        public string Failure { get; private set; } = string.Empty;

        private CancellationToken lifetimeToken;
        private AsyncLifetime appLifetime;
        public CancellationToken LifetimeToken => lifetimeToken;
        private string bootScenePath;
        private string titleScenePath;
        private string mainScenePath;
        private AppServices services;
        public AppServices Services => services;
        public DataService Data => services?.Data;
        public InputContextService Input => services?.Input;
        public PauseService Pause => services?.Pause;
        public AudioService Audio => services?.Audio;
        public SettingsService Settings => services?.Settings;
        public GameSessionService Game => services?.Game;
        public string StorageMessage { get; private set; } = "";
        private ITextFileStore fileStore;
        private GamePayloadPolicy gamePayloadPolicy;
        private IRuntimeSettings runtimeSettings;
        private bool isCommittingSettings;
        private AppStartupDestination startupDestination;
        private bool startupDestinationConfigured;

        /// <summary>Begin 전에 최초 진입 경로를 설정합니다. 기본 경로는 Title입니다.</summary>
        public void ConfigureStartupDestination(AppStartupDestination destination)
        {
            if (State != AppState.NotStarted) throw new InvalidOperationException("Configure startup destination before Begin.");
            if (startupDestinationConfigured) throw new InvalidOperationException("Startup destination is already configured.");
            if (!Enum.IsDefined(typeof(AppStartupDestination), destination)) throw new ArgumentOutOfRangeException(nameof(destination));
            startupDestination = destination;
            startupDestinationConfigured = true;
        }

        /// <summary>Begin 전에 시스템 설정 적용 경계를 주입합니다. 루트가 수명을 소유합니다.</summary>
        public void ConfigureRuntimeSettings(IRuntimeSettings runtimeSettings)
        {
            if (State != AppState.NotStarted) throw new InvalidOperationException("Configure runtime settings before Begin.");
            if (this.runtimeSettings != null) throw new InvalidOperationException("Runtime settings are already configured.");
            this.runtimeSettings = runtimeSettings ?? throw new ArgumentNullException(nameof(runtimeSettings));
        }

        /// <summary>초기화 전에 저장소를 주입합니다. 테스트는 실제 사용자 파일과 분리된 경로를 사용합니다.</summary>
        /// <remarks>
        /// Begin 전에는 저장소를 교체할 수 있으며 Editor Main 직접 Play도 이 경로로 격리 저장소를 설정합니다.
        /// ITextFileStore는 해제 계약이 없으므로 외부 자원을 가진 저장소의 수명은 주입자가 관리합니다.
        /// </remarks>
        public void ConfigureStorage(ITextFileStore fileStore)
        {
            if (State != AppState.NotStarted) throw new InvalidOperationException("Configure storage before Begin.");
            this.fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        }

        /// <summary>Boot 시작 전에 게임별 저장 버전·초기값·검증 규칙을 연결합니다.</summary>
        public void ConfigureGamePayload(GamePayloadPolicy policy)
        {
            if (State != AppState.NotStarted) throw new InvalidOperationException("Configure game payload before Begin.");
            if (gamePayloadPolicy != null) throw new InvalidOperationException("Game payload policy is already configured.");
            gamePayloadPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        /// <summary>
        /// Enter Play Mode에서 도메인 재로드가 꺼져 있어도 이전 세션의 정적 참조가 남지 않게 초기화합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        /// <summary>
        /// 첫 인스턴스를 싱글 인스턴스로 등록해 씬 전환에서 보존하고, 중복 인스턴스는 제거합니다.
        /// GameObject 파괴 토큰을 이후 비동기 작업의 수명 토큰으로 사용합니다.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            appLifetime = new AsyncLifetime(destroyCancellationToken);
            lifetimeToken = appLifetime.Token;
            gameState.StateChanged += OnGameStateChanged;
            sceneFlow = new SceneFlow(this);
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 설정 검증과 앱 초기화를 한 번만 시작합니다. 이미 준비된 상태에서 다시 호출되면
        /// 중복 초기화 대신 Title 씬 복귀를 시도합니다.
        /// </summary>
        /// <param name="config">Boot 씬에 연결된 앱 설정 에셋입니다.</param>
        /// <returns>초기화 또는 Title 복귀 요청을 접수했으면 <see langword="true"/>입니다.</returns>
        public bool Begin(AppConfig config)
        {
            if (Instance != this || lifetimeToken.IsCancellationRequested)
                return false;
            if (State == AppState.Ready)
                return TryReturnToTitle();
            if (State != AppState.NotStarted)
                return false;

            State = AppState.Initializing;
            Initialize(config);
            return true;
        }

        /// <summary>
        /// 프레임 경계를 두고 설정을 검증한 뒤 씬 경로를 스냅샷하고 Ready 상태를 공개합니다.
        /// Unity 이벤트에서 시작되는 async-void 경계이므로 모든 예외를 내부에서 실패 상태로 변환합니다.
        /// </summary>
        /// <param name="config">검증하고 실행 경로를 복사할 앱 설정입니다.</param>
        private async void Initialize(AppConfig config)
        {
            try
            {
                CurrentStep = "Checking startup configuration";
                StateChanged?.Invoke();
                await Awaitable.NextFrameAsync(lifetimeToken);
                var ownedRuntimeSettings = runtimeSettings;
                runtimeSettings = null;
                services = AppBootstrapper.Initialize(config, fileStore, gamePayloadPolicy, ownedRuntimeSettings, step =>
                {
                    CurrentStep = step;
                    StateChanged?.Invoke();
                }, lifetimeToken, transform);
                bootScenePath = services.Config.BootScene;
                titleScenePath = services.Config.TitleScene;
                mainScenePath = services.Config.MainScene;
                StorageMessage = string.Join("\n", new[] { Settings.Message, Game.Message }).Trim();
                Settings.Changed += OnSettingsChanged;
                CurrentStep = "Preparing scene navigation";
                StateChanged?.Invoke();
                await Awaitable.NextFrameAsync(lifetimeToken);

                if (startupDestination == AppStartupDestination.NewGameInMain)
                {
                    Game.StartNew(CreateInitialPayload()); Data.BeginSession(Game.Current);
                    StorageMessage = Game.Message;
                }
                await sceneFlow.InitializeActiveSceneAsync(lifetimeToken);
                lifetimeToken.ThrowIfCancellationRequested();
                State = AppState.Ready;
                CurrentStep = "Ready";
                if (startupDestination == AppStartupDestination.NewGameInMain)
                {
                    if (!TryNavigate(mainScenePath))
                    {
                        if (SceneManager.GetActiveScene().path != mainScenePath)
                            throw new InvalidOperationException("Could not enter the configured Main scene after startup.");
                        gameState.TryTransitionTo(GameState.Gameplay);
                        StateChanged?.Invoke();
                    }
                }
                else if (!TryReturnToTitle())
                {
                    // Title 자체에서 초기화한 경우에도 실행 상태를 공개합니다.
                    gameState.TryTransitionTo(GameState.Menu);
                    StateChanged?.Invoke();
                }
            }
            catch (OperationCanceledException) when (lifetimeToken.IsCancellationRequested) { }
            catch (Exception exception) { Fail(exception); }
        }

        /// <summary>Ready 상태일 때 Main 씬 전환을 요청합니다.</summary>
        /// <returns>전환을 시작했으면 <see langword="true"/>입니다.</returns>
        public bool TryEnterMain() => TryStartNewGame();

        public bool TryStartNewGame()
        {
            if (!CanStartGame()) return false;
            try { Game.StartNew(CreateInitialPayload()); Data.BeginSession(Game.Current); }
            catch (Exception exception) { Fail(exception); return false; }
            StorageMessage = Game.Message;
            return TryNavigate(mainScenePath);
        }

        /// <summary>Title에서 비어 있는 슬롯을 골라 새 진행을 시작합니다.</summary>
        public bool TryStartNewGame(int slotId)
        {
            if (!CanStartGame() || !CheckSlotId(slotId)) return false;
            Game.RefreshSave();
            if (!Game.GetSlot(slotId).IsEmpty)
                return RejectSlotRequest("Choose an empty slot or delete an existing save in Slot Management.");
            try { Game.StartNew(slotId, CreateInitialPayload()); Data.BeginSession(Game.Current); }
            catch (Exception exception) { Fail(exception); return false; }
            StorageMessage = Game.Message;
            return TryNavigate(mainScenePath);
        }

        private bool CheckSlotId(int slotId)
        {
            if (Game == null) return false;
            return slotId >= 1 && slotId <= Game.SlotCount || RejectSlotRequest("Choose a valid save slot.");
        }
        private bool RejectSlotRequest(string message)
        {
            StorageMessage = message;
            StateChanged?.Invoke();
            return false;
        }

        /// <summary>Title 메뉴에서 선택할 슬롯 상태를 다시 읽습니다.</summary>
        public bool TryRefreshGameSlots()
        {
            if (!CanStartGame()) return false;
            Game.RefreshSave();
            StorageMessage = Game.Message;
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>Title에서만 삭제를 허용합니다. 진행 중인 세션이 있으면 먼저 종료해야 합니다.</summary>
        public bool TryDeleteGameSlot(int slotId)
        {
            if (!CanStartGame() || !CheckSlotId(slotId)) return false;
            if (Game.Current != null) return RejectSlotRequest("Return to the main menu before deleting a save slot.");
            var deleted = Game.TryDelete(slotId);
            StorageMessage = Game.Message;
            StateChanged?.Invoke();
            return deleted;
        }

        public bool TryRecoverGameBackup(int slotId)
        {
            if (!CanStartGame() || !CheckSlotId(slotId)) return false;
            if (Game.Current != null) return RejectSlotRequest("End the current game before recovering a slot.");
            var recovered = Game.TryRecoverBackup(slotId);
            StorageMessage = Game.Message;
            StateChanged?.Invoke();
            return recovered;
        }

        private string CreateInitialPayload() => gamePayloadPolicy != null
            ? gamePayloadPolicy.CreateInitialPayload() : "{}";

        /// <summary>기존 단일 슬롯 호출은 슬롯 1을 이어갑니다. 새 메뉴는 슬롯 인자를 사용합니다.</summary>
        public bool TryContinueGame() => TryContinueGame(1);
        public bool TryContinueGame(int slotId)
        {
            if (!CanStartGame() || !CheckSlotId(slotId)) return false;
            var isLoaded = Game.TryContinue(slotId);
            StorageMessage = Game.Message;
            if (isLoaded)
            {
                Data.BeginSession(Game.Current);
                return TryNavigate(mainScenePath);
            }
            StateChanged?.Invoke();
            return false;
        }

        private bool CanStartGame() => Instance == this && State == AppState.Ready && !IsTransitioning
            && !gameState.IsNotifying && !lifetimeToken.IsCancellationRequested
            && SceneManager.GetActiveScene().path == titleScenePath;

        /// <summary>
        /// 활성 Gameplay에서 Pause ↔ Gameplay 상태만 요청할 수 있습니다.
        /// 시간·입력·UI는 변경하지 않습니다. Boot/Menu/Loading/Failed는 기존 앱 흐름이 소유합니다.
        /// </summary>
        public bool TryChangeGameState(GameState next)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning || gameState.IsNotifying
                || lifetimeToken.IsCancellationRequested || Game?.Current == null)
                return false;
            if (next != GameState.Gameplay && next != GameState.Pause) return false;
            if (CurrentGameState != GameState.Gameplay && CurrentGameState != GameState.Pause) return false;
            if (!gameState.TryTransitionTo(next)) return false;
            if (!lifetimeToken.IsCancellationRequested) StateChanged?.Invoke();
            return true;
        }

        public bool TrySaveGame(bool replaceExisting = false) => TrySaveGame(null, replaceExisting);

        /// <summary>활성 게임 세션의 JSON 객체를 저장합니다. 추가 Gameplay 씬에서도 사용할 수 있으며 null이면 현재 payload를 다시 저장합니다.</summary>
        public bool TrySaveGame(string payloadJson, bool replaceExisting = false)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning || gameState.IsNotifying
                || lifetimeToken.IsCancellationRequested || Game?.Current == null) return false;
            var activeScenePath = SceneManager.GetActiveScene().path;
            if (activeScenePath == bootScenePath || activeScenePath == titleScenePath) return false;
            var capturedPayload = payloadJson ?? (Data.Runtime?.SessionId == Game.Current.SessionId
                ? Data.Runtime.SnapshotPayload() : Game.Current.PayloadJson);
            var isSaved = Game.TrySave(capturedPayload, replaceExisting);
            StarterLog.Info(LogCategory.Storage, $"Save completed: {isSaved}; slot: {Game.CurrentSlotId}", this);
            if (isSaved) Data.AcceptSavedSnapshot(Game.Current, replacePayload: payloadJson != null);
            StorageMessage = Game.Message;
            StateChanged?.Invoke();
            return isSaved;
        }

        public bool TrySaveSettings(UserSettings settings)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning || gameState.IsNotifying) return false;
            if (settings == null || !Settings.CanSave)
            {
                var accepted = Settings.TryApplyAndSave(settings);
                StorageMessage = Settings.Message;
                StateChanged?.Invoke();
                return accepted && State == AppState.Ready;
            }

            try { settings.Validate(); }
            catch (ArgumentException exception)
            {
                StorageMessage = exception.Message;
                StateChanged?.Invoke();
                return false;
            }

            var previous = Settings.Current;
            // 플랫폼 적용 실패가 디스크에 새 값을 남기지 않도록 먼저 적용한다.
            try { services.RuntimeSettings.Apply(settings); }
            catch (Exception exception) { Fail(exception); return false; }

            bool isSaved;
            // 서비스 알림은 외부 Reload·복구에도 쓰인다. 이 저장 호출에서만 중복 적용을 생략한다.
            isCommittingSettings = true;
            try { isSaved = Settings.TryApplyAndSave(settings); }
            finally { isCommittingSettings = false; }
            StorageMessage = Settings.Message;
            if (!isSaved)
            {
                try { services.RuntimeSettings.Apply(previous); }
                catch (Exception exception) { Fail(exception); return false; }
            }
            StateChanged?.Invoke();
            return isSaved && State == AppState.Ready;
        }

        public bool TryRecoverBackup(bool recoverSettings)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning || gameState.IsNotifying) return false;
            var isRecovered = recoverSettings ? Settings.TryRecoverBackup() : Game.TryRecoverBackup();
            StorageMessage = recoverSettings ? Settings.Message : Game.Message;
            StateChanged?.Invoke();
            return isRecovered && State == AppState.Ready;
        }

        private void OnSettingsChanged()
        {
            if (isCommittingSettings) return;
            StorageMessage = Settings.Message;
            try { services.RuntimeSettings.Apply(Settings.Current); }
            catch (Exception exception) { Fail(exception); }
            StateChanged?.Invoke();
        }

        /// <summary>Ready 상태일 때 Title 씬 전환을 요청합니다.</summary>
        /// <returns>전환을 시작했으면 <see langword="true"/>입니다.</returns>
        public bool TryReturnToTitle() => TryNavigate(titleScenePath);

        /// <summary>
        /// 앱 상태, 중복 요청, 수명과 현재 씬을 확인하고 비동기 로드 전에 전환 잠금을 획득합니다.
        /// </summary>
        /// <param name="scenePath">이동할 씬의 프로젝트 상대 경로입니다.</param>
        /// <returns>씬 로드 요청을 새로 시작했으면 <see langword="true"/>입니다.</returns>
        private bool TryNavigate(string scenePath)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning || gameState.IsNotifying
                || lifetimeToken.IsCancellationRequested)
                return false;
            if (SceneManager.GetActiveScene().path == scenePath || !gameState.CanTransitionTo(GameState.Loading))
                return false;

            // Acquire before starting async work so repeated button presses cannot overlap.
            IsTransitioning = true;
            LoadingProgress = 0;
            CurrentStep = "Loading";
            gameState.TryTransitionTo(GameState.Loading);
            StateChanged?.Invoke();
            if (State != AppState.Ready || lifetimeToken.IsCancellationRequested)
            {
                IsTransitioning = false;
                return false;
            }
            Navigate(scenePath);
            return true;
        }

        /// <summary>
        /// 단일 모드로 씬을 비동기 로드합니다. Unity 씬 로드는 시작 후 취소할 수 없으므로
        /// 완료될 때까지 전환 잠금을 유지하고 오류는 실패 상태로 변환합니다.
        /// </summary>
        /// <param name="scenePath">빌드 씬 목록에 포함된 대상 씬 경로입니다.</param>
        private async void Navigate(string scenePath)
        {
            var sceneLoaded = false;
            try
            {
                CurrentStep = "Loading";
                await sceneFlow.NavigateAsync(scenePath, lifetimeToken, progress =>
                {
                    LoadingProgress = progress;
                    StateChanged?.Invoke();
                }, () =>
                {
                    if (scenePath != titleScenePath) return;
                    Game.EndSession();
                    Data.EndSession();
                    Game.RefreshSave();
                });
                lifetimeToken.ThrowIfCancellationRequested();
                sceneLoaded = true;
                if (State == AppState.Ready)
                {
                    LoadingProgress = 1;
                    CurrentStep = "Ready";
                }
            }
            catch (OperationCanceledException) when (lifetimeToken.IsCancellationRequested) { }
            catch (Exception exception) { Fail(exception); }
            finally
            {
                IsTransitioning = false;
                if (!lifetimeToken.IsCancellationRequested)
                {
                    if (sceneLoaded && State == AppState.Ready)
                        gameState.TryTransitionTo(scenePath == titleScenePath ? GameState.Menu : GameState.Gameplay);
                    StateChanged?.Invoke();
                }
            }
        }

        /// <summary>예외 정보를 공개 상태와 로그에 기록하고 추가 앱 진입을 차단합니다.</summary>
        /// <param name="exception">초기화 또는 씬 전환 중 발생한 예외입니다.</param>
        private void Fail(Exception exception)
        {
            State = AppState.Failed;
            Failure = exception.Message;
            CurrentStep = "Startup failed";
            gameState.TryTransitionTo(GameState.Failed);
            appLifetime?.Cancel();
            sceneFlow?.Dispose();
            ReleaseRuntimeSettings();
            StateChanged?.Invoke();
            StarterLog.Error(LogCategory.App, Failure, this);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            StarterLog.Info(LogCategory.App, $"GameState: {previous} -> {current}", this);
            if (services != null && !services.IsDisposed)
            {
                Pause.Apply(current == GameState.Pause);
                Audio.SetPaused(current == GameState.Pause);
                if (current == GameState.Loading) Audio.OnSceneExit();
                var context = current == GameState.Menu ? InputContext.UI : CurrentSceneRoot?.InitialInputContext ?? InputContext.Gameplay;
                Input.SetState(context, current == GameState.Boot || current == GameState.Loading || current == GameState.Failed
                    ? InputContext.None : current == GameState.Pause ? InputContext.UI : (InputContext?)null);
            }
            var subscribers = GameStateChanged;
            if (subscribers == null) return;
            foreach (Action<GameState, GameState> subscriber in subscribers.GetInvocationList())
            {
                if (this == null || (lifetimeToken.IsCancellationRequested && current != GameState.Failed) || gameState.Current != current) break;
                try { subscriber(previous, current); }
                catch (Exception exception)
                {
                    // 표시용 소비자의 오류가 씬 전환이나 나머지 알림을 중단하지 않게 합니다.
                    StarterLog.Warning(LogCategory.App, $"GameState observer failed: {exception.Message}", this);
                }
            }
        }

        private void ReleaseRuntimeSettings()
        {
            if (Settings != null) Settings.Changed -= OnSettingsChanged;
            var ownedServices = services;
            var ownedSettings = runtimeSettings;
            runtimeSettings = null;
            try { ownedServices?.Dispose(); }
            catch (Exception error) { StarterLog.Warning(LogCategory.App, $"Service cleanup failed: {error.Message}", this); }
            try { ownedSettings?.Dispose(); }
            catch (Exception exception)
            {
                StarterLog.Warning(LogCategory.App, $"Could not restore runtime settings: {exception.Message}", this);
            }
        }

        /// <summary>
        /// 현재 인스턴스가 파괴될 때 정적 참조를 해제합니다. 이후 서비스가 추가되면
        /// 이 지점에서 초기화의 역순으로 소유 자원을 정리해야 합니다.
        /// </summary>
        private void OnDestroy()
        {
            appLifetime?.Cancel();
            sceneFlow?.Dispose();
            ReleaseRuntimeSettings();
            if (Instance != this)
                return;
            gameState.StateChanged -= OnGameStateChanged;
            GameStateChanged = null;
            StateChanged = null;
            Instance = null;


            fileStore = null;
            services = null;
            appLifetime?.Dispose();
        }
    }
}
