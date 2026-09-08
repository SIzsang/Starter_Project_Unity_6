namespace StarterProject
{
    /// <summary><see cref="AppRoot"/>의 앱 초기화 생명주기를 나타냅니다.</summary>
    public enum AppState
    {
        /// <summary>아직 초기화를 요청받지 않은 상태입니다.</summary>
        NotStarted,

        /// <summary>필수 설정과 서비스 준비를 순서대로 진행하는 상태입니다.</summary>
        Initializing,

        /// <summary>초기화가 끝나 Title/Main 씬 요청을 받을 수 있는 상태입니다.</summary>
        Ready,

        /// <summary>필수 초기화 또는 씬 전환이 실패해 추가 진입을 차단한 상태입니다.</summary>
        Failed
    }
}
