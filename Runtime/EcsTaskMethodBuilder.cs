#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace LeoECSLiteExtensions.Async
{
    /// Builder of <c>async EcsTask</c> methods. Where the method resumes is decided by
    /// <see cref="EcsStateMachineRunner"/>; this type only stores the result.
    public struct EcsTaskMethodBuilder
    {
        private EcsTaskSource _source;
        private EcsStateMachineRunner _runner;

        public static EcsTaskMethodBuilder Create() => new()
        {
            _runner = EcsStateMachineRunner.CreateForCurrentSystem(),
            _source = new EcsTaskSource()
        };

        public EcsTask Task => new EcsTask(_source);

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => _runner.Start(ref stateMachine);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void SetResult() => _source.SetResult();

        public void SetException(Exception exception) => _source.SetException(exception);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine => _runner.AwaitOnCompleted(ref awaiter, ref stateMachine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine => _runner.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }

    /// Builder of <c>async EcsTask&lt;T&gt;</c> methods. Same as <see cref="EcsTaskMethodBuilder"/>,
    /// but <see cref="SetResult"/> receives the returned value.
    public struct EcsTaskMethodBuilder<T>
    {
        private EcsTaskSource<T> _source;
        private EcsStateMachineRunner _runner;

        public static EcsTaskMethodBuilder<T> Create() => new()
        {
            _runner = EcsStateMachineRunner.CreateForCurrentSystem(),
            _source = new EcsTaskSource<T>()
        };

        public EcsTask<T> Task => new EcsTask<T>(_source);

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => _runner.Start(ref stateMachine);

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void SetResult(T result) => _source.SetResult(result);

        public void SetException(Exception exception) => _source.SetException(exception);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine => _runner.AwaitOnCompleted(ref awaiter, ref stateMachine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine => _runner.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
}
