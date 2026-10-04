using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace StarterProject.Tests
{
    public sealed class PersistenceTests
    {
        private string testDirectoryPath;
        private JsonFileStore fileStore;
        [SetUp]
        public void SetUp()
        {
            testDirectoryPath = Path.Combine(Application.temporaryCachePath, "StarterPersistenceTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectoryPath);
            fileStore = new JsonFileStore(testDirectoryPath);
        }
        [TearDown]
        public void TearDown() { if (Directory.Exists(testDirectoryPath)) Directory.Delete(testDirectoryPath, true); }
        private string GetTestFilePath(string fileName) => Path.Combine(testDirectoryPath, fileName);
        private void WriteSettingsFile(string json) => File.WriteAllText(GetTestFilePath(SettingsService.FileName), json);
        private UserSettings CreateDefaultSettings() => new UserSettings { masterVolume = 0.75f, fullscreen = true, language = "ko" };

        [Test]
        public void FirstRunCopiesDefaultsAndDoesNotCreateFiles()
        {
            var defaults = CreateDefaultSettings();
            var settings = new SettingsService(defaults, fileStore);
            defaults.masterVolume = 0;
            var copy = settings.Current;
            copy.masterVolume = 0;
            Assert.That(settings.Current.masterVolume, Is.EqualTo(0.75f));
            Assert.That(settings.Status, Is.EqualTo(StorageStatus.Missing));
            Assert.That(Directory.GetFiles(testDirectoryPath), Is.Empty);
        }

        [TestCase("{\"schemaVersion\":1}", 0.75f, true, "ko")]
        [TestCase("{\"schemaVersion\":1,\"masterVolume\":0,\"fullscreen\":false,\"language\":\"en\"}", 0f, false, "en")]
        [TestCase("{\"schemaVersion\":1,\"masterVolume\":9,\"fullscreen\":false,\"language\":\" \"}", 0.75f, false, "ko")]
        [TestCase("{\"schemaVersion\":1,\"masterVolume\":\"0\",\"fullscreen\":\"false\",\"language\":null}", 0.75f, true, "ko")]
        public void PartialSettingsPreserveDefaultsAndValidateFieldTypes(string json, float volume, bool fullscreen, string language)
        {
            WriteSettingsFile(json);
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            Assert.That(settings.Current.masterVolume, Is.EqualTo(volume));
            Assert.That(settings.Current.fullscreen, Is.EqualTo(fullscreen));
            Assert.That(settings.Current.language, Is.EqualTo(language));
            Assert.That(File.ReadAllText(GetTestFilePath(SettingsService.FileName)), Is.EqualTo(json));
        }

        [TestCase("ja")]
        [TestCase("fr")]
        [TestCase("zh-Hans")]
        [TestCase("pt-BR")]
        [TestCase("game:pirate")]
        public void GameLanguageIdentifiersWorkAsDefaultsAndSurviveSaveReload(string language)
        {
            var defaults = CreateDefaultSettings();
            defaults.language = language;
            var settings = new SettingsService(defaults, fileStore);
            Assert.That(settings.Current.language, Is.EqualTo(language));
            Assert.That(settings.TryApplyAndSave(settings.Current), Is.True, settings.Message);
            var restarted = new SettingsService(CreateDefaultSettings(), fileStore);
            Assert.That(restarted.Current.language, Is.EqualTo(language));
            Assert.That(restarted.Status, Is.EqualTo(StorageStatus.Loaded));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("en us")]
        [TestCase("en\u0001")]
        public void InvalidLanguageIdentifiersAreRejectedAndLoadedFieldsFallBack(string language)
        {
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            Assert.That(settings.TryApplyAndSave(settings.Current), Is.True, settings.Message);
            var before = File.ReadAllText(GetTestFilePath(SettingsService.FileName));
            var candidate = settings.Current;
            candidate.language = language;
            Assert.That(settings.TryApplyAndSave(candidate), Is.False);
            Assert.That(settings.Current.language, Is.EqualTo("ko"));
            Assert.That(File.ReadAllText(GetTestFilePath(SettingsService.FileName)), Is.EqualTo(before));

            var external = JObject.Parse(before);
            external["language"] = language;
            var invalidText = external.ToString();
            WriteSettingsFile(invalidText);
            settings.Reload();
            Assert.That(settings.Current.language, Is.EqualTo("ko"));
            Assert.That(settings.Message, Does.Contain("Invalid setting fields"));
            Assert.That(File.ReadAllText(GetTestFilePath(SettingsService.FileName)), Is.EqualTo(invalidText));
        }

        [Test]
        public void LanguageIdentifierLengthLimitAppliesToSaveAndReload()
        {
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            var candidate = settings.Current;
            candidate.language = new string('x', UserSettings.MaxLanguageIdentifierLength);
            Assert.That(settings.TryApplyAndSave(candidate), Is.True, settings.Message);
            var restarted = new SettingsService(CreateDefaultSettings(), fileStore);
            Assert.That(restarted.Current.language, Is.EqualTo(candidate.language));
            var before = File.ReadAllText(GetTestFilePath(SettingsService.FileName));
            candidate.language += "x";
            Assert.That(settings.TryApplyAndSave(candidate), Is.False);
            Assert.That(File.ReadAllText(GetTestFilePath(SettingsService.FileName)), Is.EqualTo(before));
            var external = JObject.Parse(before);
            external["language"] = candidate.language;
            WriteSettingsFile(external.ToString());
            settings.Reload();
            Assert.That(settings.Current.language, Is.EqualTo("ko"));
        }

        [Test]
        public void SettingsRestoreAfterRecreationAndKeepPreviousBackup()
        {
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            Assert.That(settings.TryApplyAndSave(new UserSettings { masterVolume = 0, fullscreen = false, language = "en" }), Is.True);
            var first = File.ReadAllText(GetTestFilePath(SettingsService.FileName));
            Assert.That(settings.TryApplyAndSave(CreateDefaultSettings()), Is.True);
            Assert.That(File.ReadAllText(GetTestFilePath(SettingsService.FileName + ".bak")), Is.EqualTo(first));
            var restart = new SettingsService(new UserSettings(), fileStore);
            Assert.That(restart.Current.language, Is.EqualTo("ko"));
            Assert.That(restart.Current.masterVolume, Is.EqualTo(0.75f));
        }

        [TestCase("{broken")]
        [TestCase("null")]
        [TestCase("{}")]
        [TestCase("{\"schemaVersion\":1,\"schemaVersion\":2}")]
        [TestCase("{\"schemaVersion\":1} trailing")]
        public void CorruptSettingsFallbackAndExplicitSavePreservesOriginal(string json)
        {
            WriteSettingsFile(json);
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            Assert.That(settings.Status, Is.EqualTo(StorageStatus.Invalid));
            Assert.That(settings.Current.language, Is.EqualTo("ko"));
            Assert.That(settings.TryApplyAndSave(CreateDefaultSettings()), Is.True, settings.Message);
            var preserved = Directory.GetFiles(testDirectoryPath, "*.preserved-*");
            Assert.That(preserved.Length, Is.EqualTo(1));
            Assert.That(File.ReadAllText(preserved[0]), Is.EqualTo(json));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(999)]
        public void UnsupportedSettingsNeverOverwriteOrUseOlderBackup(int version)
        {
            var text = "{\"schemaVersion\":" + version + "}";
            WriteSettingsFile(text);
            File.WriteAllText(GetTestFilePath(SettingsService.FileName + ".bak"), "{\"schemaVersion\":1,\"language\":\"en\"}");
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            Assert.That(settings.Status, Is.EqualTo(StorageStatus.UnsupportedVersion));
            Assert.That(settings.TryApplyAndSave(CreateDefaultSettings()), Is.False);
            Assert.That(File.ReadAllText(GetTestFilePath(SettingsService.FileName)), Is.EqualTo(text));
        }

        [Test]
        public void SettingsBackupRequiresExplicitRecoveryAndKeepsCorruptOriginal()
        {
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            settings.TryApplyAndSave(new UserSettings { language = "en" });
            settings.TryApplyAndSave(CreateDefaultSettings());
            WriteSettingsFile("broken");
            settings.Reload();
            Assert.That(settings.Status, Is.EqualTo(StorageStatus.Recovered));
            Assert.That(settings.Current.language, Is.EqualTo("en"));
            Assert.That(settings.TryApplyAndSave(CreateDefaultSettings()), Is.False);
            Assert.That(settings.TryRecoverBackup(), Is.True, settings.Message);
            Assert.That(File.ReadAllText(Directory.GetFiles(testDirectoryPath, "*.preserved-*").Single()), Is.EqualTo("broken"));
            Assert.That(new SettingsService(CreateDefaultSettings(), fileStore).Status, Is.EqualTo(StorageStatus.Loaded));
        }

        [Test]
        public void NewGameDoesNotOverwriteAndContinueRestoresPayload()
        {
            var game = new GameSessionService(fileStore);
            Assert.That(game.CanContinue, Is.False);
            game.StartNew("{\"checkpoint\":\"town\"}");
            var id = game.Current.SessionId;
            Assert.That(game.TrySave(), Is.True, game.Message);
            var text = File.ReadAllText(GetTestFilePath(GameSessionService.FileName));
            game.StartNew();
            Assert.That(game.TrySave(), Is.False);
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)), Is.EqualTo(text));
            Assert.That(game.TrySave(replaceExisting: true), Is.True, game.Message);
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName + ".bak")), Is.EqualTo(text));
            var restarted = new GameSessionService(fileStore);
            Assert.That(restarted.TryContinue(), Is.True);
            Assert.That(restarted.Current.SessionId, Is.Not.EqualTo(id));
            Assert.That(restarted.Current.SessionId, Is.EqualTo(game.Current.SessionId));
        }

        [Test]
        public void SupportedOldGameVersionMigratesInMemoryAndBacksUpOnSave()
        {
            var game = new GameSessionService(fileStore);
            game.StartNew("{\"checkpoint\":\"town\"}");
            Assert.That(game.TrySave(), Is.True, game.Message);
            // Keep the saved UTC text unchanged while constructing a v1 fixture.
            var old = JsonData.ParseObject(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)));
            old["schemaVersion"] = 1;
            old["data"] = old["payload"];
            old.Remove("payload");
            old.Remove("payloadVersion");
            var oldText = old.ToString();
            File.WriteAllText(GetTestFilePath(GameSessionService.FileName), oldText);
            var restart = new GameSessionService(fileStore);
            Assert.That(restart.TryContinue(), Is.True, restart.Message);
            Assert.That(JObject.Parse(restart.Current.PayloadJson)["checkpoint"].Value<string>(), Is.EqualTo("town"));
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)), Is.EqualTo(oldText));
            Assert.That(restart.TrySave(), Is.True, restart.Message);
            Assert.That(JObject.Parse(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)))["schemaVersion"].Value<int>(), Is.EqualTo(2));
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName + ".bak")), Is.EqualTo(oldText));
        }

        [Test]
        public void CorruptGameUsesBackupAndExplicitRecoveryPreservesBothCopies()
        {
            var game = new GameSessionService(fileStore);
            game.StartNew("{\"checkpoint\":\"first\"}");
            Assert.That(game.TrySave(), Is.True, game.Message);
            Assert.That(game.TrySave("{\"checkpoint\":\"second\"}"), Is.True, game.Message);
            var backup = File.ReadAllText(GetTestFilePath(GameSessionService.FileName + ".bak"));
            File.WriteAllText(GetTestFilePath(GameSessionService.FileName), "damaged");
            Assert.That(game.TryContinue(), Is.True);
            Assert.That(game.Status, Is.EqualTo(StorageStatus.Recovered));
            Assert.That(JObject.Parse(game.Current.PayloadJson)["checkpoint"].Value<string>(), Is.EqualTo("first"));
            Assert.That(game.TrySave(), Is.False);
            Assert.That(game.TryRecoverBackup(), Is.True, game.Message);
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName + ".bak")), Is.EqualTo(backup));
            Assert.That(File.ReadAllText(Directory.GetFiles(testDirectoryPath, "*.preserved-*").Single()), Is.EqualTo("damaged"));
            Assert.That(game.TrySave(), Is.True, game.Message);
        }

        [Test]
        public void FutureGameVersionAppearingAfterLoadBlocksSavingAndContinue()
        {
            var game = new GameSessionService(fileStore);
            game.StartNew();
            Assert.That(game.TrySave(), Is.True, game.Message);
            var json = JObject.Parse(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)));
            json["schemaVersion"] = 999;
            File.WriteAllText(GetTestFilePath(GameSessionService.FileName), json.ToString());
            Assert.That(game.TrySave(replaceExisting: true), Is.False);
            Assert.That(game.TryContinue(), Is.False);
            Assert.That(game.Status, Is.EqualTo(StorageStatus.UnsupportedVersion));
        }

        [Test]
        public void InvalidGameAndUnsupportedPayloadCannotBeOverwrittenByNewGame()
        {
            File.WriteAllText(GetTestFilePath(GameSessionService.FileName), "{}");
            var game = new GameSessionService(fileStore);
            game.StartNew();
            Assert.That(game.TrySave(replaceExisting: true), Is.False);
            Assert.That(game.TryContinue(), Is.False);
            File.Delete(GetTestFilePath(GameSessionService.FileName));
            Assert.That(game.TrySave(), Is.True, game.Message);
            var otherVersion = new GameSessionService(fileStore, payloadVersion: 2);
            Assert.That(otherVersion.TryContinue(), Is.False);
            Assert.That(otherVersion.Status, Is.EqualTo(StorageStatus.UnsupportedVersion));
        }

        [Test]
        public void FailedWriteKeepsRuntimeSettingsAndExistingDiskFile()
        {
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            settings.TryApplyAndSave(CreateDefaultSettings());
            var before = File.ReadAllText(GetTestFilePath(SettingsService.FileName));
            var failing = new SettingsService(CreateDefaultSettings(), new WriteFailureFileStore(fileStore));
            Assert.That(failing.TryApplyAndSave(new UserSettings { masterVolume = 0 }), Is.False);
            Assert.That(failing.Current.masterVolume, Is.EqualTo(0.75f));
            Assert.That(File.ReadAllText(GetTestFilePath(SettingsService.FileName)), Is.EqualTo(before));
        }

        [Test]
        public void FailedGameWriteKeepsSavedTimestampAndFile()
        {
            var game = new GameSessionService(fileStore);
            game.StartNew();
            game.TrySave();
            var before = File.ReadAllText(GetTestFilePath(GameSessionService.FileName));
            var failing = new GameSessionService(new WriteFailureFileStore(fileStore));
            failing.TryContinue();
            var timestamp = failing.Current.SavedUtc;
            Assert.That(failing.TrySave("{\"checkpoint\":1}"), Is.False);
            Assert.That(failing.Current.SavedUtc, Is.EqualTo(timestamp));
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)), Is.EqualTo(before));
        }

        [Test]
        public void LockedDestinationAndTemporaryVerificationFailureProtectExistingFile()
        {
            var game = new GameSessionService(fileStore);
            game.StartNew();
            game.TrySave();
            var before = File.ReadAllText(GetTestFilePath(GameSessionService.FileName));
            using (var locked = new FileStream(GetTestFilePath(GameSessionService.FileName), FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.That(game.TrySave(), Is.False);
            Assert.That(() => fileStore.WriteAllText(GameSessionService.FileName, "{}", _ => false, false), Throws.TypeOf<InvalidDataException>());
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)), Is.EqualTo(before));
            Assert.That(Directory.GetFiles(testDirectoryPath, "*.tmp"), Is.Empty);
        }

        [Test]
        public void OversizedFileAndDirectoryConflictAreReported()
        {
            WriteSettingsFile(new string('x', JsonFileStore.MaxFileSizeBytes + 1));
            Assert.That(new SettingsService(CreateDefaultSettings(), fileStore).Status, Is.EqualTo(StorageStatus.Invalid));
            var blocked = GetTestFilePath("blocked");
            File.WriteAllText(blocked, "occupied");
            var settings = new SettingsService(CreateDefaultSettings(), new JsonFileStore(blocked));
            Assert.That(settings.Status, Is.EqualTo(StorageStatus.IoError));
            Assert.That(settings.TryApplyAndSave(CreateDefaultSettings()), Is.False);
        }

        [Test]
        public void DeleteSaveRemovesOnlyItsGeneratedFileFamily()
        {
            const string target = "save-slot-1.json";
            var generatedId = Guid.NewGuid().ToString("N");
            var deletedNames = new[] { target, target + ".bak", target + ".preserved-" + generatedId,
                target + "." + generatedId + ".tmp" };
            var retainedNames = new[] { "save-slot-2.json", "save-slot-2.json.bak", "save-slot-10.json",
                SettingsService.FileName, SettingsService.FileName + ".bak", target + ".bak.notes",
                target + ".preserved-not-a-guid", target + ".not-a-guid.tmp",
                target + ".preserved-" + generatedId + ".notes", target + "." + Guid.NewGuid().ToString("D") + ".tmp" };
            foreach (var name in deletedNames.Concat(retainedNames)) File.WriteAllText(GetTestFilePath(name), name);
            var nestedDirectory = Path.Combine(testDirectoryPath, "other-owner");
            Directory.CreateDirectory(nestedDirectory);
            var nestedSave = Path.Combine(nestedDirectory, target);
            File.WriteAllText(nestedSave, "nested save");

            ((IDeleteSaveFileStore)fileStore).DeleteSaveFiles(target);

            foreach (var name in deletedNames) Assert.That(File.Exists(GetTestFilePath(name)), Is.False, name);
            foreach (var name in retainedNames) Assert.That(File.ReadAllText(GetTestFilePath(name)), Is.EqualTo(name));
            Assert.That(File.ReadAllText(nestedSave), Is.EqualTo("nested save"));
            Assert.DoesNotThrow(() => fileStore.DeleteSaveFiles(target));
        }

        [Test, Platform("Win")]
        public void DeleteLockedPrimaryReportsFailureAndKeepsPrimary()
        {
            const string target = "save-slot-1.json";
            var primaryPath = GetTestFilePath(target);
            File.WriteAllText(primaryPath, "primary");
            File.WriteAllText(primaryPath + ".bak", "backup");
            using (var locked = new FileStream(primaryPath, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.That(() => fileStore.DeleteSaveFiles(target), Throws.InstanceOf<IOException>());
            Assert.That(File.ReadAllText(primaryPath), Is.EqualTo("primary"));
        }

        [Test, Platform("Win")]
        public void DeleteLockedBackupDoesNotDeletePrimary()
        {
            const string target = "save-slot-1.json";
            var primaryPath = GetTestFilePath(target);
            File.WriteAllText(primaryPath, "primary");
            File.WriteAllText(primaryPath + ".bak", "backup");
            using (var locked = new FileStream(primaryPath + ".bak", FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.That(() => fileStore.DeleteSaveFiles(target), Throws.InstanceOf<IOException>());
            Assert.That(File.ReadAllText(primaryPath), Is.EqualTo("primary"));
            Assert.That(File.ReadAllText(primaryPath + ".bak"), Is.EqualTo("backup"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("../outside.json")]
        [TestCase("..\\outside.json")]
        public void DeleteSaveRejectsInvalidNamesWithoutChangingFiles(string fileName)
        {
            var sentinel = GetTestFilePath("sentinel.json");
            File.WriteAllText(sentinel, "unchanged");
            Assert.Throws<ArgumentException>(() => fileStore.DeleteSaveFiles(fileName));
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("unchanged"));
        }

        [Test]
        public void DeleteMissingSaveSucceedsWithoutCreatingStorageAndReportsDirectoryConflict()
        {
            const string target = "save-slot-1.json";
            Assert.DoesNotThrow(() => fileStore.DeleteSaveFiles(target));
            var missingDirectory = Path.Combine(testDirectoryPath, "missing-storage");
            var missingStore = new JsonFileStore(missingDirectory);
            Assert.DoesNotThrow(() => missingStore.DeleteSaveFiles(target));
            Assert.That(Directory.Exists(missingDirectory), Is.False);
            File.WriteAllText(missingDirectory, "directory blocked by file");
            Assert.That(() => missingStore.DeleteSaveFiles(target), Throws.InstanceOf<IOException>());
            Assert.That(File.ReadAllText(missingDirectory), Is.EqualTo("directory blocked by file"));
        }

        [Test]
        public void FileStoreRejectsDirectoryTraversal()
        {
            Assert.That(() => fileStore.ReadAllText("../outside.json"), Throws.ArgumentException);
            Assert.That(() => fileStore.ReadAllText("..\\outside.json"), Throws.ArgumentException);
        }

        [Test]
        public void OversizedSaveAndPayloadValidationFailureReturnFailureWithoutReplacingFile()
        {
            var game = new GameSessionService(fileStore, payloadValidator: text =>
            {
                if (JObject.Parse(text)["invalid"] != null) throw new InvalidDataException("Invalid game data.");
            });
            game.StartNew();
            Assert.That(game.TrySave(), Is.True);
            var before = File.ReadAllText(GetTestFilePath(GameSessionService.FileName));
            Assert.That(game.TrySave("{\"invalid\":true}"), Is.False);
            Assert.That(game.TrySave("{\"large\":\"" + new string('x', JsonFileStore.MaxFileSizeBytes) + "\"}"), Is.False);
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)), Is.EqualTo(before));
            Assert.That(Directory.GetFiles(testDirectoryPath, "*.tmp"), Is.Empty);
        }

        [TestCase("not-a-number")]
        [TestCase("2147483648")]
        public void PayloadConversionFailuresReturnFalseAndPreserveSessionAndFile(string invalidValue)
        {
            var game = new GameSessionService(fileStore, payloadValidator: text =>
            {
                var value = JObject.Parse(text)["value"];
                if (value != null) int.Parse(value.Value<string>(), System.Globalization.CultureInfo.InvariantCulture);
            });
            game.StartNew();
            Assert.That(game.TrySave(), Is.True, game.Message);
            var current = game.Current;
            var before = File.ReadAllText(GetTestFilePath(GameSessionService.FileName));
            var invalidPayload = new JObject { ["value"] = invalidValue }.ToString();
            Assert.That(game.TrySave(invalidPayload), Is.False);
            Assert.That(game.Current, Is.SameAs(current));
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)), Is.EqualTo(before));
            Assert.That(Directory.GetFiles(testDirectoryPath, "*.tmp"), Is.Empty);
        }

        [Test]
        public void PayloadValidatorProgrammingErrorsAreNotHiddenAsDataFailures()
        {
            var game = new GameSessionService(fileStore, payloadValidator: text =>
            {
                if (JObject.Parse(text)["value"] != null) throw new InvalidOperationException("Policy implementation failed.");
            });
            game.StartNew();
            Assert.That(game.TrySave(), Is.True, game.Message);
            var current = game.Current;
            var before = File.ReadAllText(GetTestFilePath(GameSessionService.FileName));
            Assert.Throws<InvalidOperationException>(() => game.TrySave("{\"value\":1}"));
            Assert.That(game.Current, Is.SameAs(current));
            Assert.That(File.ReadAllText(GetTestFilePath(GameSessionService.FileName)), Is.EqualTo(before));
        }

        [Test]
        public void VeryLargeFutureVersionRemainsProtected()
        {
            var text = "{\"schemaVersion\":9999999999999999999999999999999999}";
            WriteSettingsFile(text);
            var settings = new SettingsService(CreateDefaultSettings(), fileStore);
            Assert.That(settings.Status, Is.EqualTo(StorageStatus.UnsupportedVersion));
            Assert.That(settings.TryApplyAndSave(CreateDefaultSettings()), Is.False);
            Assert.That(File.ReadAllText(GetTestFilePath(SettingsService.FileName)), Is.EqualTo(text));
        }

        private sealed class WriteFailureFileStore : ITextFileStore
        {
            private readonly ITextFileStore innerFileStore;
            public WriteFailureFileStore(ITextFileStore innerFileStore) { this.innerFileStore = innerFileStore; }
            public string ReadAllText(string fileName) => innerFileStore.ReadAllText(fileName);
            public void WriteAllText(string fileName, string contents, Func<string, bool> validateContents, bool preserveOriginal)
                => throw new IOException("Injected disk full failure.");
        }
    }
}
