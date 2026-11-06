#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace LeoECSLiteExtensions.Async
{
    /// Return type for async code that touches ECS state. Its builder resumes every await
    /// (UniTask, Task, JobHandle, custom awaiters) inside the owning system's Run.
    /// default(EcsTask) is an already completed task.
    [AsyncMethodBuilder(typeof(EcsTaskMethodBuilder))]
    public readonly struct EcsTask
    {
        private readonly EcsTaskSource? _source;

        internal EcsTask(EcsTaskSource source)
        {
            _source = source;
        }

        public bool IsCompleted => _source == null || _source.IsCompleted;

        public Exception? Exception => _source?.Exception;

        /// An already completed task with a value, for methods that can answer without awaiting.
        public static EcsTask<T> FromResult<T>(T result) => new EcsTask<T>(result);

        public Awaiter GetAwaiter() => new Awaiter(_source);

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly EcsTaskSource? _source;

            internal Awaiter(EcsTaskSource? source)
            {
                _source = source;
            }

            public bool IsCompleted => _source == null || _source.IsCompleted;

            public void GetResult() => _source?.GetResult();

            public void OnCompleted(Action continuation) => RegisterContinuation(_source, continuation);

            public void UnsafeOnCompleted(Action continuation) => RegisterContinuation(_source, continuation);
        }

        internal static void RegisterContinuation(EcsTaskSource? source, Action continuation)
        {
            if (source == null)
            {
                continuation();
                return;
            }

            source.OnCompleted(continuation);
        }
    }

    /// <see cref="EcsTask"/> that returns a value: <c>var target = await FindTarget(ct);</c>.
    /// default(EcsTask&lt;T&gt;) is an already completed task with default(T).
    [AsyncMethodBuilder(typeof(EcsTaskMethodBuilder<>))]
    public readonly struct EcsTask<T>
    {
        private readonly EcsTaskSource<T>? _source;

        // Used only when there is no source: the task was created already completed.
        private readonly T _result;

        internal EcsTask(EcsTaskSource<T> source)
        {
            _source = source;
            _result = default!;
        }

        internal EcsTask(T result)
        {
            _source = null;
            _result = result;
        }

        public bool IsCompleted => _source == null || _source.IsCompleted;

        public Exception? Exception => _source?.Exception;

        public Awaiter GetAwaiter() => new Awaiter(_source, _result);

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly EcsTaskSource<T>? _source;
            private readonly T _result;

            internal Awaiter(EcsTaskSource<T>? source, T result)
            {
                _source = source;
                _result = result;
            }

            public bool IsCompleted => _source == null || _source.IsCompleted;

            public T GetResult() => _source == null ? _result : _source.GetResult();

            public void OnCompleted(Action continuation) => EcsTask.RegisterContinuation(_source, continuation);

            public void UnsafeOnCompleted(Action continuation) => EcsTask.RegisterContinuation(_source, continuation);
        }
    }
}
