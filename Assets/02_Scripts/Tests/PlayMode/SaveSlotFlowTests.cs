using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace StarterProject.Tests
{
    /// <summary>슬롯 요청의 씬·전환 경계와 활성 세션의 저장 대상 유지 여부를 검증합니다.</summary>
    public sealed class SaveSlotFlowTests
    {
        private const string Boot = "Assets/01_Scenes/Boot/00_StartScene.unity";
        private const string Title = "Assets/01_Scenes/Main/01_Title.unity";
        private const string Main = "Assets/01_Scenes/Main/02_MainScene.unity";
        private string testDirectory;
        private JsonFileStore fileStore;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            testDirectory = Path.Combine(Application.temporaryCachePath, "StarterSlotFlowTests-" + Guid.NewGuid().ToString("N"));
            fileStore = new JsonFileStore(testDirectory);
            if (AppRoot.Instance != null) Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync(Main);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (AppRoot.Instance != null) Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }

        [UnityTest]
        public IEnumerator SlotRequestsRespectTitleAndTransitionGuardsAndContinueSelectedSession()
        {
            yield return LoadBoot();
            var root = AppRoot.Instance;
            Assert.That(root.TryStartNewGame(2), Is.True);
            Assert.That(root.TryStartNewGame(1), Is.False);
            Assert.That(root.TryContinueGame(1), Is.False);
            Assert.That(root.TryDeleteGameSlot(2), Is.False);
            yield return WaitForScene(Main);
            Assert.That(root.Game.CurrentSlotId, Is.EqualTo(2));
            Assert.That(root.TrySaveGame("{\"marker\":\"second-slot\"}"), Is.True, root.StorageMessage);
            var sessionId = root.Game.Current.SessionId;
            var original = File.ReadAllText(Path.Combine(testDirectory, "save-slot-2.json"));
            Assert.That(root.TryDeleteGameSlot(2), Is.False, "The active gameplay session cannot delete its save.");
            Assert.That(root.TryStartNewGame(1), Is.False);
            Assert.That(root.TryContinueGame(2), Is.False);

            Assert.That(root.TryReturnToTitle(), Is.True);
            Assert.That(root.TryDeleteGameSlot(2), Is.False);
            yield return WaitForScene(Title);
            Assert.That(root.Game.CurrentSlotId, Is.Null);
            Assert.That(root.TryStartNewGame(2), Is.False, "An occupied slot must not be replaced by New Game.");
            Assert.That(File.ReadAllText(Path.Combine(testDirectory, "save-slot-2.json")), Is.EqualTo(original));
            Assert.That(root.TryContinueGame(2), Is.True);
            yield return WaitForScene(Main);
            Assert.That(root.Game.CurrentSlotId, Is.EqualTo(2));
            Assert.That(root.Game.Current.SessionId, Is.EqualTo(sessionId));
            Assert.That(JsonUtility.FromJson<MarkerPayload>(root.Game.Current.PayloadJson).marker, Is.EqualTo("second-slot"));
            Assert.That(File.Exists(Path.Combine(testDirectory, GameSessionService.FileName)), Is.False);
        }

        [UnityTest]
        public IEnumerator DeletingAtTitleAllowsNewSessionInSameSlotAndPreservesOtherSlot()
        {
            var prepared = new GameSessionService(fileStore);
            prepared.StartNew(1, "{\"marker\":\"keep\"}");
            Assert.That(prepared.TrySave(), Is.True);
            var firstBefore = File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName));
            prepared.StartNew(3, "{\"marker\":\"remove\"}");
            Assert.That(prepared.TrySave(), Is.True);
            var removedId = prepared.Current.SessionId;
            prepared.EndSession();

            yield return LoadBoot();
            var root = AppRoot.Instance;
            Assert.That(root.TryDeleteGameSlot(3), Is.True, root.StorageMessage);
            Assert.That(root.Game.GetSlot(3).IsEmpty, Is.True);
            Assert.That(root.TryContinueGame(3), Is.False);
            Assert.That(root.TryStartNewGame(3), Is.True);
            yield return WaitForScene(Main);
            Assert.That(root.Game.CurrentSlotId, Is.EqualTo(3));
            Assert.That(root.Game.Current.SessionId, Is.Not.EqualTo(removedId));
            Assert.That(root.TrySaveGame("{\"marker\":\"fresh\"}"), Is.True, root.StorageMessage);
            Assert.That(File.ReadAllText(Path.Combine(testDirectory, GameSessionService.FileName)), Is.EqualTo(firstBefore));

            var restarted = new GameSessionService(new JsonFileStore(testDirectory));
            Assert.That(restarted.TryContinue(3), Is.True, restarted.Message);
            Assert.That(JsonUtility.FromJson<MarkerPayload>(restarted.Current.PayloadJson).marker, Is.EqualTo("fresh"));
        }

        [UnityTest]
        public IEnumerator InvalidSlotRequestsAtTitleLeaveSessionAndSceneUnchanged()
        {
            yield return LoadBoot();
            var root = AppRoot.Instance;
            foreach (var slotId in new[] { 0, root.Game.SlotCount + 1 })
            {
                Assert.That(root.TryStartNewGame(slotId), Is.False);
                Assert.That(root.TryContinueGame(slotId), Is.False);
                Assert.That(root.TryDeleteGameSlot(slotId), Is.False);
                Assert.That(root.TryRecoverGameBackup(slotId), Is.False);
            }
            Assert.That(root.Game.Current, Is.Null);
            Assert.That(root.IsTransitioning, Is.False);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Title));
            Assert.That(root.Game.AnyCanContinue, Is.False);
        }

        private IEnumerator LoadBoot()
        {
            var root = new GameObject("SlotFlowTestRoot").AddComponent<AppRoot>();
            root.ConfigureStorage(fileStore);
            root.ConfigureRuntimeSettings(new NoOpRuntimeSettings());
            yield return SceneManager.LoadSceneAsync(Boot);
            yield return WaitForScene(Title);
        }

        private static IEnumerator WaitForScene(string path)
        {
            var deadline = Time.realtimeSinceStartup + 20f;
            while (SceneManager.GetActiveScene().path != path || AppRoot.Instance == null || AppRoot.Instance.IsTransitioning)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Timed out waiting for slot flow.");
                yield return null;
            }
            Assert.That(AppRoot.Instance.State, Is.EqualTo(AppState.Ready));
            yield return null;
        }

        [Serializable]
        private sealed class MarkerPayload
        {
            public string marker = "";
        }

        private sealed class NoOpRuntimeSettings : IRuntimeSettings
        {
            public void Apply(UserSettings settings) { }
            public void Dispose() { }
        }
    }
}