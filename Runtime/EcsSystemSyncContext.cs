#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace LeoECSLiteExtensions.Async
{
    /// Queue of continuations owned by one async system. Continuations run only inside that system's
    /// Run (see <see cref="Drain"/>), so code after an await keeps the ECS system order.
    public sealed class EcsSystemSyncContext : SynchronizationContext
    {
        private const int NotExecuting = -1;

        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();

        private int _executingThreadId = NotExecuting;
        private volatile bool _isDisposed;

        public bool IsExecutingOnCurrentThread =>
            Volatile.Read(ref _executingThreadId) == Environment.CurrentManagedThreadId;

        public bool HasPending => !_queue.IsEmpty;

        public override void Post(SendOrPostCallback d, object? state)
        {
            // The owner system is destroyed: nobody will drain the queue anymore.
            if (_isDisposed)
            {
                return;
            }

            // Posts can come from worker threads or the PlayerLoop, so they always wait for the next Drain.
            _queue.Enqueue((d, state));
        }

        public override void Send(SendOrPostCallback d, object? state) =>
            throw new NotSupportedException($"{nameof(EcsSystemSyncContext)} does not support synchronous Send");

        public override SynchronizationContext CreateCopy() => this;

        public ExecutionScope Enter()
        {
            if (Volatile.Read(ref _executingThreadId) != NotExecuting)
            {
                throw new InvalidOperationException($"{nameof(EcsSystemSyncContext)} is already executing");
            }

            return new ExecutionScope(this);
        }

        /// Runs only the continuations queued before this call: a continuation that posts again
        /// (e.g. Task.Yield) resumes on the next Run instead of looping forever.
        public void Drain()
        {
            if (!IsExecutingOnCurrentThread)
            {
                throw new InvalidOperationException($"{nameof(Drain)} must be called inside {nameof(Enter)} scope");
            }

            var count = _queue.Count;

            for (var i = 0; i < count && _queue.TryDequeue(out var item); i++)
            {
                try
                {
                    item.Callback(item.State);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;

            while (_queue.TryDequeue(out _))
            {
            }
        }

        public readonly struct ExecutionScope : IDisposable
        {
            private readonly EcsSystemSyncContext _context;
            private readonly SynchronizationContext? _previous;

            internal ExecutionScope(EcsSystemSyncContext context)
            {
                _context = context;
                _previous = Current;

                Volatile.Write(ref context._executingThreadId, Environment.CurrentManagedThreadId);
                SetSynchronizationContext(context);
            }

            public void Dispose()
            {
                // Restore Unity's main-thread context so foreign code outside the system is not affected.
                SetSynchronizationContext(_previous);
                Volatile.Write(ref _context._executingThreadId, NotExecuting);
            }
        }
    }
}
