using System;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarterProject.Editor
{
    /// <summary>Editor 전용 개발 창입니다. 런타임 변경은 AppRoot의 기존 보호 경계를 통과합니다.</summary>
    public sealed class StarterDebugWindow : EditorWindow
    {
        private int slot = 1;
        private int sceneIndex;
        private string result = "";
        private IDisposable inputScope;
        private CancellationToken scopeToken;
        private AppRoot scopeOwner;
        private Vector2 scroll;

        [MenuItem("Tools/Starter Project/Debug Menu")]
        public static void Open() => GetWindow<StarterDebugWindow>("Starter Debug");

        private void OnInspectorUpdate()
        {
            if (inputScope != null && (scopeOwner != AppRoot.Instance || scopeToken.IsCancellationRequested))
                ReleaseInput();
            Repaint();
        }

        private void OnDisable() => ReleaseInput();

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Console", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Environment", BuildEnvironment.Current.ToString());
            StarterLog.MinimumLevel = (LogLevel)EditorGUILayout.EnumPopup("Minimum level", StarterLog.MinimumLevel);
            StarterLog.EnabledCategories = (LogCategory)EditorGUILayout.EnumFlagsField("Categories", StarterLog.EnabledCategories) & LogCategory.All;
            if (GUILayout.Button("Reset log filters")) StarterLog.ResetDefaults();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length > 0)
            {
                sceneIndex = Mathf.Clamp(sceneIndex, 0, scenes.Length - 1);
                sceneIndex = EditorGUILayout.Popup("Edit scene", sceneIndex, scenes);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("Open scene") && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(scenes[sceneIndex]);
            }
            EditorGUILayout.LabelField("Active", SceneManager.GetActiveScene().path);

            var root = AppRoot.Instance;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Application", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying || root == null)
                EditorGUILayout.HelpBox("Start Play through Boot or the isolated Main Play workflow to use runtime commands.", MessageType.Info);
            else
            {
                EditorGUILayout.LabelField("App / Game", root.State + " / " + root.CurrentGameState);
                EditorGUILayout.LabelField("Step", root.CurrentStep);
                EditorGUILayout.LabelField("Scene lifetime", root.CurrentSceneRoot != null ? root.CurrentSceneRoot.State.ToString() : "None");
                EditorGUILayout.LabelField("Input", root.Input != null ? root.Input.Current.ToString() : "None");
                EditorGUILayout.LabelField("Session", root.Game?.Current?.SessionId ?? "None");
                EditorGUILayout.LabelField("Slot", root.Game?.CurrentSlotId?.ToString() ?? "None");
                if (!string.IsNullOrEmpty(root.Failure)) EditorGUILayout.HelpBox(root.Failure, MessageType.Error);
                if (root.Services != null && !root.Services.IsDisposed)
                    EditorGUILayout.LabelField("Services", $"Data: {root.Data.Definitions.Count}, Slots: {root.Game.SlotCount}, Audio: ready");

                using (new EditorGUI.DisabledScope(root.State != AppState.Ready || root.IsTransitioning))
                {
                    slot = EditorGUILayout.IntSlider("Save slot", slot, 1, root.Game?.SlotCount ?? GameSessionService.DefaultSlotCount);
                    var menu = root.CurrentGameState == GameState.Menu;
                    using (new EditorGUI.DisabledScope(!menu))
                    {
                        if (GUILayout.Button("New game in empty slot")) Request(root.TryStartNewGame(slot), "New game");
                        if (GUILayout.Button("Continue selected slot")) Request(root.TryContinueGame(slot), "Continue");
                    }
                    using (new EditorGUI.DisabledScope(menu))
                    {
                        if (GUILayout.Button("Return to Title")) Request(root.TryReturnToTitle(), "Title");
                        if (GUILayout.Button("Save active slot")) Request(root.TrySaveGame(), "Save");
                        if (GUILayout.Button(root.CurrentGameState == GameState.Pause ? "Resume" : "Pause"))
                            Request(root.TryChangeGameState(root.CurrentGameState == GameState.Pause ? GameState.Gameplay : GameState.Pause), "Pause/Resume");
                        if (GUILayout.Button(inputScope == null ? "Use Debug input context" : "Restore input context"))
                        {
                            if (inputScope != null) ReleaseInput();
                            else
                            {
                                scopeOwner = root;
                                scopeToken = root.CurrentSceneRoot != null ? root.CurrentSceneRoot.LifetimeToken : root.LifetimeToken;
                                inputScope = root.Input.Push(InputContext.Debug, scopeToken);
                            }
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void Request(bool accepted, string command)
        {
            result = command + (accepted ? " accepted." : " rejected by the app state or slot policy.");
            StarterLog.Info(LogCategory.Developer, result);
        }
        private void ReleaseInput()
        {
            inputScope?.Dispose(); inputScope = null; scopeOwner = null;
        }
    }
}