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
            var startButton = GameObject.Find("New Game").GetComponent<Button>();
            startButton.onClick.Invoke();
            Assert.That(root.IsTransitioning, Is.False, "Opening New Game must only show the slot picker.");
            Assert.That(root.Game.Current, Is.Null, "A session starts only after choosing an empty slot.");
            Assert.That(Object.FindFirstObjectByType<StarterTitleMenu>().CurrentPage, Is.EqualTo(StarterTitlePage.NewGame));
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
            Assert.That(selected.name, Is.EqualTo("New Game"));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Title));
            Assert.That(AppRoot.Instance.Game.Current, Is.Null);
            Assert.That(Object.FindFirstObjectByType<StarterTitleMenu>().CurrentPage, Is.EqualTo(StarterTitlePage.NewGame));
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Slot 1"));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
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
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("New Game"));
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Title));
            Assert.That(AppRoot.Instance.Game.Current, Is.Null);
            Assert.That(Object.FindFirstObjectByType<StarterTitleMenu>().CurrentPage, Is.EqualTo(StarterTitlePage.NewGame));
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
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
        public IEnumerator SaveNotifiesObserversWithCommittedOrRejectedResult()
        {
            yield return LoadBoot();
            var root = AppRoot.Instance;
            Assert.That(root.TryStartNewGame(), Is.True);
            yield return WaitForScene(Main);
            var observations = new List<(string Message, StorageStatus Status, GameSession Session, bool CanContinue)>();
            root.StateChanged += () => observations.Add((root.StorageMessage, root.Game.Status, root.Game.Current, root.Game.CanContinue));

            Assert.That(root.TrySaveGame("{\"marker\":\"saved\"}"), Is.True);
            Assert.That(observations.Count, Is.EqualTo(1), "The save result must reach observers before the call returns.");
            Assert.That(observations[0].Message, Is.EqualTo(root.StorageMessage));
            Assert.That(observations[0].Status, Is.EqualTo(StorageStatus.Loaded));
            Assert.That(observations[0].CanContinue, Is.True);
            Assert.That(observations[0].Session, Is.SameAs(root.Game.Current));
            Assert.That(JsonUtility.FromJson<TestPayload>(observations[0].Session.PayloadJson).marker, Is.EqualTo("saved"));
            Assert.That(observations[0].Session.SavedUtc, Is.Not.Empty);
            var committedSession = root.Game.Current;
            var savePath = Path.Combine(testDirectory, GameSessionService.FileName);
            var committedFile = File.ReadAllText(savePath);

            Assert.That(root.TrySaveGame("not-json"), Is.False);
            Assert.That(observations.Count, Is.EqualTo(2));
            Assert.That(observations[1].Message, Does.StartWith("Save failed."));
            Assert.That(observations[1].Message, Is.EqualTo(root.StorageMessage));
            Assert.That(observations[1].Status, Is.EqualTo(StorageStatus.Loaded));
            Assert.That(observations[1].CanContinue, Is.True);
            Assert.That(observations[1].Session, Is.SameAs(committedSession));
            Assert.That(File.ReadAllText(savePath), Is.EqualTo(committedFile));
        }

        [UnityTest]
        public IEnumerator FailedContinueNotifiesObserversWithRefreshedSaveState()
        {
            var savedGame = new GameSessionService(fileStore);
            savedGame.StartNew("{\"marker\":\"saved\"}");
            Assert.That(savedGame.TrySave(), Is.True);
            yield return LoadBoot();
            var root = AppRoot.Instance;
            Assert.That(root.Game.CanContinue, Is.True);
            File.Delete(Path.Combine(testDirectory, GameSessionService.FileName));
            string observedMessage = null;
            var observedStatus = StorageStatus.Loaded;
            var observedCanContinue = true;
            GameSession observedSession = savedGame.Current;
            var observedIsTransitioning = true;
            root.StateChanged += () =>
            {
                observedMessage = root.StorageMessage;
                observedStatus = root.Game.Status;
                observedCanContinue = root.Game.CanContinue;
                observedSession = root.Game.Current;
                observedIsTransitioning = root.IsTransitioning;
            };

            Assert.That(root.TryContinueGame(), Is.False);
            Assert.That(observedMessage, Is.EqualTo("No saved game is available in this slot."));
            Assert.That(observedStatus, Is.EqualTo(StorageStatus.Missing));
            Assert.That(observedCanContinue, Is.False);
            Assert.That(observedSession, Is.Null);
            Assert.That(observedIsTransitioning, Is.False);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Title));
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

        [UnityTest]
        public IEnumerator ContinueOpensAllSlotsAndLoadsOnlyTheChosenSavedGame()
        {
            var savedGame = new GameSessionService(fileStore);
            savedGame.StartNew(3, "{\"marker\":\"slot-three\"}");
            Assert.That(savedGame.TrySave(), Is.True);
            var savedSession = savedGame.Current.SessionId;
            yield return LoadBoot();
            GameObject.Find("Continue").GetComponent<Button>().onClick.Invoke();
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Title));
            Assert.That(AppRoot.Instance.Game.Current, Is.Null);
            Assert.That(AppRoot.Instance.IsTransitioning, Is.False);
            Assert.That(GameObject.Find("Slot 1").GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("Slot 2").GetComponent<Button>().interactable, Is.False);
            var third = GameObject.Find("Slot 3").GetComponent<Button>();
            Assert.That(third.interactable, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(third.gameObject));
            var menu = Object.FindFirstObjectByType<StarterTitleMenu>();
            menu.GoBack();
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Continue"));
            menu.OpenContinue();
            third.onClick.Invoke();
            yield return WaitForScene(Main);
            Assert.That(AppRoot.Instance.Game.CurrentSlotId, Is.EqualTo(3));
            Assert.That(AppRoot.Instance.Game.Current.SessionId, Is.EqualTo(savedSession));
        }

        [UnityTest]
        public IEnumerator FullSlotsUseSeparateManagementAndDeleteDefaultsToCancel()
        {
            var savedGame = new GameSessionService(fileStore);
            for (var slot = 1; slot <= 3; slot++)
            {
                savedGame.StartNew(slot);
                Assert.That(savedGame.TrySave(), Is.True);
            }
            var secondSavePath = Path.Combine(testDirectory, "save-slot-2.json");
            var secondSave = File.ReadAllText(secondSavePath);
            var secondSavedAt = DateTimeOffset.Parse(savedGame.Slots[1].SavedUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            yield return LoadBoot();
            var menu = Object.FindFirstObjectByType<StarterTitleMenu>();
            menu.OpenNewGame();
            for (var slot = 1; slot <= 3; slot++)
                Assert.That(GameObject.Find("Slot " + slot).GetComponent<Button>().interactable, Is.False);
            menu.GoBack();
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("New Game"));
            menu.OpenNewGame();
            GameObject.Find("Manage Slots").GetComponent<Button>().onClick.Invoke();
            Assert.That(menu.CurrentPage, Is.EqualTo(StarterTitlePage.SlotManagement));
            GameObject.Find("Delete Slot 2").GetComponent<Button>().onClick.Invoke();
            Assert.That(menu.CurrentPage, Is.EqualTo(StarterTitlePage.DeleteConfirmation));
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Cancel Delete"));
            Assert.That(GameObject.Find("Delete Prompt").GetComponent<Text>().text, Does.Contain(secondSavedAt));
            Assert.That(GameObject.Find("New Game").GetComponent<Button>().interactable, Is.False);
            menu.GoBack();
            Assert.That(menu.CurrentPage, Is.EqualTo(StarterTitlePage.SlotManagement));
            Assert.That(File.ReadAllText(secondSavePath), Is.EqualTo(secondSave));
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Slot 2"));
            menu.RequestDeleteSlot(2);
            menu.ConfirmDelete();
            Assert.That(menu.CurrentPage, Is.EqualTo(StarterTitlePage.SlotManagement));
            Assert.That(File.Exists(secondSavePath), Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject?.name, Is.EqualTo("Slot 2"));
            Assert.That(AppRoot.Instance.Game.Current, Is.Null);
            menu.GoBack();
            Assert.That(menu.CurrentPage, Is.EqualTo(StarterTitlePage.NewGame));
            var secondSlot = GameObject.Find("Slot 2").GetComponent<Button>();
            Assert.That(secondSlot.interactable, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(secondSlot.gameObject));
            secondSlot.onClick.Invoke();
            yield return WaitForScene(Main);
            Assert.That(AppRoot.Instance.Game.CurrentSlotId, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TitleOptionsRefreshAfterExternalSettingsSavesWithTheSameMessage()
        {
            yield return LoadBoot();
            Assert.That(GameObject.Find("Status").GetComponent<Text>().text, Does.Contain("No saved games available."));
            Object.FindFirstObjectByType<StarterTitleMenu>().OpenOptions();
            var root = AppRoot.Instance;
            Assert.That(root.TrySaveSettings(new UserSettings { masterVolume = 0.25f }), Is.True);
            var message = root.StorageMessage;
            Assert.That(root.TrySaveSettings(new UserSettings { masterVolume = 0.75f }), Is.True);
            Assert.That(root.StorageMessage, Is.EqualTo(message));
            Assert.That(GameObject.Find("Volume").GetComponentInChildren<Text>().text, Is.EqualTo($"Volume: {0.75f:P0}"));
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
