using Microsoft.CodeAnalysis;

namespace LeoECSLiteExtensions.Analyzers
{
    /// <summary>
    /// EcsTask rules that need no ECS. They share the LEOECS 1xx async group with the ECS rules in
    /// com.alexey-developer89.ecs-async-system. See README.md for examples.
    /// </summary>
    internal static class Rules
    {
        private const string Category = "LeoECS.Async";

        public static readonly DiagnosticDescriptor MissingCancellationToken = new DiagnosticDescriptor(
            id: "LEOECS103",
            title: "Awaited call without the cancellation token",
            messageFormat: "'{0}' can take a CancellationToken. Pass '{1}', so the wait stops when its owner cancels it.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The owner of an EcsTask cancels its token: AsyncRunSystem on destroy, an EcsTask GOAP action " +
                         "when the action ends. A wait without the token stays pending, and its finally blocks never run.");

        public static readonly DiagnosticDescriptor UnawaitedEcsTask = new DiagnosticDescriptor(
            id: "LEOECS105",
            title: "EcsTask is not awaited",
            messageFormat: "The EcsTask from '{0}' is not awaited. Its exceptions are lost and the code below does not " +
                           "wait for it. Await it, or write '_ = ...' if this is on purpose.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
