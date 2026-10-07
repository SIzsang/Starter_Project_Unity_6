using System;
using UnityEngine;

namespace StarterProject
{
    /// <summary>게임 시간만 중지합니다. UI·비동기 정리는 unscaled 시간/앱 토큰을 사용합니다.</summary>
    public sealed class PauseService : IDisposable
    {
        private float previousScale;
        private bool disposed;
        public bool IsPaused { get; private set; }

        internal void Apply(bool paused)
        {
            if (disposed || IsPaused == paused) return;
            if (paused) { previousScale = Time.timeScale; Time.timeScale = 0; }
            else Time.timeScale = previousScale;
            IsPaused = paused;
        }

        public void Dispose()
        {
            if (disposed) return;
            Apply(false);
            disposed = true;
        }
    }
}