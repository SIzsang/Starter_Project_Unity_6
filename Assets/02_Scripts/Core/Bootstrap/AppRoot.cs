using System;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarterProject
{
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
        private string titleScenePath;
        private string mainScenePath;
        public SettingsService Settings { get; private set; }
        public GameSessionService Game { get; private set; }
        public string StorageMessage { get; private set; } = "";
        private ITextFileStore fileStore;
        private IRuntimeSettings runtimeSettings;

        /// <summary>Begin 전에 시스템 설정 적용 경계를 주입합니다. 루트가 수명을 소유합니다.</summary>
        public void ConfigureRuntimeSettings(IRuntimeSettings runtimeSettings)
        {
            if (State != AppState.NotStarted) throw new InvalidOperationException("Configure runtime settings before Begin.");
            if (this.runtimeSettings != null) throw new InvalidOperationException("Runtime settings are already configured.");
            this.runtimeSettings = runtimeSettings ?? throw new ArgumentNullException(nameof(runtimeSettings));
        }

        /// <summary>초기화 전에 저장소를 주입합니다. 테스트는 실제 사용자 파일과 분리된 경로를 사용합니다.</summary>
        public void ConfigureStorage(ITextFileStore fileStore)
        {
            if (State != AppState.NotStarted) throw new InvalidOperationException("Configure storage before Begin.");
            this.fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
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
            lifetimeToken = destroyCancellationToken;
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
                if (config == null)
                    throw new InvalidOperationException("AppBootstrap requires an AppConfig asset.");
                config.Validate();
                // 검증과 복사 사이에 프레임을 넘기지 않아, 검증한 값만 실행에 사용합니다.
                titleScenePath = config.TitleScene;
                mainScenePath = config.MainScene;
                var defaultSettings = config.CreateDefaultSettings();

                CurrentStep = "Loading user settings and save information";
                fileStore = fileStore ?? new JsonFileStore(Path.Combine(Application.persistentDataPath, "StarterData"));
                Settings = new SettingsService(defaultSettings, fileStore);
                Game = new GameSessionService(fileStore);
                StorageMessage = string.Join("\n", new[] { Settings.Message, Game.Message }).Trim();

                CurrentStep = "Applying audio and display settings";
                runtimeSettings = runtimeSettings ?? new UnityRuntimeSettings();
                runtimeSettings.Apply(Settings.Current);
                Settings.Changed += OnSettingsChanged;

                CurrentStep = "Preparing scene navigation";
                StateChanged?.Invoke();
                await Awaitable.NextFrameAsync(lifetimeToken);

                State = AppState.Ready;
                CurrentStep = "Ready";
                if (!TryReturnToTitle()) StateChanged?.Invoke();
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
            Game.StartNew();
            StorageMessage = Game.Message;
            return TryNavigate(mainScenePath);
        }

        public bool TryContinueGame()
        {
            if (!CanStartGame()) return false;
            var isLoaded = Game.TryContinue();
            StorageMessage = Game.Message;
            return isLoaded && TryNavigate(mainScenePath);
        }

        private bool CanStartGame() => Instance == this && State == AppState.Ready && !IsTransitioning
            && !lifetimeToken.IsCancellationRequested && SceneManager.GetActiveScene().path == titleScenePath;

        public bool TrySaveGame(bool replaceExisting = false)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning
                || SceneManager.GetActiveScene().path != mainScenePath) return false;
            var isSaved = Game.TrySave(replaceExisting: replaceExisting);
            StorageMessage = Game.Message;
            return isSaved;
        }

        public bool TrySaveSettings(UserSettings settings)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning) return false;
            var isSaved = Settings.TryApplyAndSave(settings);
            StorageMessage = Settings.Message;
            StateChanged?.Invoke();
            return isSaved && State == AppState.Ready;
        }

        public bool TryRecoverBackup(bool recoverSettings)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning) return false;
            var isRecovered = recoverSettings ? Settings.TryRecoverBackup() : Game.TryRecoverBackup();
            StorageMessage = recoverSettings ? Settings.Message : Game.Message;
            StateChanged?.Invoke();
            return isRecovered && State == AppState.Ready;
        }

        private void OnSettingsChanged()
        {
            StorageMessage = Settings.Message;
            try { runtimeSettings.Apply(Settings.Current); }
            catch (Exception exception) { Fail(exception); }
            StateChanged?.Invoke();
        }

        /// <summary>Ready 상태일 때 Title 씬 전환을 요청합니다.</summary>
        /// <returns>전환을 시작했으면 <see langword="true"/>입니다.</returns>
        public bool TryReturnToTitle()
        {
            if (!TryNavigate(titleScenePath)) return false;
            Game?.EndSession();
            Game?.RefreshSave();
            return true;
        }

        /// <summary>
        /// 앱 상태, 중복 요청, 수명과 현재 씬을 확인하고 비동기 로드 전에 전환 잠금을 획득합니다.
        /// </summary>
        /// <param name="scenePath">이동할 씬의 프로젝트 상대 경로입니다.</param>
        /// <returns>씬 로드 요청을 새로 시작했으면 <see langword="true"/>입니다.</returns>
        private bool TryNavigate(string scenePath)
        {
            if (Instance != this || State != AppState.Ready || IsTransitioning
                || lifetimeToken.IsCancellationRequested)
                return false;
            if (SceneManager.GetActiveScene().path == scenePath)
                return false;

            // Acquire before starting async work so repeated button presses cannot overlap.
            IsTransitioning = true;
            LoadingProgress = 0;
            CurrentStep = "Loading";
            StateChanged?.Invoke();
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
            try
            {
                CurrentStep = "Loading";
                if (string.IsNullOrEmpty(scenePath) || SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
                    throw new InvalidOperationException($"Cannot load a scene outside the build scene list: {scenePath}");
                var loadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                if (loadOperation == null)
                    throw new InvalidOperationException($"Scene loading could not start: {scenePath}");

                // Unity scene loads cannot be cancelled. Hold the gate until the load completes.
                while (!loadOperation.isDone)
                {
                    LoadingProgress = Mathf.Clamp01(loadOperation.progress);
                    if (!lifetimeToken.IsCancellationRequested) StateChanged?.Invoke();
                    await Awaitable.NextFrameAsync();
                }
                lifetimeToken.ThrowIfCancellationRequested();
                LoadingProgress = 1;
                CurrentStep = "Ready";
            }
            catch (OperationCanceledException) when (lifetimeToken.IsCancellationRequested) { }
            catch (Exception exception) { Fail(exception); }
            finally
            {
                IsTransitioning = false;
                if (!lifetimeToken.IsCancellationRequested) StateChanged?.Invoke();
            }
        }

        /// <summary>예외 정보를 공개 상태와 로그에 기록하고 추가 앱 진입을 차단합니다.</summary>
        /// <param name="exception">초기화 또는 씬 전환 중 발생한 예외입니다.</param>
        private void Fail(Exception exception)
        {
            State = AppState.Failed;
            Failure = exception.Message;
            CurrentStep = "Startup failed";
            ReleaseRuntimeSettings();
            StateChanged?.Invoke();
            Debug.LogError($"[Starter Project] {Failure}", this);
        }

        private void ReleaseRuntimeSettings()
        {
            if (Settings != null) Settings.Changed -= OnSettingsChanged;
            var ownedSettings = runtimeSettings;
            runtimeSettings = null;
            try { ownedSettings?.Dispose(); }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Starter Project] Could not restore runtime settings: {exception.Message}", this);
            }
        }

        /// <summary>
        /// 현재 인스턴스가 파괴될 때 정적 참조를 해제합니다. 이후 서비스가 추가되면
        /// 이 지점에서 초기화의 역순으로 소유 자원을 정리해야 합니다.
        /// </summary>
        private void OnDestroy()
        {
            ReleaseRuntimeSettings();
            if (Instance != this)
                return;
            StateChanged = null;
            Instance = null;
            Game = null;
            Settings = null;
            fileStore = null;
        }
    }
}
