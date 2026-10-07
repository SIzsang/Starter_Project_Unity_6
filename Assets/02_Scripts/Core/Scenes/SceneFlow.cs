using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarterProject
{
    /// <summary>AppRoot가 소유하는 단일 씬 전환과 SceneRoot 생명주기입니다.</summary>
    public sealed class SceneFlow : IDisposable
    {
        private readonly AppRoot app;
        private bool disposed;
        private GameObject fallbackHost;
        public SceneRoot Current { get; private set; }

        public SceneFlow(AppRoot app) => this.app = app != null ? app : throw new ArgumentNullException(nameof(app));

        public async Awaitable InitializeActiveSceneAsync(CancellationToken token)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();
            var scene = SceneManager.GetActiveScene();
            if (Current != null && Current.gameObject.scene == scene && Current.State == SceneRootState.Entered)
                return;
            ReleaseCurrent();
            var roots = UnityEngine.Object.FindObjectsByType<SceneRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(root => root.gameObject.scene == scene).ToArray();
            if (roots.Length > 1) throw new InvalidOperationException($"Scene requires a single SceneRoot: {scene.path}");
            var sceneRoot = roots.SingleOrDefault();
            if (sceneRoot == null)
            {
                var host = new GameObject("SceneRoot");
                SceneManager.MoveGameObjectToScene(host, scene);
                fallbackHost = host;
                sceneRoot = host.AddComponent<SceneRoot>();
                sceneRoot.UseFallbackInput();
            }
            if (!sceneRoot.isActiveAndEnabled) throw new InvalidOperationException($"SceneRoot must be active: {scene.path}");
            Current = sceneRoot;
            await sceneRoot.InitializeAsync(app, token);
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            await sceneRoot.EnterAsync(token);
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();
        }

        public async Awaitable NavigateAsync(string scenePath, CancellationToken token, Action<float> progress, Action afterExit = null)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(scenePath) || SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
                throw new InvalidOperationException($"Cannot load a scene outside the build scene list: {scenePath}");
            try
            {
                if (Current != null && Current.State == SceneRootState.Entered)
                    await Current.ExitAsync(token);
            }
            finally { ReleaseCurrent(); }
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            afterExit?.Invoke();
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (operation == null) throw new InvalidOperationException($"Scene loading could not start: {scenePath}");
            // 이미 시작한 Unity 씬 로드는 끝까지 관찰하되 종료된 앱에 결과를 적용하지 않습니다.
            while (!operation.isDone)
            {
                if (!token.IsCancellationRequested && !disposed) progress?.Invoke(Mathf.Clamp01(operation.progress));
                await Awaitable.NextFrameAsync();
            }
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            await InitializeActiveSceneAsync(token);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(SceneFlow));
        }

        private void ReleaseCurrent()
        {
            var owned = Current;
            Current = null;
            if (fallbackHost != null) UnityEngine.Object.Destroy(fallbackHost);
            fallbackHost = null;
            if (owned == null) return;
            try { owned.Dispose(); }
            catch (Exception error) { StarterLog.Warning(LogCategory.Scene, $"Scene disposal failed: {error.Message}"); }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            ReleaseCurrent();
        }
    }
}