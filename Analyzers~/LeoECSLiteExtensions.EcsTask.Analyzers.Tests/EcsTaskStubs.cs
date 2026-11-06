namespace LeoECSLiteExtensions.Analyzers.Tests
{
    /// <summary>
    /// Tiny copies of EcsTask and UniTask, with the same names and namespaces. No ECS types on purpose:
    /// the EcsTask rules must work in assemblies without LeoECS (e.g. GOAP actions).
    /// </summary>
    internal static class EcsTaskStubs
    {
        public const string Usings = @"
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LeoECSLiteExtensions.Async;
";

        public const string Source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LeoECSLiteExtensions.Async
{
    [AsyncMethodBuilder(typeof(EcsTaskMethodBuilder))]
    public readonly struct EcsTask
    {
        public TaskAwaiter GetAwaiter() => default;
    }

    public struct EcsTaskMethodBuilder
    {
        public static EcsTaskMethodBuilder Create() => default;
        public EcsTask Task => default;
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void SetResult() { }
        public void SetException(Exception exception) { }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    }

    [AsyncMethodBuilder(typeof(EcsTaskMethodBuilder<>))]
    public readonly struct EcsTask<T>
    {
        public TaskAwaiter<T> GetAwaiter() => default;
    }

    public struct EcsTaskMethodBuilder<T>
    {
        public static EcsTaskMethodBuilder<T> Create() => default;
        public EcsTask<T> Task => default;
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void SetResult(T result) { }
        public void SetException(Exception exception) { }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    }
}

// Stands in for UniTask: its token is an optional parameter. The snippets use Task as the
// non-EcsTask return type, which behaves the same for the analyzers.
namespace Cysharp.Threading.Tasks
{
    public static class UniTask
    {
        public static Task Delay(int milliseconds, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
";
    }
}
