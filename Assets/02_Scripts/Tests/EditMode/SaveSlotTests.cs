using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace StarterProject.Tests
{
    /// <summary>슬롯 간 데이터 분리, 기존 1번 파일 호환, 선택 삭제와 보호 상태를 검증합니다.</summary>
    public sealed class SaveSlotTests
    {
        private string testDirectory;
        private JsonFileStore fileStore;

        [SetUp]
        public void SetUp()
        {
            testDirectory = Path.Combine(Application.temporaryCachePath, "StarterSlotTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            fileStore = new JsonFileStore(testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }

        private string SlotPath(int slotId) => Path.Combine(testDirectory, "save-slot-" + slotId + ".json");
        private static string Payload(int slotId) => "{\"marker\":\"slot-" + slotId + "\"}";

        [Test]
        public void ThreeSlotsSaveAndReloadIndependentSessionsAndExposeStableSnapshots()
        {
            var game = new GameSessionService(fileStore);
            var initialSlots = game.Slots;
            Assert.That(initialSlots.Count, Is.EqualTo(game.SlotCount));
            Assert.That(initialSlots.Select(slot => slot.SlotId), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(initialSlots.All(slot => slot.IsEmpty && !slot.CanContinue), Is.True);
            Assert.That(game.AnyCanContinue, Is.False);
            var sessionIds = new string[game.SlotCount];

            for (var slotId = 1; slotId <= game.SlotCount; slotId++)
            {
                game.StartNew(slotId, Payload(slotId));
                sessionIds[slotId - 1] = game.Current.SessionId;
                Assert.That(game.CurrentSlotId, Is.EqualTo(slotId));
                Assert.That(game.TrySave(), Is.True, game.Message);
                game.EndSession();
                Assert.That(game.CurrentSlotId, Is.Null);
            }

            Assert.That(initialSlots.All(slot => slot.IsEmpty), Is.True, "Published slot snapshots must not mutate later.");
            Assert.That(sessionIds.Distinct().Count(), Is.EqualTo(game.SlotCount));
            var restarted = new GameSessionService(new JsonFileStore(testDirectory));
            Assert.That(restarted.AnyCanContinue, Is.True);
            for (var slotId = 1; slotId <= game.SlotCount; slotId++)
            {
                var info = restarted.GetSlot(slotId);
                Assert.That(info.Status, Is.EqualTo(StorageStatus.Loaded));
                Assert.That(info.IsEmpty, Is.False);
                Assert.That(info.SavedUtc, Is.Not.Empty);
                Assert.That(restarted.TryContinue(slotId), Is.True, restarted.Message);
                Assert.That(restarted.CurrentSlotId, Is.EqualTo(slotId));
                Assert.That(restarted.Current.SessionId, Is.EqualTo(sessionIds[slotId - 1]));
                Assert.That(JObject.Parse(restarted.Current.PayloadJson)["marker"].Value<string>(), Is.EqualTo("slot-" + slotId));
            }
        }

        [Test]
        public void ExistingSlotOneFileStillLoadsThroughLegacyEntryPoints()
        {
            var original = new GameSessionService(fileStore);
            original.StartNew("{\"marker\":\"legacy\"}");
            Assert.That(original.TrySave(), Is.True, original.Message);
            var savedId = original.Current.SessionId;
            Assert.That(File.Exists(Path.Combine(testDirectory, GameSessionService.FileName)), Is.True);
            Assert.That(GameSessionService.FileName, Is.EqualTo("save-slot-1.json"));

            var restarted = new GameSessionService(fileStore);
            Assert.That(restarted.TryContinue(), Is.True, restarted.Message);
            Assert.That(restarted.CurrentSlotId, Is.EqualTo(1));
            Assert.That(restarted.Current.SessionId, Is.EqualTo(savedId));
            Assert.That(restarted.GetSlot(2).IsEmpty && restarted.GetSlot(3).IsEmpty, Is.True);
        }

        [Test]
        public void DeletingOneSlotRemovesRecoverableBackupAndAllowsFreshReuseOnlyThere()
        {
            var settings = new SettingsService(new UserSettings(), fileStore);
            Assert.That(settings.TryApplyAndSave(new UserSettings { language = "ko", masterVolume = 0.25f }), Is.True);
            var settingsPath = Path.Combine(testDirectory, SettingsService.FileName);
            var settingsBefore = File.ReadAllText(settingsPath);
            var game = new GameSessionService(fileStore);
            for (var slotId = 1; slotId <= game.SlotCount; slotId++)
            {
                game.StartNew(slotId, Payload(slotId));
                Assert.That(game.TrySave(), Is.True, game.Message);
            }
            Assert.That(game.TryContinue(2), Is.True);
            var deletedSessionId = game.Current.SessionId;
            Assert.That(game.TrySave("{\"marker\":\"slot-2-updated\"}"), Is.True, game.Message);
            Assert.That(File.Exists(SlotPath(2) + ".bak"), Is.True);
            var firstBefore = File.ReadAllText(SlotPath(1));
            var thirdBefore = File.ReadAllText(SlotPath(3));
            game.EndSession();

            Assert.That(game.TryDelete(2), Is.True, game.Message);
            var restarted = new GameSessionService(new JsonFileStore(testDirectory));
            Assert.That(restarted.GetSlot(2).IsEmpty, Is.True);
            Assert.That(restarted.TryContinue(2), Is.False, "Deleted data must not return through its backup.");
            Assert.That(File.Exists(SlotPath(2)) || File.Exists(SlotPath(2) + ".bak"), Is.False);
            Assert.That(File.ReadAllText(SlotPath(1)), Is.EqualTo(firstBefore));
            Assert.That(File.ReadAllText(SlotPath(3)), Is.EqualTo(thirdBefore));
            Assert.That(File.ReadAllText(settingsPath), Is.EqualTo(settingsBefore));

            restarted.StartNew(2, "{\"marker\":\"replacement\"}");
            Assert.That(restarted.Current.SessionId, Is.Not.EqualTo(deletedSessionId));
            Assert.That(restarted.TrySave(), Is.True, restarted.Message);
            Assert.That(File.Exists(SlotPath(2) + ".bak"), Is.False);
            Assert.That(File.ReadAllText(SlotPath(1)), Is.EqualTo(firstBefore));
            Assert.That(File.ReadAllText(SlotPath(3)), Is.EqualTo(thirdBefore));
        }

        [Test]
        public void UnsupportedSecondSlotDoesNotBlockOtherSlotsAndIsNotOverwritten()
        {
            const string futureSave = "{\"schemaVersion\":999}";
            File.WriteAllText(SlotPath(2), futureSave);
            var game = new GameSessionService(fileStore);
            Assert.That(game.GetSlot(2).Status, Is.EqualTo(StorageStatus.UnsupportedVersion));
            Assert.That(game.GetSlot(2).IsEmpty, Is.False);
            Assert.That(game.GetSlot(2).CanContinue, Is.False);
            foreach (var slotId in new[] { 1, 3 })
            {
                game.StartNew(slotId, Payload(slotId));
                Assert.That(game.TrySave(), Is.True, game.Message);
            }
            game.EndSession();
            Assert.That(game.TryContinue(2), Is.False);
            Assert.That(game.Current, Is.Null);
            Assert.That(game.TryContinue(1), Is.True);
            Assert.That(game.TryContinue(3), Is.True);
            Assert.That(File.ReadAllText(SlotPath(2)), Is.EqualTo(futureSave));
        }

        [Test]
        public void CorruptSlotRecoveryUsesOnlyItsOwnBackup()
        {
            var game = new GameSessionService(fileStore);
            game.StartNew(1, Payload(1));
            Assert.That(game.TrySave(), Is.True);
            var otherSave = File.ReadAllText(SlotPath(1));
            game.StartNew(2, "{\"marker\":\"backup\"}");
            Assert.That(game.TrySave(), Is.True);
            Assert.That(game.TrySave("{\"marker\":\"latest\"}"), Is.True);
            game.EndSession();
            File.WriteAllText(SlotPath(2), "damaged-slot-two");
            game.RefreshSave();
            Assert.That(game.GetSlot(2).Status, Is.EqualTo(StorageStatus.Recovered));
            Assert.That(game.TryRecoverBackup(2), Is.True, game.Message);
            Assert.That(game.TryContinue(2), Is.True);
            Assert.That(JObject.Parse(game.Current.PayloadJson)["marker"].Value<string>(), Is.EqualTo("backup"));
            Assert.That(File.ReadAllText(SlotPath(1)), Is.EqualTo(otherSave));
            Assert.That(game.GetSlot(3).IsEmpty, Is.True);
        }

        [Test]
        public void StorageWithoutDeleteCapabilityKeepsWorkingAndRejectsDeletion()
        {
            var game = new GameSessionService(new ReadWriteOnlyStore(fileStore));
            game.StartNew(2, Payload(2));
            Assert.That(game.TrySave(), Is.True, game.Message);
            var before = File.ReadAllText(SlotPath(2));
            game.EndSession();
            Assert.That(game.TryDelete(2), Is.False);
            Assert.That(File.ReadAllText(SlotPath(2)), Is.EqualTo(before));
            Assert.That(game.TryContinue(2), Is.True, game.Message);
        }

        [TestCase(0)]
        [TestCase(4)]
        public void InvalidSlotIdsDoNotCreateFilesOrSession(int slotId)
        {
            var game = new GameSessionService(fileStore);
            Assert.Throws<ArgumentOutOfRangeException>(() => game.GetSlot(slotId));
            Assert.Throws<ArgumentOutOfRangeException>(() => game.StartNew(slotId));
            Assert.That(game.Current, Is.Null);
            Assert.That(Directory.GetFiles(testDirectory), Is.Empty);
        }

        private sealed class ReadWriteOnlyStore : ITextFileStore
        {
            private readonly ITextFileStore inner;
            public ReadWriteOnlyStore(ITextFileStore inner) => this.inner = inner;
            public string ReadAllText(string fileName) => inner.ReadAllText(fileName);
            public void WriteAllText(string fileName, string contents, Func<string, bool> validateContents, bool preserveOriginal)
                => inner.WriteAllText(fileName, contents, validateContents, preserveOriginal);
        }
    }
}