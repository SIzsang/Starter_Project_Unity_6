using System;
using System.Threading;
using UnityEngine;

namespace StarterProject
{
    /// <summary>앱·씬·객체 소유자가 명시적으로 해제하는 취소 범위입니다. Token은 Dispose 후에도 조회할 수 있습니다.</summary>
    public sealed class AsyncLifetime : IDisposable
    {
        private readonly CancellationTokenSource source;
        public CancellationToken Token { get; }
        private bool ended;
        private bool disposed;

        public AsyncLifetime(CancellationToken parent = default, CancellationToken owner = default)
        {
            source = CancellationTokenSource.CreateLinkedTokenSource(parent, owner);
            Token = source.Token;
        }

        public void Cancel()
        {
            if (ended) return;
            ended = true;
            try { source.Cancel(); }
            catch (AggregateException error) { StarterLog.Warning(LogCategory.Lifetime, $"Cancellation callback failed: {error.Message}"); }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { Cancel(); }
            finally { source.Dispose(); }
        }
    }
}