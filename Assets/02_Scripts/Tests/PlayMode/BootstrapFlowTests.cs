using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarterProject.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace StarterProject.Tests
{
    /// <summary>
    /// Boot에서 Title과 Main으로 이어지는 실제 씬 흐름, 실패 차단, 중복 방지와
    /// <see cref="AppRoot"/> 수명을 PlayMode에서 검증하는 통합 테스트 모음입니다.
    /// </summary>
    public sealed class BootstrapFlowTests
    {
        private string testDirectory;
        private const string Boot = "Assets/01_Scenes/Boot/00_StartScene.unity";
        private const string Title = "Assets/01_Scenes/Main/01_Title.unity";
        private const string Main = "Assets/01_Scenes/Main/02_MainScene.unity";

        /// <summary>각 테스트를 독립시키기 위해 기존 AppRoot를 제거하고 Main 씬에서 시작합니다.</summary>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            testDirectory = Path.Combine(Application.temporaryCachePath, "StarterTests-" + Guid.NewGuid().ToString("N"));
            if (AppRoot.Instance != null)
                Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            // A single scene load preserves Unity's DontSave test runner. Explicitly
            // unloading every scene can also unload the runner and stall the test job.
            yield return SceneManager.LoadSceneAsync(Main);
            yield return null;
        }

        /// <summary>테스트가 만든 영속 AppRoot를 제거해 다음 테스트로 상태가 새지 않게 합니다.</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (AppRoot.Instance != null)
                Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }

        /// <summary>전체 왕복 흐름과 연타 차단, 공통 루트·씬 UI의 단일 인스턴스를 검증합니다.</summary>
        [UnityTest]
        public IEnumerator Boot_Title_Main_Return_PreservesRootAndRejectsRepeatedClicks()
        {
            yield return LoadBoot();
            yield return WaitForScene(Title);
            var root = AppRoot.Instance;
            Assert.That(root.State, Is.EqualTo(AppState.Ready));
            Assert.That(Object.FindObjectsByType<AppRoot>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var button = FindActionButton();
            Assert.That(button.interactable, Is.True);
            Assert.That(EventSystem.current, Is.Not.Null);
            Assert.That(EventSystem.current.currentInputModule, Is.Not.Null);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(root.IsTransitioning, Is.True);
            Assert.That(root.TryEnterMain(), Is.False);
            Assert.That(root.TryReturnToTitle(), Is.False);
            yield return WaitForScene(Main);
            Assert.That(AppRoot.Instance, Is.SameAs(root));
            Assert.That(Object.FindObjectsByType<StarterScreen>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            FindActionButton().onClick.Invoke();
            yield return WaitForScene(Title);
            Assert.That(AppRoot.Instance, Is.SameAs(root));
        }

        [UnityTest]
        public IEnumerator ConfiguredMainStartupUsesBootAndFreshSession()
        {
            var root = CreateRoot("DirectMainRoot");
            root.ConfigureStartupDestination(AppStartupDestination.NewGameInMain);
            var visitedTitle = false;
            void OnSceneChanged(Scene previous, Scene current) => visitedTitle |= current.path == Title;
            SceneManager.activeSceneChanged += OnSceneChanged;
            try
            {
                yield return LoadBoot();
                yield return WaitForScene(Main);
                Assert.That(visitedTitle, Is.False);
                Assert.That(AppRoot.Instance, Is.SameAs(root));
                Assert.That(root.State, Is.EqualTo(AppState.Ready));
                Assert.That(root.Game.Current, Is.Not.Null);
                Assert.That(root.Game.CanContinue, Is.False);
                Assert.That(File.Exists(Path.Combine(testDirectory, GameSessionService.FileName)), Is.False);
                Assert.That(root.TrySaveGame(), Is.True, root.StorageMessage);
                Assert.That(File.Exists(Path.Combine(testDirectory, GameSessionService.FileName)), Is.True);
                Assert.That(root.TryStartNewGame(), Is.False);
            }
            finally { SceneManager.activeSceneChanged -= OnSceneChanged; }
        }

        [UnityTest]
        public IEnumerator GamePayloadPolicySupportsGameDataWithoutChangingCore()
        {
            var policy = ScriptableObject.CreateInstance<TestStagePayloadPolicy>();
            var root = CreateRoot("PayloadRoot");
            root.ConfigureGamePayload(policy);
            yield return LoadBoot();
            yield return WaitForScene(Title);
            Assert.That(root.TryStartNewGame(), Is.True);
            yield return WaitForScene(Main);
            Assert.That(root.Game.Current.PayloadJson, Does.Contain("\"stage\":1"));
            Assert.That(root.TrySaveGame("{\"stage\":3}"), Is.True, root.StorageMessage);
            var savePath = Path.Combine(testDirectory, GameSessionService.FileName);
            var saved = File.ReadAllText(savePath);
            Assert.That(saved, Does.Contain("\"payloadVersion\": 2"));
            Assert.That(root.TrySaveGame("{\"other\":4}"), Is.False);
            Assert.That(File.ReadAllText(savePath), Is.EqualTo(saved));
            Assert.That(root.TryReturnToTitle(), Is.True);
            yield return WaitForScene(Title);
            Assert.That(root.TryContinueGame(), Is.True);
            yield return WaitForScene(Main);
            Assert.That(root.Game.Current.PayloadJson, Does.Contain("\"stage\": 3"));
            Object.Destroy(policy);
        }

        /// <summary>Ready 이후 Boot 재진입이 새 루트를 만들지 않고 Title 복귀로 처리되는지 검증합니다.</summary>
        [UnityTest]
        public IEnumerator ReturningToBootReusesReadyRoot()
        {
            yield return LoadBoot();
            yield return WaitForScene(Title);
            var root = AppRoot.Instance;
            yield return LoadBoot();
            yield return WaitForScene(Title);
            Assert.That(AppRoot.Instance, Is.SameAs(root));
            Assert.That(Object.FindObjectsByType<AppRoot>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        /// <summary>AppConfig 누락이 Failed로 전환되고 다른 씬 진입을 차단하는지 검증합니다.</summary>
        [UnityTest]
        public IEnumerator MissingConfigFailsAndDoesNotEnterAnotherScene()
        {
            var originalScene = SceneManager.GetActiveScene();
            var root = CreateRoot("MissingConfigRoot");
            LogAssert.Expect(LogType.Error, "[Starter Project] AppBootstrap requires an AppConfig asset.");
            Assert.That(root.Begin(null), Is.True);
            Assert.That(root.State, Is.EqualTo(AppState.Initializing));
            Assert.That(root.TryEnterMain(), Is.False);
            Assert.That(root.Begin(null), Is.False);
            yield return WaitFor(() => root.State == AppState.Failed);
            Assert.That(root.Failure, Does.Contain("AppConfig"));
            Assert.That(root.TryEnterMain(), Is.False);
            Assert.That(root.Begin(null), Is.False);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(originalScene));
        }

        /// <summary>빌드 목록에 없는 Main 설정이 Title 진입 전에 실패하는지 검증합니다.</summary>
        [UnityTest]
        public IEnumerator MissingMainSceneFailsBeforeTitle()
        {
            var originalScene = SceneManager.GetActiveScene();
            var config = ScriptableObject.CreateInstance<AppConfig>();
            JsonUtility.FromJsonOverwrite("{\"mainScene\":\"Assets/Missing.unity\"}", config);
            var root = CreateRoot("InvalidConfigRoot");
            LogAssert.Expect(LogType.Error, "[Starter Project] Main scene is missing or disabled in the build scene list: Assets/Missing.unity");
            root.Begin(config);
            yield return WaitFor(() => root.State == AppState.Failed);
            Assert.That(root.TryEnterMain(), Is.False);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(originalScene));
            Object.Destroy(config);
        }

        /// <summary>중복 AppRoot가 시작할 수 없고 원본 파괴가 진행 중 초기화를 취소하는지 검증합니다.</summary>
        [UnityTest]
        public IEnumerator DuplicateRootCannotStartAndDestroyingRootCancelsInitialization()
        {
            var config = ScriptableObject.CreateInstance<AppConfig>();
            var root = CreateRoot("Root");
            var duplicate = CreateRoot("Duplicate");
            Assert.That(duplicate.Begin(config), Is.False);
            root.Begin(config);
            Object.Destroy(root.gameObject);
            yield return null;
            yield return null;
            Assert.That(AppRoot.Instance, Is.Null);
            Assert.That(Object.FindObjectsByType<AppRoot>(FindObjectsSortMode.None), Is.Empty);
            Object.Destroy(config);
        }

        /// <summary>Main을 직접 Play할 때 시작 버튼이 잠기고 Boot 안내가 표시되는지 검증합니다.</summary>
        [UnityTest]
        public IEnumerator MainOpenedWithoutBootCannotStartGame()
        {
            yield return SceneManager.LoadSceneAsync(Main);
            yield return null;
            Assert.That(AppRoot.Instance, Is.Null);
            Assert.That(FindActionButton().interactable, Is.False);
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("00_StartScene")), Is.True);
        }

        /// <summary>실패한 영속 루트가 Boot 화면에 원인을 표시하고 Boot에 머무르는지 검증합니다.</summary>
        [UnityTest]
        public IEnumerator BootDisplaysFailureAndStaysOnBoot()
        {
            var root = CreateRoot("FailedRoot");
            LogAssert.Expect(LogType.Error, "[Starter Project] AppBootstrap requires an AppConfig asset.");
            root.Begin(null);
            yield return WaitFor(() => root.State == AppState.Failed);
            yield return LoadBoot();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Boot));
            Assert.That(AppRoot.Instance, Is.SameAs(root));
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Any(t => t.text.Contains("Could not start") && t.text.Contains("AppConfig")), Is.True);
        }

        /// <summary>검증 이후 SO가 변경되어도 이미 검증한 실행 경로를 사용하는지 확인합니다.</summary>
        [UnityTest]
        public IEnumerator ConfigurationChangesAfterValidationDoNotChangeRuntimeRoutes()
        {
            var config = ScriptableObject.CreateInstance<AppConfig>();
            try
            {
                var root = CreateRoot("SnapshotRoot");
                root.Begin(config);
                yield return WaitFor(() => root.CurrentStep == "Preparing scene navigation");
                JsonUtility.FromJsonOverwrite("{\"mainScene\":\"Assets/Missing.unity\"}", config);
                yield return WaitForScene(Title);
                Assert.That(root.TryEnterMain(), Is.True);
                yield return WaitForScene(Main);
                Assert.That(root.State, Is.EqualTo(AppState.Ready));
            }
            finally { Object.DestroyImmediate(config); }
        }

        /// <summary>같은 화면에서 루트 생성·실패·제거와 화면 재활성화가 표시 상태에 반영되는지 확인합니다.</summary>
        [UnityTest]
        public IEnumerator ScreenRefreshesWhenRootChangesAndAfterReenable()
        {
            var view = Object.FindFirstObjectByType<StarterScreen>();
            var button = FindActionButton();
            var root = CreateRoot("ScreenTestRoot");
            LogAssert.Expect(LogType.Error, "[Starter Project] AppBootstrap requires an AppConfig asset.");
            root.Begin(null);
            yield return WaitFor(() => root.State == AppState.Failed);
            yield return null;
            Assert.That(view.GetComponentsInChildren<Text>().Any(t => t.text.Contains("Could not start")), Is.True);
            view.enabled = false;
            Object.Destroy(root.gameObject);
            yield return null;
            view.enabled = true;
            Assert.That(button.interactable, Is.False);
            Assert.That(view.GetComponentsInChildren<Text>().Any(t => t.text.Contains("00_StartScene")), Is.True);
        }

        /// <summary>씬 역할 중복과 잘못된 첫 빌드 씬을 AppConfig가 거부하는지 검증합니다.</summary>
        [Test]
        public void ConfigRejectsDuplicateRolesAndWrongBootOrder()
        {
            var config = ScriptableObject.CreateInstance<AppConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"mainScene\":\"" + Title + "\"}", config);
                Assert.That(() => config.Validate(), Throws.InvalidOperationException.With.Message.Contains("different scenes"));
                JsonUtility.FromJsonOverwrite("{\"bootScene\":\"" + Main + "\",\"mainScene\":\"" + Boot + "\"}", config);
                Assert.That(() => config.Validate(), Throws.InvalidOperationException.With.Message.Contains("first enabled scene"));
            }
            finally { Object.DestroyImmediate(config); }
        }

        [UnityTest]
        public IEnumerator SettingsAndSavedGameSurviveAppRootRecreation()
        {
            yield return LoadBoot();
            yield return WaitForScene(Title);
            var root = AppRoot.Instance;
            Assert.That(root.TrySaveSettings(new UserSettings { masterVolume = 0, fullscreen = false, language = "ko" }), Is.True);
            Assert.That(root.TryContinueGame(), Is.False);
            Assert.That(root.TryStartNewGame(), Is.True);
            Assert.That(root.TryStartNewGame(), Is.False);
            yield return WaitForScene(Main);
            var session = root.Game.Current.SessionId;
            Assert.That(root.TrySaveGame(), Is.True, root.StorageMessage);
            Assert.That(root.TryReturnToTitle(), Is.True);
            yield return WaitForScene(Title);
            Assert.That(root.Game.Current, Is.Null);
            Assert.That(GameObject.Find("Secondary Action").GetComponent<Button>().interactable, Is.True);
            Object.Destroy(root.gameObject);
            yield return null;
            yield return LoadBoot();
            yield return WaitForScene(Title);
            root = AppRoot.Instance;
            Assert.That(root.Settings.Current.masterVolume, Is.Zero);
            Assert.That(root.Settings.Current.fullscreen, Is.False);
            Assert.That(root.Settings.Current.language, Is.EqualTo("ko"));
            GameObject.Find("Secondary Action").GetComponent<Button>().onClick.Invoke();
            Assert.That(root.TryContinueGame(), Is.False);
            yield return WaitForScene(Main);
            Assert.That(root.Game.Current.SessionId, Is.EqualTo(session));
        }

        [UnityTest]
        public IEnumerator SaveButtonRequiresConfirmationBeforeReplacingAnotherSession()
        {
            yield return LoadBoot();
            yield return WaitForScene(Title);
            var root = AppRoot.Instance;
            root.TryStartNewGame();
            yield return WaitForScene(Main);
            Assert.That(root.TrySaveGame(), Is.True);
            var original = File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName));
            root.TryReturnToTitle();
            yield return WaitForScene(Title);
            root.TryStartNewGame();
            yield return WaitForScene(Main);
            var save = GameObject.Find("Secondary Action").GetComponent<Button>();
            save.onClick.Invoke();
            Assert.That(save.GetComponentInChildren<Text>().text, Is.EqualTo("Confirm Replace Save"));
            Assert.That(File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName)), Is.EqualTo(original));
            GameObject.Find("Cancel Replace").GetComponent<Button>().onClick.Invoke();
            Assert.That(save.GetComponentInChildren<Text>().text, Is.EqualTo("Save Game"));
            save.onClick.Invoke();
            save.onClick.Invoke();
            Assert.That(File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName)), Is.Not.EqualTo(original));
            Assert.That(File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName + ".bak")), Is.EqualTo(original));
        }

        [UnityTest]
        public IEnumerator CorruptSettingsAndFutureSaveReachTitleWithProtectedFiles()
        {
            Directory.CreateDirectory(testDirectory);
            File.WriteAllText(Path.Combine(testDirectory, SettingsService.FileName), "broken");
            File.WriteAllText(Path.Combine(testDirectory, GameSessionService.FileName), "{\"schemaVersion\":999}");
            yield return LoadBoot();
            yield return WaitForScene(Title);
            var root = AppRoot.Instance;
            Assert.That(root.State, Is.EqualTo(AppState.Ready));
            Assert.That(root.Settings.Status, Is.EqualTo(StorageStatus.Invalid));
            Assert.That(root.Game.Status, Is.EqualTo(StorageStatus.UnsupportedVersion));
            Assert.That(GameObject.Find("Secondary Action").GetComponent<Button>().interactable, Is.False);
            Assert.That(root.TryContinueGame(), Is.False);
            root.TryStartNewGame();
            yield return WaitForScene(Main);
            Assert.That(root.TrySaveGame(true), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName)), Is.EqualTo("{\"schemaVersion\":999}"));
        }

        [UnityTest]
        public IEnumerator SettingsButtonsPersistValidZeroAndFalse()
        {
            yield return LoadBoot();
            yield return WaitForScene(Title);
            GameObject.Find("Volume").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Fullscreen").GetComponent<Button>().onClick.Invoke();
            Assert.That(AppRoot.Instance.Settings.Current.masterVolume, Is.Zero);
            Assert.That(AppRoot.Instance.Settings.Current.fullscreen, Is.False);
            var restart = new SettingsService(new UserSettings(), new JsonFileStore(testDirectory));
            Assert.That(restart.Current.masterVolume, Is.Zero);
            Assert.That(restart.Current.fullscreen, Is.False);
        }

        private AppRoot CreateRoot(string name)
        {
            var root = new GameObject(name).AddComponent<AppRoot>();
            root.ConfigureStorage(new JsonFileStore(testDirectory));
            root.ConfigureRuntimeSettings(new NoOpRuntimeSettings());
            return root;
        }

        private sealed class NoOpRuntimeSettings : IRuntimeSettings
        {
            public void Apply(UserSettings settings) { }
            public void Dispose() { }
        }

        private IEnumerator LoadBoot()
        {
            if (AppRoot.Instance == null) CreateRoot("TestRoot");
            yield return SceneManager.LoadSceneAsync(Boot);
        }

        private static Button FindActionButton() => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .Single(b => b.name == "Start Game" || b.name == "Back to Title");

        /// <summary>대상 씬이 활성화되고 AppRoot의 전환 잠금이 풀릴 때까지 기다립니다.</summary>
        /// <param name="path">기다릴 씬의 프로젝트 상대 경로입니다.</param>
        private static IEnumerator WaitForScene(string path)
        {
            yield return WaitFor(() => SceneManager.GetActiveScene().path == path
                && AppRoot.Instance != null && !AppRoot.Instance.IsTransitioning);
            yield return null; // Let the newly loaded screen update its button state.
        }

        /// <summary>조건이 참이 될 때까지 최대 20초 동안 프레임 단위로 기다립니다.</summary>
        /// <param name="condition">완료 여부를 반환하는 조건입니다.</param>
        private static IEnumerator WaitFor(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 20f;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Timed out waiting for app flow.");
                yield return null;
            }
        }
    }

    public sealed class TestStagePayloadPolicy : GamePayloadPolicy
    {
        public override int PayloadVersion => 2;
        public override string CreateInitialPayload() => "{\"stage\":1}";
        public override void ValidatePayload(string payloadJson)
        {
            if (!payloadJson.Contains("\"stage\"))
                throw new InvalidDataException("A stage is required.");
        }
    }
}
