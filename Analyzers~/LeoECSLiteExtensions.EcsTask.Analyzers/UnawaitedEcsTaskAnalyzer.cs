using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LeoECSLiteExtensions.Analyzers
{
    /// <summary>
    /// LEOECS105: an EcsTask call used as a statement, without await.
    /// <code>
    /// MoveToCover(ct);         // reported
    /// await MoveToCover(ct);   // fine
    /// _ = MoveToCover(ct);     // fine: explicit "fire and forget"
    /// </code>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnawaitedEcsTaskAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rules.UnawaitedEcsTask);

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
                    nodeContext => AnalyzeStatement(nodeContext, symbols),
                    SyntaxKind.ExpressionStatement);
            });
        }

        private static void AnalyzeStatement(SyntaxNodeAnalysisContext context, EcsTaskSymbols symbols)
        {
            var statement = (ExpressionStatementSyntax)context.Node;
            if (statement.Expression is not InvocationExpressionSyntax call)
            {
                return;
            }

            var resultType = context.SemanticModel.GetTypeInfo(call, context.CancellationToken).Type;
            if (!symbols.IsEcsTask(resultType))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rules.UnawaitedEcsTask,
                call.GetLocation(),
                call.Expression.ToString()));
        }
    }
}
