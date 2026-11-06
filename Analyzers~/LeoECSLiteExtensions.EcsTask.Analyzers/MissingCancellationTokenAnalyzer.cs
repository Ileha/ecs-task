using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LeoECSLiteExtensions.Analyzers
{
    /// <summary>
    /// LEOECS103: inside an EcsTask function, an awaited call could take the cancellation token but does not get it.
    /// <code>
    /// protected override async EcsTask RunAsync(CancellationToken ct)
    /// {
    ///     await UniTask.Delay(1000); // UniTask.Delay has an optional `cancellationToken` parameter
    ///     await Task.Delay(1000);    // Task.Delay(int, CancellationToken) exists
    /// }
    /// </code>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MissingCancellationTokenAnalyzer : DiagnosticAnalyzer
    {
        // A parameter named `_` says "I do not need the token", so nothing is reported for it.
        private const string DiscardName = "_";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rules.MissingCancellationToken);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var symbols = EcsTaskSymbols.TryCreate(compilationContext.Compilation);
                if (symbols == null)
                {
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeAwait(nodeContext, symbols),
                    SyntaxKind.AwaitExpression);
            });
        }

        private static void AnalyzeAwait(SyntaxNodeAnalysisContext context, EcsTaskSymbols symbols)
        {
            var awaitExpression = (AwaitExpressionSyntax)context.Node;
            var model = context.SemanticModel;

            // Only EcsTask code: its owner (AsyncRunSystem, EcsTask GOAP action) cancels this token.
            var function = AsyncCode.GetEnclosingFunction(awaitExpression, model);
            if (function == null || !symbols.IsEcsTask(function.ReturnType))
            {
                return;
            }

            var token = FindTokenInScope(awaitExpression, model, symbols);
            if (token == null)
            {
                return;
            }

            // Also looks into chains such as `await UniTask.Delay(1000).SuppressCancellationThrow()`.
            var calls = awaitExpression.Expression
                .DescendantNodesAndSelf(node => !AsyncCode.IsFunctionBoundary(node))
                .OfType<InvocationExpressionSyntax>();

            foreach (var call in calls)
            {
                if (model.GetOperation(call, context.CancellationToken) is IInvocationOperation invocation
                    && CanTakeToken(invocation, symbols))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rules.MissingCancellationToken,
                        call.Expression.GetLocation(),
                        call.Expression.ToString(),
                        token.Name));
                }
            }
        }

        /// <summary>A CancellationToken parameter of this function or of a function around it (a lambda can use the outer token).</summary>
        private static IParameterSymbol? FindTokenInScope(SyntaxNode node, SemanticModel model, EcsTaskSymbols symbols)
        {
            foreach (var function in node.Ancestors().Where(AsyncCode.IsFunctionBoundary))
            {
                var token = AsyncCode.GetFunctionSymbol(function, model)?.Parameters
                    .FirstOrDefault(parameter => symbols.IsCancellationToken(parameter.Type) && parameter.Name != DiscardName);

                if (token != null)
                {
                    return token;
                }
            }

            return null;
        }

        private static bool CanTakeToken(IInvocationOperation invocation, EcsTaskSymbols symbols)
        {
            // The compiler fills an omitted optional parameter with its default value.
            var skipsOptionalToken = invocation.Arguments.Any(argument =>
                argument.ArgumentKind == ArgumentKind.DefaultValue && symbols.IsCancellationToken(argument.Parameter?.Type));

            return skipsOptionalToken || HasOverloadWithToken(invocation.TargetMethod, symbols);
        }

        /// <summary>True for <c>Task.Delay(int)</c> because <c>Task.Delay(int, CancellationToken)</c> exists: same parameters plus one token.</summary>
        private static bool HasOverloadWithToken(IMethodSymbol method, EcsTaskSymbols symbols)
        {
            method = (method.ReducedFrom ?? method).OriginalDefinition;

            if (method.Parameters.Any(parameter => symbols.IsCancellationToken(parameter.Type)))
            {
                return false;
            }

            var parameterTypes = method.Parameters.Select(parameter => parameter.Type).ToList();

            return method.ContainingType.GetMembers(method.Name)
                .OfType<IMethodSymbol>()
                .Any(overload =>
                    overload.Parameters.Length == parameterTypes.Count + 1
                    && overload.Parameters.Count(parameter => symbols.IsCancellationToken(parameter.Type)) == 1
                    && overload.Parameters
                        .Where(parameter => !symbols.IsCancellationToken(parameter.Type))
                        .Select(parameter => parameter.Type)
                        .SequenceEqual(parameterTypes, SymbolEqualityComparer.Default));
        }
    }
}
