using System;
using UnityEngine;

namespace StarterProject
{
    [Flags]
    public enum LogCategory
    {
        None = 0, App = 1, Bootstrap = 2, Scene = 4, Storage = 8, Data = 16,
        Input = 32, Audio = 64, UI = 128, Lifetime = 256, Developer = 512,
        Build = 1024, Validation = 2048, Game = 4096, All = 8191
    }

    public enum LogLevel { Debug, Info, Warning, Error, Off }

    /// <summary>메인 스레드의 경량 Console 진단입니다. 저장 payload·비밀값을 기록하지 않습니다.</summary>
    public static class StarterLog
    {
        private static LogLevel minimumLevel = DefaultMinimumLevel;
        private static LogCategory categories = LogCategory.All;
        public static LogLevel MinimumLevel
        {
            get => minimumLevel;
            set
            {
                if (!Enum.IsDefined(typeof(LogLevel), value)) throw new ArgumentOutOfRangeException(nameof(value));
                minimumLevel = value;
            }
        }
        public static LogCategory EnabledCategories
        {
            get => categories;
            set
            {
                if ((value & ~LogCategory.All) != 0) throw new ArgumentOutOfRangeException(nameof(value));
                categories = value;
            }
        }
        public static LogLevel DefaultMinimumLevel => BuildEnvironment.DefaultLogLevel;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetDefaults()
        {
            minimumLevel = DefaultMinimumLevel;
            categories = LogCategory.All;
        }

        public static bool IsEnabled(LogCategory category, LogLevel level) =>
            level >= LogLevel.Debug && level < LogLevel.Off && level >= minimumLevel
            && category != LogCategory.None && (category & ~LogCategory.All) == 0 && (category & categories) != 0;

        public static void Write(LogCategory category, LogLevel level, string message, UnityEngine.Object context = null)
        {
            if (!IsEnabled(category, level)) return;
            var text = $"[Starter Project][{category}][{level}] {message}";
            if (level == LogLevel.Error) UnityEngine.Debug.LogError(text, context);
            else if (level == LogLevel.Warning) UnityEngine.Debug.LogWarning(text, context);
            else UnityEngine.Debug.Log(text, context);
        }

        public static void Debug(LogCategory category, string message, UnityEngine.Object context = null) => Write(category, LogLevel.Debug, message, context);
        public static void Info(LogCategory category, string message, UnityEngine.Object context = null) => Write(category, LogLevel.Info, message, context);
        public static void Warning(LogCategory category, string message, UnityEngine.Object context = null) => Write(category, LogLevel.Warning, message, context);
        public static void Error(LogCategory category, string message, UnityEngine.Object context = null) => Write(category, LogLevel.Error, message, context);
    }
}