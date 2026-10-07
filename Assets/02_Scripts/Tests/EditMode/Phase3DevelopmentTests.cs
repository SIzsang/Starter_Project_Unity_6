using System;
using System.Linq;
using NUnit.Framework;
using StarterProject.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace StarterProject.Tests
{
    public sealed class Phase3DiagnosticsTests
    {
        private LogLevel level;
        private LogCategory categories;
        [SetUp] public void Setup()
        {
            level = StarterLog.MinimumLevel;
            categories = StarterLog.EnabledCategories;
            StarterLog.MinimumLevel = LogLevel.Warning;
            StarterLog.EnabledCategories = LogCategory.Storage | LogCategory.Scene;
        }
        [TearDown] public void Cleanup()
        {
            StarterLog.MinimumLevel = level;
            StarterLog.EnabledCategories = categories;
        }

        [Test] public void FilterCombinesSeverityAndCategoryWithoutEmittingRejectedErrors()
        {
            Assert.That(StarterLog.IsEnabled(LogCategory.Storage, LogLevel.Warning), Is.True);
            Assert.That(StarterLog.IsEnabled(LogCategory.Storage, LogLevel.Info), Is.False);
            Assert.That(StarterLog.IsEnabled(LogCategory.Audio, LogLevel.Error), Is.False);
            StarterLog.Error(LogCategory.Audio, "filtered error");
            LogAssert.NoUnexpectedReceived();
            StarterLog.MinimumLevel = LogLevel.Off;
            StarterLog.Error(LogCategory.Storage, "off error");
            LogAssert.NoUnexpectedReceived();
        }

        [Test] public void ConsoleKeepsErrorAndWarningSeverity()
        {
            LogAssert.Expect(LogType.Warning, "[Starter Project][Scene][Warning] scene warning");
            LogAssert.Expect(LogType.Error, "[Starter Project][Storage][Error] save error");
            StarterLog.Warning(LogCategory.Scene, "scene warning");
            StarterLog.Error(LogCategory.Storage, "save error");
        }

        [Test] public void ResetRestoresEnvironmentDefaultsAfterPlaySessionFilterChanges()
        {
            StarterLog.MinimumLevel = LogLevel.Off;
            StarterLog.EnabledCategories = LogCategory.None;
            StarterLog.ResetDefaults();
            Assert.That(BuildEnvironment.Current, Is.EqualTo(AppBuildKind.Development));
            Assert.That(StarterLog.MinimumLevel, Is.EqualTo(LogLevel.Debug));
            Assert.That(StarterLog.EnabledCategories, Is.EqualTo(LogCategory.All));
            Assert.That(StarterLog.IsEnabled(LogCategory.App, LogLevel.Debug), Is.True);
        }
    }

    public sealed class Phase3BuildTests
    {
        [TestCase(AppBuildKind.Development, "Windows", "STARTER_BUILD_DEVELOPMENT")]
        [TestCase(AppBuildKind.QA, "Windows-QA", "STARTER_BUILD_QA")]
        [TestCase(AppBuildKind.Release, "Windows-Release", "STARTER_BUILD_RELEASE")]
        public void OptionsIsolatePlayerEnvironmentAndPreserveEditorSettings(AppBuildKind kind, string folder, string define)
        {
            var beforeDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            var beforeScenes = EditorBuildSettings.scenes.Select(s => (s.path, s.enabled)).ToArray();
            var options = StarterBuildConfiguration.CreateOptions(kind);
            Assert.That(options.extraScriptingDefines, Is.EqualTo(new[] { define }));
            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneWindows64));
            Assert.That(options.locationPathName.Replace('\\', '/'), Does.StartWith("Builds/" + folder + "/"));
            var expected = BuildOptions.StrictMode;
            if (kind == AppBuildKind.Development) expected |= BuildOptions.Development | BuildOptions.AllowDebugging;
            if (kind == AppBuildKind.QA) expected |= BuildOptions.ForceEnableAssertions;
            Assert.That(options.options, Is.EqualTo(expected));
            Assert.That(options.scenes, Is.EqualTo(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)));
            Assert.That(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone), Is.EqualTo(beforeDefines));
            Assert.That(EditorBuildSettings.scenes.Select(s => (s.path, s.enabled)), Is.EqualTo(beforeScenes));
        }

        [TestCase("STARTER_BUILD_DEVELOPMENT")]
        [TestCase("STARTER_BUILD_QA")]
        [TestCase("STARTER_BUILD_RELEASE")]
        public void GlobalEnvironmentDefinesAreRejectedBeforeBuild(string define)
        {
            Assert.That(() => StarterBuildConfiguration.ValidateDefines("GAME_FEATURE; " + define + ";OTHER"),
                Throws.InvalidOperationException.With.Message.Contains("global Player Settings"));
        }

        [Test] public void GameDefinesRemainAllowedAndInvalidBuildKindCannotCreateOutput()
        {
            Assert.DoesNotThrow(() => StarterBuildConfiguration.ValidateDefines("GAME_FEATURE;STARTER_BUILD_QA_CUSTOM"));
            Assert.That(() => StarterBuildConfiguration.CreateOptions((AppBuildKind)99), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }

    public sealed class Phase3ValidatorTests
    {
        private Scene scene;
        [SetUp] public void Setup() => scene = EditorSceneManager.NewPreviewScene();
        [TearDown] public void Cleanup() => EditorSceneManager.ClosePreviewScene(scene);
        private T Add<T>(string name) where T : Component
        {
            var owner = new GameObject(name);
            SceneManager.MoveGameObjectToScene(owner, scene);
            return owner.AddComponent<T>();
        }

        [TestCase("Duplicate")]
        [TestCase("Disabled")]
        [TestCase("Inactive")]
        public void ValidatorRejectsUnusableSceneOwnership(string condition)
        {
            var root = Add<SceneRoot>("First root");
            if (condition == "Duplicate") Add<SceneRoot>("Second root");
            else if (condition == "Disabled") root.enabled = false;
            else root.gameObject.SetActive(false);
            Assert.That(() => StarterProjectValidator.ValidateSceneContracts(scene),
                Throws.InvalidOperationException.With.Message.Contains("one active SceneRoot"));
        }

        [Test] public void InvalidSerializedInputContextIsDetectedBeforeRuntime()
        {
            var root = Add<SceneRoot>("Root");
            var data = new SerializedObject(root);
            data.FindProperty("initialInputContext").intValue = 100;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(() => StarterProjectValidator.ValidateSceneContracts(scene),
                Throws.InvalidOperationException.With.Message.Contains("input context"));
        }

        [Test] public void CustomUiWithoutExampleComponentsKeepsOptionalInputAdapter()
        {
            Add<RectTransform>("Game custom UI");
            Assert.DoesNotThrow(() => StarterProjectValidator.ValidateSceneContracts(scene));
            var contract = StarterProjectValidator.ValidateSceneContracts(scene);
            Assert.That(contract.input, Is.Null);
            Assert.That(contract.gameplay, Is.False);
        }

        [TestCase(0, false)]
        [TestCase(2, true)]
        public void SavePolicyChecksVersionWithoutCallingGamePayloadHooks(int version, bool valid)
        {
            var bootstrap = Add<AppBootstrap>("Bootstrap");
            var policy = ScriptableObject.CreateInstance<GatePayloadPolicy>();
            try
            {
                policy.Version = version;
                var data = new SerializedObject(bootstrap);
                data.FindProperty("gamePayloadPolicy").objectReferenceValue = policy;
                data.ApplyModifiedPropertiesWithoutUndo();
                if (valid) Assert.DoesNotThrow(() => StarterProjectValidator.ValidateSavePolicy(bootstrap));
                else Assert.That(() => StarterProjectValidator.ValidateSavePolicy(bootstrap),
                    Throws.InvalidOperationException.With.Message.Contains("positive payload version"));
                Assert.That(policy.HookCalls, Is.Zero);
            }
            finally { Object.DestroyImmediate(policy); }
        }
    }

    public sealed class GatePayloadPolicy : GamePayloadPolicy
    {
        public int Version;
        public int HookCalls;
        public override int PayloadVersion => Version;
        public override string CreateInitialPayload() { HookCalls++; throw new InvalidOperationException("Must not run in validation."); }
        public override void ValidatePayload(string value) { HookCalls++; throw new InvalidOperationException("Must not run in validation."); }
    }
}
