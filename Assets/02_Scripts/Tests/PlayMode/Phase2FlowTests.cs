using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StarterProject.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace StarterProject.Tests
{
    public sealed class Phase2FlowTests
    {
        private const string Main = "Assets/01_Scenes/Main/02_MainScene.unity";
        private const string Title = "Assets/01_Scenes/Main/01_Title.unity";
        private AppConfig config;
        private float originalScale;
        private MemoryStore store;
        private NoOpSettings runtime;
        [UnitySetUp] public IEnumerator Setup()
        {
            originalScale = Time.timeScale;
            if (AppRoot.Instance != null) Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync(Main); yield return null;
            config = ScriptableObject.CreateInstance<AppConfig>(); store = new MemoryStore();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (AppRoot.Instance != null) Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            Time.timeScale = originalScale;
            Object.Destroy(config);
        }
        private AppRoot CreateRoot()
        {
            var root = new GameObject("Phase2 Root").AddComponent<AppRoot>();
            runtime = new NoOpSettings();
            root.ConfigureStorage(store); root.ConfigureRuntimeSettings(runtime);
            return root;
        }
        private IEnumerator Ready(AppRoot root)
        {
            root.ConfigureStartupDestination(AppStartupDestination.NewGameInMain);
            Assert.That(root.Begin(config), Is.True);
            yield return Wait(() => root.State == AppState.Ready && !root.IsTransitioning);
            yield return null;
        }
        private static IEnumerator Wait(Func<bool> condition)
        {
            var end = Time.realtimeSinceStartup + 20;
            while (!condition()) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(end)); yield return null; }
        }
        private static IEnumerator Await(Awaitable task)
        {
            var awaiter = task.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;
            awaiter.GetResult();
        }
        [UnityTest] public IEnumerator SceneHooksRunInOrderAndExitCancelsOwnedContext()
        {
            var app = CreateRoot();
            var scene = new GameObject("Probe Scene").AddComponent<Phase2ProbeSceneRoot>();
            yield return Await(scene.InitializeAsync(app, app.LifetimeToken));
            yield return Await(scene.EnterAsync(app.LifetimeToken));
            using var input = new InputContextService();
            input.SetState(InputContext.Gameplay);
            using var scope = input.Push(InputContext.UI, scene.LifetimeToken);
            yield return Await(scene.ExitAsync(app.LifetimeToken));
            Assert.That(scene.Calls, Is.EqualTo(new[] { "Initialize", "Enter", "Exit" }));
            Assert.That(scene.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(input.Current, Is.EqualTo(InputContext.Gameplay));
            scene.Dispose(); scene.Dispose();
            Assert.That(scene.Calls, Is.EqualTo(new[] { "Initialize", "Enter", "Exit", "Dispose" }));
            Object.Destroy(scene.gameObject);
        }
        [UnityTest] public IEnumerator DestroyingSceneDuringEnterCancelsBeforeApplyingResult()
        {
            var app = CreateRoot();
            var scene = new GameObject("Delayed Scene").AddComponent<Phase2ProbeSceneRoot>();
            scene.DelayEnter = true;
            yield return Await(scene.InitializeAsync(app, app.LifetimeToken));
            var task = scene.EnterAsync(app.LifetimeToken);
            var awaiter = task.GetAwaiter();
            scene.Dispose(); Object.Destroy(scene.gameObject);
            yield return null;
            while (!awaiter.IsCompleted) yield return null;
            Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
            Assert.That(scene.Calls, Is.EqualTo(new[] { "Initialize", "Dispose" }));
        }
        [UnityTest] public IEnumerator RuntimeSaveFailureKeepsWorkingDataAndSuccessfulSaveCapturesSnapshot()
        {
            var root = CreateRoot(); yield return Ready(root);
            var working = root.Data.Runtime;
            working.Payload["stage"] = 3;
            var before = root.Game.Current.PayloadJson;
            store.FailWrites = true;
            Assert.That(root.TrySaveGame(), Is.False);
            Assert.That(root.Game.Current.PayloadJson, Is.EqualTo(before));
            Assert.That(working.Payload["stage"].Value<int>(), Is.EqualTo(3));
            store.FailWrites = false;
            Assert.That(root.TrySaveGame(), Is.True, root.StorageMessage);
            Assert.That(JObject.Parse(root.Game.Current.PayloadJson)["stage"].Value<int>(), Is.EqualTo(3));
            var saved = store.Files[GameSessionService.FileName];
            working.Payload["stage"] = 4;
            Assert.That(store.Files[GameSessionService.FileName], Is.EqualTo(saved));
            Assert.That(root.TryReturnToTitle(), Is.True);
            yield return Wait(() => SceneManager.GetActiveScene().path == Title && !root.IsTransitioning);
            Assert.That(root.Data.Runtime, Is.Null);
            Assert.That(root.TryContinueGame(), Is.True);
            yield return Wait(() => SceneManager.GetActiveScene().path == Main && !root.IsTransitioning);
            Assert.That(root.Data.Runtime.Payload["stage"].Value<int>(), Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator PauseInputUIAudioAndNavigationRestoreTogether()
        {
            var root = CreateRoot(); yield return Ready(root);
            var adapter = root.GetComponent<StarterInputContext>();
            Assert.That(adapter.RuntimeActions, Is.Not.Null);
            Assert.That(root.Input.Current, Is.EqualTo(InputContext.UI), "Fallback Main keeps the example UI usable.");
            Assert.That(adapter.RuntimeActions.FindActionMap("UI").enabled, Is.True);
            Assert.That(adapter.RuntimeActions.FindActionMap("Player").enabled, Is.False);
            using var gameplay = root.Input.Push(InputContext.Gameplay, root.CurrentSceneRoot.LifetimeToken);
            Assert.That(adapter.RuntimeActions.FindActionMap("Player").enabled, Is.True);
            Assert.That(adapter.RuntimeActions.FindActionMap("UI").enabled, Is.False);
            Time.timeScale = 0.5f;
            var clip = AudioClip.Create("Phase2 Tone", 44100, 1, 44100, false);
            try
            {
                Assert.That(root.Audio.PlayBgm(clip), Is.True);
                yield return null;
                Assert.That(root.Audio.BgmSource.isPlaying, Is.True);
                Assert.That(root.TryChangeGameState(GameState.Pause), Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(root.Audio.BgmSource.isPlaying, Is.False);
                Assert.That(root.Input.Current, Is.EqualTo(InputContext.UI));
                Assert.That(root.GetComponentInChildren<StarterPauseOverlay>().IsVisible, Is.True);
                Assert.That(root.Audio.PlaySfx(clip), Is.False);
                Assert.That(root.Audio.PlayUI(clip), Is.True);
                yield return null;
                Assert.That(root.Audio.UiSource.isPlaying, Is.True);
                Assert.That(root.TryChangeGameState(GameState.Gameplay), Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(0.5f));
                Assert.That(root.Input.Current, Is.EqualTo(InputContext.Gameplay));
                Assert.That(root.Audio.BgmSource.isPlaying, Is.True);
                Assert.That(root.TryChangeGameState(GameState.Pause), Is.True);
                Assert.That(root.TryReturnToTitle(), Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(0.5f));
                Assert.That(root.Input.Current, Is.EqualTo(InputContext.None));
                yield return Wait(() => SceneManager.GetActiveScene().path == Title && !root.IsTransitioning);
                Assert.That(root.Input.Current, Is.EqualTo(InputContext.UI));
                Assert.That(adapter.RuntimeActions.FindActionMap("Player").enabled, Is.False);
            }
            finally { Object.Destroy(clip); }
        }
        [UnityTest] public IEnumerator DestroyWhilePausedRestoresTimeAndDisposesAppServices()
        {
            var root = CreateRoot(); yield return Ready(root);
            Time.timeScale = 0.25f;
            Assert.That(root.TryChangeGameState(GameState.Pause), Is.True);
            var services = root.Services; var token = root.LifetimeToken; var audio = root.Audio.BgmSource;
            Object.Destroy(root.gameObject); yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(0.25f));
            Assert.That(token.IsCancellationRequested, Is.True);
            Assert.That(services.IsDisposed, Is.True);
            Assert.That(services.Input.Current, Is.EqualTo(InputContext.None));
            Assert.That(audio == null, Is.True);
        }
        [UnityTest] public IEnumerator AsyncExitKeepsRuntimeUntilOldSceneCleanupCompletes()
        {
            var root = CreateRoot();
            var probe = new GameObject("Async Exit Scene").AddComponent<Phase2ProbeSceneRoot>();
            probe.DelayExit = true;
            yield return Ready(root);
            Assert.That(root.CurrentSceneRoot, Is.SameAs(probe));
            Assert.That(root.TryReturnToTitle(), Is.True);
            Assert.That(root.Data.Runtime, Is.Not.Null, "Exit still owns the active session until cleanup completes.");
            yield return Wait(() => SceneManager.GetActiveScene().path == Title && !root.IsTransitioning);
            Assert.That(probe.SawRuntimeDuringExit, Is.True);
            Assert.That(root.Data.Runtime, Is.Null);
            Assert.That(root.Game.Current, Is.Null);
        }
        [UnityTest] public IEnumerator FailureWhilePausedRestoresTimeAndCancelsOwnedScene()
        {
            var root = CreateRoot(); yield return Ready(root);
            Time.timeScale = 0.75f;
            Assert.That(root.TryChangeGameState(GameState.Pause), Is.True);
            var sceneToken = root.CurrentSceneRoot.LifetimeToken;
            runtime.Fail = true;
            LogAssert.Expect(LogType.Error, "[Starter Project][App][Error] Phase2 apply failure.");
            Assert.That(root.TrySaveSettings(root.Settings.Current), Is.False);
            Assert.That(root.State, Is.EqualTo(AppState.Failed));
            Assert.That(root.CurrentGameState, Is.EqualTo(GameState.Failed));
            Assert.That(Time.timeScale, Is.EqualTo(0.75f));
            Assert.That(root.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(sceneToken.IsCancellationRequested, Is.True);
            Assert.That(root.Input.Current, Is.EqualTo(InputContext.None));
            Assert.That(root.Services.IsDisposed, Is.True);
        }
        private sealed class NoOpSettings : IRuntimeSettings
        {
            public bool Fail;
            public void Apply(UserSettings value) { if (Fail) throw new InvalidOperationException("Phase2 apply failure."); }
            public void Dispose() { }
        }
        private sealed class MemoryStore : ITextFileStore
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
            public bool FailWrites;
            public string ReadAllText(string name) => Files.TryGetValue(name, out var text) ? text : null;
            public void WriteAllText(string name, string content, Func<string, bool> validate, bool preserve)
            {
                if (FailWrites) throw new IOException("Test write failure.");
                if (!validate(content)) throw new InvalidDataException();
                Files[name] = content;
            }
        }
    }

    public sealed class Phase2ProbeSceneRoot : SceneRoot
    {
        public readonly List<string> Calls = new List<string>();
        public bool DelayEnter;
        public bool DelayExit;
        public bool SawRuntimeDuringExit;
        protected override Awaitable OnInitializeAsync(CancellationToken token) { Calls.Add("Initialize"); return Completed(); }
        protected override async Awaitable OnEnterAsync(CancellationToken token)
        {
            if (DelayEnter) await Awaitable.NextFrameAsync(token);
            token.ThrowIfCancellationRequested(); Calls.Add("Enter");
        }
        protected override async Awaitable OnExitAsync(CancellationToken token)
        {
            if (DelayExit) await Awaitable.NextFrameAsync(token);
            if (DelayExit) SawRuntimeDuringExit = App.Data.Runtime != null;
            Calls.Add("Exit");
        }
        protected override void OnDispose() => Calls.Add("Dispose");
    }
}