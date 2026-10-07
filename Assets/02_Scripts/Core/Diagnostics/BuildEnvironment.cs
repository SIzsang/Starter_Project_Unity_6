#if (STARTER_BUILD_DEVELOPMENT && STARTER_BUILD_QA) || (STARTER_BUILD_DEVELOPMENT && STARTER_BUILD_RELEASE) || (STARTER_BUILD_QA && STARTER_BUILD_RELEASE)
#error Choose only one Starter build environment.
#endif
namespace StarterProject
{
    public enum AppBuildKind { Development, QA, Release }

    /// <summary>Player 단위 컴파일 환경입니다. Editor 설정 에셋을 런타임 상태로 사용하지 않습니다.</summary>
    public static class BuildEnvironment
    {
        public static AppBuildKind Current
        {
            get
            {
#if STARTER_BUILD_RELEASE
                return AppBuildKind.Release;
#elif STARTER_BUILD_QA
                return AppBuildKind.QA;
#elif STARTER_BUILD_DEVELOPMENT || UNITY_EDITOR || DEVELOPMENT_BUILD
                return AppBuildKind.Development;
#else
                return AppBuildKind.Release;
#endif
            }
        }
        public static LogLevel DefaultLogLevel => Current == AppBuildKind.Development ? LogLevel.Debug
            : Current == AppBuildKind.QA ? LogLevel.Info : LogLevel.Warning;
    }
}