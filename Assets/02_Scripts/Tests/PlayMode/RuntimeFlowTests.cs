using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarterProject.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace StarterProject.Tests
{
    /// <summary>저장된 설정의 시스템 적용과 실제 UI 입력·로딩 연결을 검증합니다.</summary>
    public sealed class RuntimeFlowTests
    {
        private const string Boot = "Assets/01_Scenes/Boot/00_StartScene.unity";
        private const string Title = "Assets/01_Scenes/Main/01_Title.unity";
        private const string Main = "Assets/01_Scenes/Main/02_MainScene.unity";
        private string testDirectory;
        private JsonFileStore fileStore;
        private RecordingRuntimeSettings runtime;
        private readonly List<InputDevice> testDevices = new List<InputDevice>();
        private InputSettings inputSettings;
        private InputSettings.BackgroundBehavior originalBackgroundBehavior;
        private bool originalRunInBackground;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode originalEditorInputBehavior;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            testDirectory = Path.Combine(Application.temporaryCachePath, "StarterRuntimeTests-" + Guid.NewGuid().ToString("N"));
            fileStore = new JsonFileStore(testDirectory);
            runtime = new RecordingRuntimeSettings();
            inputSettings = InputSystem.settings;
            originalBackgroundBehavior = inputSettings.backgroundBehavior;
            originalRunInBackground = Application.runInBackground;
#if UNITY_EDITOR
            originalEditorInputBehavior = inputSettings.editorInputBehaviorInPlayMode;
            // Unity's InputTestFixture uses this route so Game View focus cannot
            // divert synthetic keyboard events into editor-only input updates.
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
            if (AppRoot.Instance != null) Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync(Main);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var device in testDevices)
                if (device.added) InputSystem.RemoveDevice(device);
            testDevices.Clear();
            if (AppRoot.Instance != null) Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            inputSettings.backgroundBehavior = originalBackgroundBehavior;
#if UNITY_EDITOR
            inputSettings.editorInputBehaviorInPlayMode = originalEditorInputBehavior;
#endif
            Application.runInBackground = originalRunInBackground;
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }

        [UnityTest]
        public IEnumerator StartupAppliesSavedSettingsBeforeReady()
        {
            var settings = new SettingsService(new UserSettings(), fileStore);
            Assert.That(settings.TryApplyAndSave(new UserSettings { masterVolume = 0.25f, fullscreen = false, language = "ko" }), Is.True);
            var root = CreateRoot();
            var appliedWhenReady = false;
            root.StateChanged += () =>
            {
                if (root.State == AppState.Ready) appliedWhenReady = runtime.Applied.Count > 0;
            };
            yield return LoadBoot();
            Assert.That(appliedWhenReady, Is.True);
            Assert.That(runtime.Applied.Count, Is.EqualTo(1));
            AssertSettings(runtime.Applied[0], 0.25f, false, "ko");
        }

        [UnityTest]
        public IEnumerator SuccessfulSaveAppliesImmediatelyAndKeepsIndependentSnapshot()
        {
            yield return LoadBoot();
            var requested = new UserSettings { masterVolume = 0, fullscreen = false, language = "ko" };
            Assert.That(AppRoot.Instance.TrySaveSettings(requested), Is.True);
            Assert.That(runtime.Applied.Count, Is.EqualTo(2));
            AssertSettings(runtime.Applied[1], 0, false, "ko");
            requested.masterVolume = 1;
            Assert.That(runtime.Applied[1].masterVolume, Is.Zero);
            Assert.That(new SettingsService(new UserSettings(), fileStore).Current.masterVolume, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FailedWritesAndProtectedVersionsRestorePreviousRuntimeSettings()
        {
            CreateRoot(new WriteFailureFileStore(fileStore));
            yield return LoadBoot();
            var original = AppRoot.Instance.Settings.Current;
            var requested = new UserSettings { masterVolume = 0.25f, fullscreen = false, language = "ko" };
            Assert.That(AppRoot.Instance.TrySaveSettings(requested), Is.False);
            Assert.That(runtime.Applied.Count, Is.EqualTo(3));
            AssertSettings(runtime.Applied.Last(), original.masterVolume, original.fullscreen, original.language);
            AssertSettings(AppRoot.Instance.Settings.Current, original.masterVolume, original.fullscreen, original.language);

            Directory.CreateDirectory(testDirectory);
            var protectedJson = "{\"schemaVersion\":999}";
            File.WriteAllText(Path.Combine(testDirectory, SettingsService.FileName), protectedJson);
            Assert.That(AppRoot.Instance.TrySaveSettings(requested), Is.False);
            Assert.That(runtime.Applied.Count, Is.EqualTo(5));
            AssertSettings(runtime.Applied.Last(), original.masterVolume, original.fullscreen, original.language);
            Assert.That(File.ReadAllText(Path.Combine(testDirectory, SettingsService.FileName)), Is.EqualTo(protectedJson));
        }

        [UnityTest]
        public IEnumerator RuntimeApplyFailureDoesNotPersistNewSettings()
        {
            yield return LoadBoot();
            var root = AppRoot.Instance;
            Assert.That(root.TrySaveSettings(new UserSettings { masterVolume = 0.75f, fullscreen = true, language = "en" }), Is.True);
            var settingsPath = Path.Combine(testDirectory, SettingsService.FileName);
            var previousFile = File.ReadAllText(settingsPath);
            runtime.FailApply = true;
            LogAssert.Expect(LogType.Error, "[Starter Project] Injected runtime apply failure.");

            Assert.That(root.TrySaveSettings(new UserSettings { masterVolume = 0.25f, fullscreen = false, language = "ko" }), Is.False);
            Assert.That(root.State, Is.EqualTo(AppState.Failed));
            Assert.That(File.ReadAllText(settingsPath), Is.EqualTo(previousFile));
            AssertSettings(root.Settings.Current, 0.75f, true, "en");
        }

        [UnityTest]
        public IEnumerator SettingsReloadAndBackupRecoveryApplyRecoveredValues()
        {
            var settings = new SettingsService(new UserSettings(), fileStore);
            Assert.That(settings.TryApplyAndSave(new UserSettings { masterVolume = 0.25f, fullscreen = false, language = "ko" }), Is.True);
            Assert.That(settings.TryApplyAndSave(new UserSettings { masterVolume = 0.75f, fullscreen = true, language = "en" }), Is.True);
            yield return LoadBoot();
            AssertSettings(runtime.Applied.Single(), 0.75f, true, "en");
            File.WriteAllText(Path.Combine(testDirectory, SettingsService.FileName), "damaged");
            AppRoot.Instance.Settings.Reload();
            Assert.That(AppRoot.Instance.Settings.Status, Is.EqualTo(StorageStatus.Recovered));
            Assert.That(runtime.Applied.Count, Is.EqualTo(2));
            AssertSettings(runtime.Applied[1], 0.25f, false, "ko");
            Assert.That(AppRoot.Instance.TryRecoverBackup(true), Is.True);
            Assert.That(runtime.Applied.Count, Is.EqualTo(3));
            AssertSettings(runtime.Applied[2], 0.25f, false, "ko");
            Assert.That(AppRoot.Instance.Settings.Status, Is.EqualTo(StorageStatus.Loaded));
            Assert.That(File.ReadAllText(Directory.GetFiles(testDirectory, "*.preserved-*").Single()), Is.EqualTo("damaged"));
        }

        [UnityTest]
        public IEnumerator DestroyingRootDisposesRuntimeAndUnsubscribesSettings()
        {
            yield return LoadBoot();
            var settings = AppRoot.Instance.Settings;
            Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            Assert.That(runtime.DisposeCount, Is.EqualTo(1));
            settings.Reload();
            Assert.That(runtime.Applied.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RuntimeApplyFailureDisposesOnceAndBlocksNavigationEvenIfCleanupFails()
        {
            runtime.FailApply = true;
            runtime.FailDispose = true;
            var root = CreateRoot();
            LogAssert.Expect(LogType.Warning, "[Starter Project] Could not restore runtime settings: Injected dispose failure.");
            LogAssert.Expect(LogType.Error, "[Starter Project] Injected runtime apply failure.");
            yield return SceneManager.LoadSceneAsync(Boot);
            var deadline = Time.realtimeSinceStartup + 20f;
            while (root.State != AppState.Failed)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Timed out waiting for runtime failure.");
                yield return null;
            }
            Assert.That(root.Failure, Is.EqualTo("Injected runtime apply failure."));
            Assert.That(runtime.DisposeCount, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Boot));
            Assert.That(root.TryStartNewGame(), Is.False);
            Assert.That(root.TryContinueGame(), Is.False);
            Assert.That(root.TryReturnToTitle(), Is.False);
            Assert.That(root.TrySaveSettings(new UserSettings()), Is.False);
            Assert.That(Object.FindObjectsByType<StarterLoadingOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single().IsVisible, Is.False);
            Object.Destroy(root.gameObject);
            yield return null;
            Assert.That(runtime.DisposeCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RootCreatedOnTitlePublishesReadyAndDuplicateReleasesOwnedRuntime()
        {
            yield return LoadBoot();
            Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            runtime = new RecordingRuntimeSettings();
            var root = CreateRoot();
            var readyWasPublished = false;
            root.StateChanged += () => readyWasPublished |= root.State == AppState.Ready && !root.IsTransitioning;
            var config = ScriptableObject.CreateInstance<AppConfig>();
            try
            {
                Assert.That(root.Begin(config), Is.True);
                var deadline = Time.realtimeSinceStartup + 20f;
                while (root.State != AppState.Ready)
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Timed out starting directly on Title.");
                    yield return null;
                }
                yield return null;
                Assert.That(readyWasPublished, Is.True);
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Title));
                Assert.That(root.IsTransitioning, Is.False);
                Assert.That(Object.FindObjectsByType<StarterLoadingOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single().IsVisible, Is.False);
                var duplicateRuntime = new RecordingRuntimeSettings();
                var duplicate = new GameObject("DuplicateRuntimeRoot").AddComponent<AppRoot>();
                duplicate.ConfigureStorage(fileStore);
                duplicate.ConfigureRuntimeSettings(duplicateRuntime);
                Assert.That(duplicate.Begin(config), Is.False);
                yield return null;
                Assert.That(duplicateRuntime.DisposeCount, Is.EqualTo(1));
                Assert.That(duplicateRuntime.Applied, Is.Empty);
                Assert.That(AppRoot.Instance, Is.SameAs(root));
                Assert.That(runtime.DisposeCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(config); }
        }

        [UnityTest]
        public IEnumerator CustomLoadingOverlayPrefabKeepsAssignedVisuals()
        {
            var root = CreateRoot();
            var prefabObject = new GameObject("Custom Loading", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            prefabObject.SetActive(false);
            var prefab = prefabObject.AddComponent<StarterLoadingOverlay>();
            var artwork = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artwork.transform.SetParent(prefabObject.transform, false);
            var track = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            track.transform.SetParent(prefabObject.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(track.transform, false);
            var message = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            message.transform.SetParent(prefabObject.transform, false);
            var percentage = new GameObject("Percentage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            percentage.transform.SetParent(prefabObject.transform, false);
            SetVisual("progressTrack", track);
            SetVisual("progressFill", fill);
            SetVisual("messageText", message);
            SetVisual("progressText", percentage);

            var overlay = StarterLoadingOverlay.EnsureCreated(root, prefab);
            Assert.That(overlay, Is.Not.SameAs(prefab));
            Assert.That(overlay.transform.parent, Is.EqualTo(root.transform));
            Assert.That(overlay.GetComponentsInChildren<Image>(true).Any(image => image.name == "Artwork"), Is.True);
            var blocker = overlay.GetComponentsInChildren<Image>(true).Single(image => image.name == "Input Blocker");
            Assert.That(blocker.raycastTarget, Is.True);
            Assert.That(blocker.color.a, Is.Zero);
            Assert.That(StarterLoadingOverlay.EnsureCreated(root), Is.SameAs(overlay));
            Object.Destroy(prefabObject);
            yield return null;

            void SetVisual(string fieldName, object value)
            {
                var field = typeof(StarterLoadingOverlay).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                field.SetValue(prefab, value);
            }
        }

        [UnityTest]
        public IEnumerator TransitionImmediatelyLocksButtonsAndKeepsOneLoadingOverlay()
        {
            yield return LoadBoot();
            var root = AppRoot.Instance;
            var overlay = Object.FindObjectsByType<StarterLoadingOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            Assert.That(overlay.IsVisible, Is.False);
            var startButton = GameObject.Find("Start Game").GetComponent<Button>();
            var progressSamples = new List<float>();
            root.StateChanged += () => progressSamples.Add(root.LoadingProgress);
            Assert.That(root.TryStartNewGame(), Is.True);
            Assert.That(startButton.interactable, Is.False, "Input must lock before the next frame.");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
            Assert.That(overlay.IsVisible, Is.True);
            Assert.That(overlay.Progress, Is.InRange(0f, 1f));
            Assert.That(root.TryStartNewGame(), Is.False);
            yield return WaitForScene(Main);
            Assert.That(overlay.IsVisible, Is.False);
            Assert.That(Object.FindObjectsByType<StarterLoadingOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(), Is.SameAs(overlay));
            Assert.That(progressSamples, Is.Not.Empty);
            Assert.That(progressSamples.All(progress => progress >= 0 && progress <= 1), Is.True);
            Assert.That(root.TryReturnToTitle(), Is.True);
            Assert.That(overlay.IsVisible, Is.True);
            yield return WaitForScene(Title);
            Assert.That(Object.FindObjectsByType<StarterLoadingOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(), Is.SameAs(overlay));
            Assert.That(overlay.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator KeyboardSubmitStartsSelectedNewGame()
        {
            yield return LoadBoot();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            testDevices.Add(keyboard);
            yield return null;
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.name, Is.EqualTo("Start Game"));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            yield return WaitForScene(Main);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Assert.That(AppRoot.Instance.Game.Current, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator GamepadSubmitStartsSelectedNewGame()
        {
            yield return LoadBoot();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            testDevices.Add(gamepad);
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Start Game"));
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            yield return WaitForScene(Main);
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            Assert.That(AppRoot.Instance.Game.Current, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ActiveGameSessionCanSaveOutsideMainAndContinue()
        {
            yield return LoadBoot();
            var root = AppRoot.Instance;
            Assert.That(root.TryStartNewGame(), Is.True);
            yield return WaitForScene(Main);
            var sessionId = root.Game.Current.SessionId;
            var gameplay = SceneManager.CreateScene("StarterTestGameplay-" + Guid.NewGuid().ToString("N"));
            Assert.That(SceneManager.SetActiveScene(gameplay), Is.True);
            yield return SceneManager.UnloadSceneAsync(Main);

            Assert.That(root.TrySaveGame("{\"marker\":\"other-scene\"}"), Is.True, root.StorageMessage);
            Assert.That(root.TryReturnToTitle(), Is.True);
            Assert.That(root.TrySaveGame(), Is.False, "Scene transitions must block saves.");
            yield return WaitForScene(Title);
            Assert.That(root.TrySaveGame(), Is.False, "Title must not save an ended game.");
            Assert.That(root.TryContinueGame(), Is.True);
            yield return WaitForScene(Main);
            Assert.That(root.Game.Current.SessionId, Is.EqualTo(sessionId));
            Assert.That(JsonUtility.FromJson<TestPayload>(root.Game.Current.PayloadJson).marker, Is.EqualTo("other-scene"));
        }

        [UnityTest]
        public IEnumerator TitleDoesNotSaveEvenWhenGameServiceHasSession()
        {
            yield return LoadBoot();
            var root = AppRoot.Instance;
            root.Game.StartNew();
            Assert.That(root.TrySaveGame(), Is.False);
            Assert.That(File.Exists(Path.Combine(testDirectory, GameSessionService.FileName)), Is.False);
        }

        [UnityTest]
        public IEnumerator EscapeCancelsSaveReplacementAndRestoresValidSelection()
        {
            yield return LoadBoot();
            var root = AppRoot.Instance;
            Assert.That(root.TryStartNewGame(), Is.True);
            yield return WaitForScene(Main);
            Assert.That(root.TrySaveGame(), Is.True);
            var originalSave = File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName));
            Assert.That(root.TryReturnToTitle(), Is.True);
            yield return WaitForScene(Title);
            Assert.That(root.TryStartNewGame(), Is.True);
            yield return WaitForScene(Main);

            var saveButton = GameObject.Find("Secondary Action").GetComponent<Button>();
            saveButton.onClick.Invoke();
            Assert.That(saveButton.GetComponentInChildren<Text>().text, Is.EqualTo("Confirm Replace Save"));
            var cancelButton = GameObject.Find("Cancel Replace").GetComponent<Button>();
            EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            testDevices.Add(keyboard);
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Assert.That(cancelButton.gameObject.activeSelf, Is.False);
            Assert.That(saveButton.GetComponentInChildren<Text>().text, Is.EqualTo("Save Game"));
            Assert.That(File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName)), Is.EqualTo(originalSave));
            var restoredSelection = EventSystem.current.currentSelectedGameObject;
            Assert.That(restoredSelection, Is.Not.Null);
            Assert.That(restoredSelection.activeInHierarchy, Is.True);
            Assert.That(restoredSelection.GetComponent<Button>().IsInteractable(), Is.True);
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(restoredSelection));
        }

        private AppRoot CreateRoot(ITextFileStore storage = null)
        {
            var root = new GameObject("RuntimeTestRoot").AddComponent<AppRoot>();
            root.ConfigureStorage(storage ?? fileStore);
            root.ConfigureRuntimeSettings(runtime);
            return root;
        }

        private IEnumerator LoadBoot()
        {
            if (AppRoot.Instance == null) CreateRoot();
            yield return SceneManager.LoadSceneAsync(Boot);
            yield return WaitForScene(Title);
        }

        private static IEnumerator WaitForScene(string path)
        {
            var deadline = Time.realtimeSinceStartup + 20f;
            while (SceneManager.GetActiveScene().path != path || AppRoot.Instance == null || AppRoot.Instance.IsTransitioning)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Timed out waiting for runtime flow.");
                yield return null;
            }
            yield return null;
        }

        private static void AssertSettings(UserSettings settings, float volume, bool fullscreen, string language)
        {
            Assert.That(settings.masterVolume, Is.EqualTo(volume));
            Assert.That(settings.fullscreen, Is.EqualTo(fullscreen));
            Assert.That(settings.language, Is.EqualTo(language));
        }

        [Serializable]
        private sealed class TestPayload
        {
            public string marker = "";
        }

        private sealed class RecordingRuntimeSettings : IRuntimeSettings
        {
            public readonly List<UserSettings> Applied = new List<UserSettings>();
            public int DisposeCount { get; private set; }
            public bool FailApply;
            public bool FailDispose;
            public void Apply(UserSettings settings)
            {
                if (FailApply) throw new InvalidOperationException("Injected runtime apply failure.");
                Applied.Add(settings.Copy());
            }
            public void Dispose()
            {
                DisposeCount++;
                if (FailDispose) throw new InvalidOperationException("Injected dispose failure.");
            }
        }

        private sealed class WriteFailureFileStore : ITextFileStore
        {
            private readonly ITextFileStore inner;
            public WriteFailureFileStore(ITextFileStore inner) => this.inner = inner;
            public string ReadAllText(string fileName) => inner.ReadAllText(fileName);
            public void WriteAllText(string fileName, string contents, Func<string, bool> validateContents, bool preserveOriginal)
                => throw new IOException("Injected write failure.");
        }
    }
}
