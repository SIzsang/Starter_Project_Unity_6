using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace StarterProject
{
    public enum InputContext { None, Gameplay, UI, Debug }

    /// <summary>메인 스레드에서 사용합니다. 임시 컨텍스트는 마지막 유효 소유자가 우선합니다.</summary>
    public sealed class InputContextService : IDisposable
    {
        private readonly List<Scope> scopes = new List<Scope>();
        private InputContext baseContext;
        private InputContext? forcedContext = InputContext.None;
        private bool disposed;
        public InputContext Current { get; private set; }
        public event Action<InputContext> Changed;

        public void SetState(InputContext context, InputContext? forced = null)
        {
            if (disposed) throw new ObjectDisposedException(nameof(InputContextService));
            Validate(context);
            if (forced.HasValue) Validate(forced.Value);
            baseContext = context; forcedContext = forced;
            Refresh();
        }

        public IDisposable Push(InputContext context, CancellationToken lifetime = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(InputContextService));
            Validate(context);
            var scope = new Scope(this, context);
            if (lifetime.IsCancellationRequested) { scope.Dispose(); return scope; }
            scopes.Add(scope);
            Refresh();
            scope.Attach(lifetime);
            return scope;
        }

        private static void Validate(InputContext context)
        {
            if (!Enum.IsDefined(typeof(InputContext), context)) throw new ArgumentOutOfRangeException(nameof(context));
        }

        private void Refresh()
        {
            var next = disposed ? InputContext.None : forcedContext ?? (scopes.Count > 0 ? scopes[scopes.Count - 1].Context : baseContext);
            if (Current == next) return;
            Current = next;
            var observers = Changed;
            if (observers == null) return;
            foreach (Action<InputContext> observer in observers.GetInvocationList())
            {
                if (Current != next) break;
                try { observer(next); }
                catch (Exception error) { StarterLog.Warning(LogCategory.Input, $"Input observer failed: {error.Message}"); }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var scope in scopes.ToArray()) scope.Dispose();
            Refresh();
            Changed = null;
        }

        private sealed class Scope : IDisposable
        {
            private InputContextService owner;
            private CancellationTokenRegistration registration;
            internal InputContext Context { get; }
            internal Scope(InputContextService owner, InputContext context) { this.owner = owner; Context = context; }
            internal void Attach(CancellationToken token)
            {
                registration = token.Register(Dispose);
                if (owner == null) registration.Dispose();
            }
            public void Dispose()
            {
                var previous = owner;
                if (previous == null) return;
                owner = null;
                previous.scopes.Remove(this);
                registration.Dispose();
                previous.Refresh();
            }
        }
    }
}