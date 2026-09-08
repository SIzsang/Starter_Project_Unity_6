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
    }
}
