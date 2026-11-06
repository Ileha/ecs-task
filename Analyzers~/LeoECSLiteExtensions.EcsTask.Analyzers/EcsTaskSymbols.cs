using Microsoft.CodeAnalysis;

namespace LeoECSLiteExtensions.Analyzers
{
    /// <summary>
    /// Knows the EcsTask types. Created once per compilation. Needs no ECS: EcsTask also runs outside systems
    /// (e.g. GOAP actions).
    /// </summary>
    internal sealed class EcsTaskSymbols
    {
        private readonly INamedTypeSymbol _ecsTask;
        private readonly INamedTypeSymbol? _ecsTaskOfT;
        private readonly INamedTypeSymbol? _cancellationToken;

        private EcsTaskSymbols(Compilation compilation, INamedTypeSymbol ecsTask)
        {
            _ecsTask = ecsTask;
            _ecsTaskOfT = compilation.GetTypeByMetadataName("LeoECSLiteExtensions.Async.EcsTask`1");
            _cancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        }

        /// <summary>Returns null when the assembly does not reference EcsTask: there is nothing to check.</summary>
        public static EcsTaskSymbols? TryCreate(Compilation compilation)
        {
            var ecsTask = compilation.GetTypeByMetadataName("LeoECSLiteExtensions.Async.EcsTask");
            return ecsTask == null ? null : new EcsTaskSymbols(compilation, ecsTask);
        }

        /// <summary>True for <c>EcsTask</c> and <c>EcsTask&lt;T&gt;</c>.</summary>
        public bool IsEcsTask(ITypeSymbol? type) => IsSameType(type, _ecsTask) || IsSameType(type, _ecsTaskOfT);

        public bool IsCancellationToken(ITypeSymbol? type) => IsSameType(type, _cancellationToken);

        private static bool IsSameType(ITypeSymbol? type, INamedTypeSymbol? expected) =>
            expected != null && SymbolEqualityComparer.Default.Equals(type?.OriginalDefinition, expected);
    }
}
