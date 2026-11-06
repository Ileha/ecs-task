#nullable enable
using System;
using System.Runtime.ExceptionServices;

namespace LeoECSLiteExtensions.Async
{
    /// Completion state of one <see cref="EcsTask"/>. Not thread-safe: it is completed and awaited
    /// only inside the owning system's Run, on the main thread.
    internal class EcsTaskSource
    {
        private Action? _continuation;
        private ExceptionDispatchInfo? _exception;

        public bool IsCompleted { get; private set; }

        public Exception? Exception => _exception?.SourceException;

        public void SetResult() => Complete(null);

        public void SetException(Exception exception) => Complete(ExceptionDispatchInfo.Capture(exception));

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation();
                return;
            }

            if (_continuation != null)
            {
                throw new InvalidOperationException($"{nameof(EcsTask)} can be awaited only once");
            }

            _continuation = continuation;
        }

        public void GetResult()
        {
            if (!IsCompleted)
            {
                throw new InvalidOperationException($"{nameof(EcsTask)} does not support blocking wait; await it");
            }

            _exception?.Throw();
        }

        private void Complete(ExceptionDispatchInfo? exception)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException($"{nameof(EcsTask)} is already completed");
            }

            _exception = exception;
            IsCompleted = true;

            var continuation = _continuation;
            _continuation = null;
            continuation?.Invoke();
        }
    }

    /// Completion state of one <see cref="EcsTask{T}"/>: the same as <see cref="EcsTaskSource"/> plus the result.
    internal sealed class EcsTaskSource<T> : EcsTaskSource
    {
        private T _result = default!;

        public void SetResult(T result)
        {
            // Store the value before completing: completion runs the continuation, which reads it.
            _result = result;
            SetResult();
        }

        public new T GetResult()
        {
            base.GetResult();
            return _result;
        }
    }
}
