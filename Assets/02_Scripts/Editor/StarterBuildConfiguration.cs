using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace StarterProject.Editor
{
    /// <summary>Windows 빌드별 옵션/심볼을 조립합니다. PlayerSettings/EditorBuildSettings를 변경하지 않습니다.</summary>
    public static class StarterBuildConfiguration
    {
        internal static readonly string[] ReservedDefines =
            { "STARTER_BUILD_DEVELOPMENT", "STARTER_BUILD_QA", "STARTER_BUILD_RELEASE" };

        internal static BuildPlayerOptions CreateOptions(AppBuildKind kind)
        {
            if (!Enum.IsDefined(typeof(AppBuildKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            var directory = kind == AppBuildKind.Development ? "Windows" : "Windows-" + kind;
            var flags = BuildOptions.StrictMode;
            if (kind == AppBuildKind.Development) flags |= BuildOptions.Development | BuildOptions.AllowDebugging;
            if (kind == AppBuildKind.QA) flags |= BuildOptions.ForceEnableAssertions;
            return new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine("Builds", directory, StarterProjectSetup.GetWindowsPreviewExecutableName(PlayerSettings.productName)),
                target = BuildTarget.StandaloneWindows64,
                options = flags,
                extraScriptingDefines = new[] { ReservedDefines[(int)kind] }
            };
        }

        internal static void ValidateGlobalDefines()
        {
            ValidateDefines(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone));
        }

        internal static void ValidateDefines(string defines)
        {
            var current = (defines ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (current.Any(define => ReservedDefines.Contains(define.Trim(), StringComparer.Ordinal)))
                throw new InvalidOperationException("Remove STARTER_BUILD_DEVELOPMENT/QA/RELEASE from global Player Settings. Build commands supply one per Player without changing Editor defines.");
        }

        public static void Build(AppBuildKind kind)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save modified scenes before building.");
            StarterProjectSetup.ValidateConfiguration();
            ValidateGlobalDefines();
            var options = CreateOptions(kind);
            Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName));
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Build failed: {report.summary.result}");
            StarterLog.Info(LogCategory.Build, $"{kind} Windows build succeeded ({report.summary.totalSize} bytes): {options.locationPathName}");
        }

        [MenuItem("Tools/Starter Project/Build Windows QA")]
        public static void BuildWindowsQA() => Build(AppBuildKind.QA);
        [MenuItem("Tools/Starter Project/Build Windows Release")]
        public static void BuildWindowsRelease() => Build(AppBuildKind.Release);
    }
}