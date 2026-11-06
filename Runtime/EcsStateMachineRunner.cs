#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LeoECSLiteExtensions.Async
{
    /// Resume logic shared by <see cref="EcsTaskMethodBuilder"/> and <see cref="EcsTaskMethodBuilder{T}"/>.
    /// The builder, not the awaiter, decides where an async method resumes. Every continuation is
    /// wrapped so it runs inside the owning <see cref="EcsSystemSyncContext"/>, even for awaiters
    /// that ignore SynchronizationContext (UniTask, PlayerLoop, thread pool).
    internal struct EcsStateMachineRunner
    {
        private EcsSystemSyncContext _context;
        private IStateMachineBox? _box;

        public static EcsStateMachineRunner CreateForCurrentSystem()
        {
            if (SynchronizationContext.Current is not EcsSystemSyncContext context)
            {
                throw new InvalidOperationException(
                    $"{nameof(EcsTask)} methods can be called only while an {nameof(EcsSystemSyncContext)} is current (e.g. inside AsyncRunSystem.Run)");
            }

            return new EcsStateMachineRunner { _context = context };
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(GetResumeAction(ref stateMachine));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(GetResumeAction(ref stateMachine));
        }

        private Action GetResumeAction<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (_box == null)
            {
                var box = new StateMachineBox<TStateMachine>(_context);

                // This runner lives inside the builder, which lives inside the state machine. Set the box
                // first, so the copy stored in the box carries it too and later awaits reuse the same box.
                _box = box;
                box.StateMachine = stateMachine;
            }

            return _box.Resume;
        }

        private interface IStateMachineBox
        {
            Action Resume { get; }
        }

        private sealed class StateMachineBox<TStateMachine> : IStateMachineBox
            where TStateMachine : IAsyncStateMachine
        {
            private static readonly SendOrPostCallback MoveNextCallback =
                state => ((StateMachineBox<TStateMachine>)state!).StateMachine.MoveNext();

            private readonly EcsSystemSyncContext _context;

            public TStateMachine StateMachine = default!;

            public StateMachineBox(EcsSystemSyncContext context)
            {
                _context = context;
                Resume = ResumeInContext;
            }

            public Action Resume { get; }

            private void ResumeInContext()
            {
                // Completed by our own code during Run (nested EcsTask, a source set by the system,
                // a callback already drained from the queue): resume inline, no extra frame.
                if (_context.IsExecutingOnCurrentThread)
                {
                    StateMachine.MoveNext();
                    return;
                }

                _context.Post(MoveNextCallback, this);
            }
        }
    }
}
