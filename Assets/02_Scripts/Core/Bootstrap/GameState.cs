namespace StarterProject
{
    /// <summary>초기화 상태인 AppState와 별도로 관리하는 현재 실행 상태입니다.</summary>
    public enum GameState
    {
        Boot,
        Menu,
        Loading,
        Gameplay,
        /// <summary>상태 계약만 제공합니다. 시간·입력·UI 제어는 별도 책임입니다.</summary>
        Pause,
        Failed
    }
}