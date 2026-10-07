using System;
using System.Threading;
using UnityEngine;

namespace StarterProject
{
    public enum SceneRootState { Created, Initialized, Entered, Exited, Disposed }

    /// <summary>씬별 조립·시작·종료 경계입니다. 게임 규칙은 파생 클래스의 훅에 둡니다.</summary>
    [DisallowMultipleComponent]
    public class SceneRoot : MonoBehaviour, IDisposable
    {
        public SceneRootState State { get; private set; }
        public AppRoot App { get; private set; }
        [SerializeField] private InputContext initialInputContext = InputContext.Gameplay;
        public InputContext InitialInputContext => initialInputContext;
        internal void UseFallbackInput() => initialInputContext = InputContext.UI;
        private AsyncLifetime lifetime;
        private CancellationToken objectToken;
        public CancellationToken LifetimeToken => lifetime?.Token ?? default;
        private bool changing;

        public async Awaitable InitializeAsync(AppRoot app, CancellationToken cancellationToken)
        {
            Require(SceneRootState.Created);
            if (!Enum.IsDefined(typeof(InputContext), initialInputContext)) throw new InvalidOperationException("SceneRoot input context is invalid.");
            App = app != null ? app : throw new ArgumentNullException(nameof(app));
            changing = true;
            objectToken = destroyCancellationToken;
            lifetime = new AsyncLifetime(cancellationToken, objectToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await OnInitializeAsync(LifetimeToken);
                EnsureAlive(cancellationToken);
                State = SceneRootState.Initialized;
            }
            finally { changing = false; }
        }

        public async Awaitable EnterAsync(CancellationToken cancellationToken)
        {
            Require(SceneRootState.Initialized);
            changing = true;
            try
            {
                await OnEnterAsync(LifetimeToken);
                EnsureAlive(cancellationToken);
                State = SceneRootState.Entered;
            }
            finally { changing = false; }
        }

        public async Awaitable ExitAsync(CancellationToken cancellationToken)
        {
            Require(SceneRootState.Entered);
            changing = true;
            try
            {
                lifetime.Cancel();
                await OnExitAsync(cancellationToken);
                EnsureAlive(cancellationToken, allowEnded: true);
                State = SceneRootState.Exited;
            }
            finally { changing = false; }
        }

        private void Require(SceneRootState expected)
        {
            if (changing || State != expected)
                throw new InvalidOperationException($"SceneRoot cannot perform this operation in {State}.");
        }

        private void EnsureAlive(CancellationToken token, bool allowEnded = false)
        {
            token.ThrowIfCancellationRequested();
            objectToken.ThrowIfCancellationRequested();
            if (!allowEnded) LifetimeToken.ThrowIfCancellationRequested();
            if (State == SceneRootState.Disposed) throw new OperationCanceledException("SceneRoot was disposed.");
        }

        protected virtual Awaitable OnInitializeAsync(CancellationToken token) => Completed();
        protected virtual Awaitable OnEnterAsync(CancellationToken token) => Completed();
        protected virtual Awaitable OnExitAsync(CancellationToken token) => Completed();
        protected virtual void OnDispose() { }

        /// <summary>호출마다 별도 Awaitable을 반환합니다. Unity Awaitable은 한 번만 await합니다.</summary>
        protected static Awaitable Completed()
        {
            var source = new AwaitableCompletionSource();
            var awaitable = source.Awaitable;
            source.SetResult();
            return awaitable;
        }

        public void Dispose()
        {
            if (State == SceneRootState.Disposed) return;
            State = SceneRootState.Disposed;
            lifetime?.Dispose();
            try { OnDispose(); }
            finally { App = null; }
        }

        private void OnDestroy()
        {
            try { Dispose(); }
            catch (Exception error) { StarterLog.Warning(LogCategory.Scene, $"Scene disposal failed: {error.Message}"); }
        }
    }
}