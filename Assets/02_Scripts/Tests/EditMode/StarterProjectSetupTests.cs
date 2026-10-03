using System.Linq;
using NUnit.Framework;
using StarterProject.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace StarterProject.Tests
{
    /// <summary>생성 도구가 미저장 작업을 보호하고 기존 입력 구성을 보정하는지 검증합니다.</summary>
    public sealed class StarterProjectSetupTests
    {
        private SceneSetup[] previousSetup;

        [SetUp]
        public void SetUp()
        {
            previousSetup = null;
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    Assert.Ignore("Save modified scenes before running scene setup tests.");
            previousSetup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            if (previousSetup == null)
                return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }

        [Test]
        public void SetupRejectsUnsavedSceneBeforeChangingBuildSettings()
        {
            var marker = new GameObject("Unsaved work");
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            var before = EditorBuildSettings.scenes.Select(s => (s.path, s.enabled)).ToArray();

            Assert.That(StarterProjectSetup.CreateExampleAssets,
                Throws.InvalidOperationException.With.Message.Contains("Save all modified scenes"));
            Assert.That(marker != null, Is.True);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
            Assert.That(scene.isDirty, Is.True);
            Assert.That(EditorBuildSettings.scenes.Select(s => (s.path, s.enabled)), Is.EqualTo(before));
        }

        [Test]
        public void InputSetupAddsMissingModuleAndDisablesLegacyModule()
        {
            var events = new GameObject("Existing EventSystem").AddComponent<EventSystem>();
            var legacy = events.gameObject.AddComponent<StandaloneInputModule>();
            StarterProjectSetup.ConfigureInput();

            var module = events.GetComponent<InputSystemUIInputModule>();
            Assert.That(module, Is.Not.Null);
            Assert.That(module.enabled, Is.True);
            Assert.That(legacy.enabled, Is.False);
            Assert.That(module.point.action.actionMap.name, Is.EqualTo("UI"));
            Assert.That(module.submit.action.name, Is.EqualTo("Submit"));
        }

        [Test]
        public void InputSetupReusesInactiveEventSystemAndRemainsIdempotent()
        {
            var events = new GameObject("Inactive EventSystem").AddComponent<EventSystem>();
            events.enabled = false;
            events.gameObject.SetActive(false);
            StarterProjectSetup.ConfigureInput();
            StarterProjectSetup.ConfigureInput();

            Assert.That(events.isActiveAndEnabled, Is.True);
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(events.GetComponents<InputSystemUIInputModule>().Length, Is.EqualTo(1));
            Assert.That(events.GetComponent<InputSystemUIInputModule>().actionsAsset, Is.Not.Null);
        }

        [Test]
        public void ValidationReadsBootWithoutReplacingOpenScene()
        {
            var active = SceneManager.GetActiveScene();
            StarterProjectSetup.ValidateConfiguration();
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.False);
        }

        [Test]
        public void EditorTestSessionsUseUniquePathsUnderLibrary()
        {
            var first = StarterProjectPlayMode.GetSessionPath(System.Guid.NewGuid().ToString("N"));
            var second = StarterProjectPlayMode.GetSessionPath(System.Guid.NewGuid().ToString("N"));
            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(first.StartsWith(StarterProjectPlayMode.TestSessionsRoot + System.IO.Path.DirectorySeparatorChar), Is.True);
            Assert.That(() => StarterProjectPlayMode.GetSessionPath("../StarterData"), Throws.ArgumentException);
        }

        [Test]
        public void SceneInputValidationRejectsMissingEventSystem()
        {
            Assert.That(() => StarterProjectSetup.ValidateSceneInput(SceneManager.GetActiveScene(), "Main"),
                Throws.InvalidOperationException.With.Message.Contains("EventSystem"));
        }

        [TestCase("Disabled")]
        [TestCase("Inactive")]
        [TestCase("Duplicate")]
        public void BootstrapValidationRejectsUnusableEntryPoint(string condition)
        {
            var config = AssetDatabase.LoadAssetAtPath<AppConfig>(StarterProjectSetup.ConfigPath);
            var preview = EditorSceneManager.OpenPreviewScene(config.BootScene);
            try
            {
                var bootstrap = preview.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<AppBootstrap>(true)).Single();
                if (condition == "Disabled") bootstrap.enabled = false;
                else if (condition == "Inactive") bootstrap.gameObject.SetActive(false);
                else bootstrap.gameObject.AddComponent<AppBootstrap>();
                Assert.That(() => StarterProjectSetup.ValidateSceneBootstrap(preview, config),
                    Throws.InvalidOperationException.With.Message.Contains("one active AppBootstrap"));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        [TestCase("Point")]
        [TestCase("Navigate")]
        [TestCase("Click")]
        public void SceneInputValidationRejectsMissingActions(string actionName)
        {
            StarterProjectSetup.ConfigureInput();
            var module = Object.FindFirstObjectByType<InputSystemUIInputModule>();
            if (actionName == "Point") module.point = null;
            else if (actionName == "Navigate") module.move = null;
            else module.leftClick = null;
            Assert.That(() => StarterProjectSetup.ValidateSceneInput(SceneManager.GetActiveScene(), "Main"),
                Throws.InvalidOperationException.With.Message.Contains(actionName));
        }

        [TestCase("My Game", "My Game.exe")]
        [TestCase("Boss: Rush?", "Boss_ Rush_.exe")]
        [TestCase("CON", "Game_CON.exe")]
        [TestCase("  ", "Game.exe")]
        public void WindowsPreviewUsesSafeGameProductName(string productName, string executableName)
        {
            Assert.That(StarterProjectSetup.GetWindowsPreviewExecutableName(productName), Is.EqualTo(executableName));
        }
    }
}
