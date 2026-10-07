using System;

namespace StarterProject
{
    /// <summary>씬·시간·입력 의존성 없이 실행 상태의 전환 규칙과 알림을 소유합니다.</summary>
    public sealed class GameStateController
    {
        public GameState Current { get; private set; } = GameState.Boot;
        public event Action<GameState, GameState> StateChanged;
        internal bool IsNotifying { get; private set; }

        /// <summary>같은 상태, 정의되지 않은 값, 알림 중 재진입과 허용되지 않는 경로를 거부합니다.</summary>
        public bool CanTransitionTo(GameState next)
        {
            if (!Enum.IsDefined(typeof(GameState), next) || Current == next || Current == GameState.Failed)
                return false;
            // 실패는 알림 도중에도 즉시 진입 차단을 보장해야 합니다.
            if (next == GameState.Failed) return true;
            if (IsNotifying) return false;

            return Current switch
            {
                GameState.Boot => next == GameState.Loading || next == GameState.Menu || next == GameState.Gameplay,
                GameState.Menu => next == GameState.Loading,
                GameState.Loading => next == GameState.Menu || next == GameState.Gameplay,
                GameState.Gameplay => next == GameState.Loading || next == GameState.Pause,
                GameState.Pause => next == GameState.Gameplay || next == GameState.Loading,
                _ => false
            };
        }

        /// <summary>성공한 전환에만 이전·현재 상태를 동기 알림합니다. Failed는 새 루트까지 종료 상태입니다.</summary>
        public bool TryTransitionTo(GameState next)
        {
            if (!CanTransitionTo(next)) return false;
            var previous = Current;
            Current = next;
            var wasNotifying = IsNotifying;
            IsNotifying = true;
            try { StateChanged?.Invoke(previous, next); }
            finally { IsNotifying = wasNotifying; }
            return true;
        }
    }
}