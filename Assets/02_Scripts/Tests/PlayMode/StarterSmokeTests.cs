using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace StarterProject.Tests
{
    /// <summary>사용자 저장과 분리된 실제 파일로 실행·저장·새 앱에서 복원하는 최소 회귀입니다.</summary>
    public sealed class StarterSmokeTests
    {
        private const string Boot = "Assets/01_Scenes/Boot/00_StartScene.unity";
        private const string Title = "Assets/01_Scenes/Main/01_Title.unity";
        private const string Main = "Assets/01_Scenes/Main/02_MainScene.unity";
        private string directory;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            directory = Path.Combine(Application.temporaryCachePath, "StarterSmoke-" + Guid.NewGuid().ToString("N"));
            if (AppRoot.Instance != null) Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (AppRoot.Instance != null) Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        private void CreateRoot()
        {
            var root = new GameObject("Smoke AppRoot").AddComponent<AppRoot>();
            root.ConfigureStorage(new JsonFileStore(directory));
            root.ConfigureRuntimeSettings(new NoOpSettings());
        }
        private static IEnumerator WaitForScene(string path)
        {
            var deadline = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != path || AppRoot.Instance == null
                || AppRoot.Instance.State != AppState.Ready || AppRoot.Instance.IsTransitioning)
            {
                Assert.That(AppRoot.Instance == null || AppRoot.Instance.State != AppState.Failed, Is.True,
                    AppRoot.Instance != null ? AppRoot.Instance.Failure : "No AppRoot");
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Smoke flow timed out.");
                yield return null;
            }
        }

        [UnityTest]
        [Category("StarterSmoke")]
        public IEnumerator BootMenuNewGameSaveAndFreshAppLoad()
        {
            CreateRoot();
            yield return SceneManager.LoadSceneAsync(Boot);
            yield return WaitForScene(Title);
            var firstRoot = AppRoot.Instance;
            Assert.That(firstRoot.CurrentGameState, Is.EqualTo(GameState.Menu));
            Assert.That(firstRoot.TryStartNewGame(2), Is.True);
            yield return WaitForScene(Main);
            Assert.That(firstRoot.CurrentSceneRoot.State, Is.EqualTo(SceneRootState.Entered));
            var sessionId = firstRoot.Game.Current.SessionId;
            var save = Path.Combine(directory, "save-slot-2.json");
            Assert.That(File.Exists(save), Is.False, "New Game alone must not save.");
            firstRoot.Data.Runtime.Payload["starterSmoke"] = 42;
            Assert.That(firstRoot.TrySaveGame(), Is.True, firstRoot.StorageMessage);
            Assert.That(File.Exists(save), Is.True);
            var snapshot = File.ReadAllText(save);
            firstRoot.Data.Runtime.Payload["starterSmoke"] = 99;
            Assert.That(File.ReadAllText(save), Is.EqualTo(snapshot));

            Object.Destroy(firstRoot.gameObject);
            yield return null;
            CreateRoot();
            yield return SceneManager.LoadSceneAsync(Boot);
            yield return WaitForScene(Title);
            Assert.That(AppRoot.Instance, Is.Not.SameAs(firstRoot));
            Assert.That(AppRoot.Instance.Game.GetSlot(2).CanContinue, Is.True);
            Assert.That(AppRoot.Instance.TryContinueGame(2), Is.True);
            yield return WaitForScene(Main);
            Assert.That(AppRoot.Instance.Game.Current.SessionId, Is.EqualTo(sessionId));
            Assert.That(AppRoot.Instance.Game.CurrentSlotId, Is.EqualTo(2));
            Assert.That(AppRoot.Instance.Data.Runtime.Payload["starterSmoke"].Value<int>(), Is.EqualTo(42));
            Assert.That(File.Exists(Path.Combine(directory, GameSessionService.FileName)), Is.False);
            Assert.That(File.Exists(Path.Combine(directory, SettingsService.FileName)), Is.False);
        }

        private sealed class NoOpSettings : IRuntimeSettings
        {
            public void Apply(UserSettings settings) { }
            public void Dispose() { }
        }
    }
}