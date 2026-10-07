using System;
using System.Collections.Generic;
using System.Linq;
using StarterProject.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace StarterProject.Editor
{
    /// <summary>실행 계약을 Preview Scene에서 검사합니다. 씬·에셋·사용자 저장을 고치거나 읽지 않습니다.</summary>
    public static class StarterProjectValidator
    {
        internal static void ValidateConfiguration(AppConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            StarterBuildConfiguration.ValidateGlobalDefines();
            var paths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (paths.Distinct(StringComparer.Ordinal).Count() != paths.Length)
                throw new InvalidOperationException("Enabled build scenes contain duplicate paths.");

            var defaultInputs = new List<InputActionAsset>();
            var usesGameplay = false;
            foreach (var path in paths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new InvalidOperationException($"Scene asset is missing: {path}");
                var preview = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    if (path == config.BootScene)
                    {
                        StarterProjectSetup.ValidateSceneBootstrap(preview, config);
                        var bootstrap = Components<AppBootstrap>(preview).Single();
                        ValidateSavePolicy(bootstrap);
                    }
                    var contract = ValidateSceneContracts(preview);
                    if (contract.input != null) defaultInputs.Add(contract.input);
                    usesGameplay |= contract.gameplay;
                }
                finally { EditorSceneManager.ClosePreviewScene(preview); }
            }
            // 기본 어댑터는 첫 UI Asset을 앱 수명으로 복제해 Gameplay에도 사용합니다.
            if (usesGameplay)
                foreach (var asset in defaultInputs.Distinct())
                    if (asset.FindActionMap("Player", false) == null)
                        throw new InvalidOperationException($"Default input asset requires a Player map for Gameplay: {AssetDatabase.GetAssetPath(asset)}");
        }

        internal static (InputActionAsset input, bool gameplay) ValidateSceneContracts(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                        throw new InvalidOperationException($"Missing script in {scene.path}: {child.name}");

            var roots = Components<SceneRoot>(scene);
            if (roots.Length > 1 || (roots.Length == 1 && !roots[0].isActiveAndEnabled))
                throw new InvalidOperationException($"Scene requires at most one active SceneRoot: {scene.path}");
            if (roots.Length == 1 && !Enum.IsDefined(typeof(InputContext), roots[0].InitialInputContext))
                throw new InvalidOperationException($"SceneRoot input context is invalid: {scene.path}");
            if (Components<AppRoot>(scene).Length > 1)
                throw new InvalidOperationException($"Scene has multiple authored AppRoot service owners: {scene.path}");

            var defaultUI = Components<StarterScreen>(scene).Any(screen => screen.isActiveAndEnabled)
                || Components<StarterTitleMenu>(scene).Any(menu => menu.isActiveAndEnabled)
                || Components<StarterInputContext>(scene).Any(adapter => adapter.isActiveAndEnabled);
            InputActionAsset input = null;
            if (defaultUI)
            {
                StarterProjectSetup.ValidateSceneInput(scene, scene.path);
                input = Components<InputSystemUIInputModule>(scene).Single(module => module.isActiveAndEnabled).actionsAsset;
            }
            return (input, roots.Length == 1 && roots[0].InitialInputContext == InputContext.Gameplay);
        }

        internal static void ValidateSavePolicy(AppBootstrap bootstrap)
        {
            var property = new SerializedObject(bootstrap).FindProperty("gamePayloadPolicy");
            var policy = property.objectReferenceValue as GamePayloadPolicy;
            if (policy == null && property.objectReferenceInstanceIDValue != 0)
                throw new InvalidOperationException("Boot has a missing GamePayloadPolicy reference.");
            if (policy != null && policy.PayloadVersion < 1)
                throw new InvalidOperationException("GamePayloadPolicy requires a positive payload version.");
            // CreateInitialPayload/ValidatePayload는 게임 훅입니다. Editor에서 세션을 만들거나 사용자 저장을 검사하지 않습니다.
        }

        private static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }

    /// <summary>일반 Build Pipeline에서도 같은 공통 구성 검사를 수행합니다.</summary>
    public sealed class StarterBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => StarterProjectSetup.ValidateConfiguration();
    }
}