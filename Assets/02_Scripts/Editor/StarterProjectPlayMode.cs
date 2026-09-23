using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarterProject.Editor
{
    /// <summary>Main 씬의 Editor Play만 Boot로 시작하고, 각 Play에 독립 저장소를 연결합니다.</summary>
    [InitializeOnLoad]
    public static class StarterProjectPlayMode
    {
        private const string PendingKey = "StarterProject.EditorPlay.Pending";
        private const string SessionKey = "StarterProject.EditorPlay.SessionId";
        private const string LatestKey = "StarterProject.EditorPlay.LatestSessionId";
        private const string PreviousStartKey = "StarterProject.EditorPlay.PreviousStartScene";

        static StarterProjectPlayMode()
        {
            AppBootstrap.EditorPrepareRoot = PrepareRoot;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        internal static string TestSessionsRoot => Path.GetFullPath(Path.Combine(ProjectRoot, "Library", "StarterProject", "PlaySessions"));

        internal static string GetSessionPath(string sessionId)
        {
            if (!Guid.TryParseExact(sessionId, "N", out _))
                throw new ArgumentException("Invalid Editor test session ID.", nameof(sessionId));
            return Path.Combine(TestSessionsRoot, sessionId);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Batch Test Runner runs use their own scene and storage lifecycle.
                if (Application.isBatchMode) return;
                var config = AssetDatabase.LoadAssetAtPath<AppConfig>(StarterProjectSetup.ConfigPath);
                if (config == null || SceneManager.GetActiveScene().path != config.MainScene) return;
                try
                {
                    StarterProjectSetup.ValidateConfiguration();
                    var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(config.BootScene);
                    var sessionId = Guid.NewGuid().ToString("N");
                    SessionState.SetString(PreviousStartKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
                    SessionState.SetString(SessionKey, sessionId);
                    SessionState.SetString(LatestKey, sessionId);
                    SessionState.SetBool(PendingKey, true);
                    EditorSceneManager.playModeStartScene = boot;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Starter Project] Main Play setup failed: {exception.Message}");
                    RestoreStartScene();
                    EditorApplication.isPlaying = false;
                }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
                AppBootstrap.EditorPrepareRoot = PrepareRoot;
            else if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                RestoreStartScene();
        }

        private static void PrepareRoot(AppRoot root)
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            var sessionId = SessionState.GetString(SessionKey, "");
            root.ConfigureStorage(new JsonFileStore(GetSessionPath(sessionId)));
            root.ConfigureStartupDestination(AppStartupDestination.NewGameInMain);
        }

        private static void RestoreStartScene()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            var previous = SessionState.GetString(PreviousStartKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous)
                ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            SessionState.EraseBool(PendingKey);
            SessionState.EraseString(SessionKey);
            SessionState.EraseString(PreviousStartKey);
        }

        /// <summary>마지막 개발용 Play의 저장 폴더를 엽니다. 일반 사용자 저장 경로는 사용하지 않습니다.</summary>
        [MenuItem("Tools/Starter Project/Open Test Data Folder")]
        public static void OpenTestDataFolder()
        {
            var latest = SessionState.GetString(LatestKey, "");
            var path = string.IsNullOrEmpty(latest) ? TestSessionsRoot : GetSessionPath(latest);
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>개발용 세션 폴더를 확인 후 UserSettings 내부 백업으로 이동합니다.</summary>
        [MenuItem("Tools/Starter Project/Reset Test Data")]
        public static void ResetTestData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before resetting test data.");
            var source = TestSessionsRoot;
            if (!Directory.Exists(source))
            {
                Debug.Log("[Starter Project] No Editor test data to reset.");
                return;
            }
            var backupRoot = Path.Combine(ProjectRoot, "UserSettings", "StarterProject", "TestDataBackups");
            var backup = Path.Combine(backupRoot, "PlaySessions-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                + "-" + Guid.NewGuid().ToString("N"));
            EnsureProjectPath(source, "Library");
            EnsureProjectPath(backup, "UserSettings");
            if (!EditorUtility.DisplayDialog("Reset Starter Project test data",
                "Move the Editor test sessions to this backup?\n\nFrom: " + source + "\nTo: " + backup,
                "Back up and reset", "Cancel")) return;
            Directory.CreateDirectory(backupRoot);
            Directory.Move(source, backup);
            SessionState.EraseString(LatestKey);
            Debug.Log("[Starter Project] Test data moved to " + backup);
        }

        private static void EnsureProjectPath(string path, string topLevelFolder)
        {
            var full = Path.GetFullPath(path);
            var allowed = Path.GetFullPath(Path.Combine(ProjectRoot, topLevelFolder)) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test data and backups must remain in this project.");
        }
    }
}
