using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RealGoodApps.CarefulEFCore.Analyzer
{
    /// <summary>
    /// A diagnostic analyzer that implements companion types for methods and classes.
    /// When using LINQ/EF Core extension methods, there is an inconsistency in the query re-writing engine for EF Core
    /// which behaves strangely when you are passing in an expression that is not either a compile-time lambda or a simple variable
    /// expression. This analyzer will trigger errors when these are detected and they can be remediated simply by assigning
    /// the expression to a variable first.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer : DiagnosticAnalyzer
    {
        private static readonly List<string> ClassList = new List<string>
        {
            "System.Linq.Enumerable",
            "System.Linq.Queryable",
            "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions",
        };

        private const string ExpressionFullyQualifiedTypeName = "System.Linq.Expressions.Expression";

        /// <summary>
        /// The diagnostic error that is triggered when a method call occurs with a dangerous expression usage.
        /// </summary>
        private static readonly DiagnosticDescriptor DangerousExpressionUsage =
            new DiagnosticDescriptor(
                "CE0001",
                "Dangerous Expression Usage",
                "The expression usage is dangerous, extract to a variable for consistent results with EF core",
                "Logic",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                "The queryable expressions used with Where and Select in EF Core should either be compiler-time lambdas or simple variable references to provide the most consistent results with the query re-writing engine. You may fix this by extracting the expression to a variable prior to calling these methods.");

        /// <summary>
        /// Our analyzer's diagnostic descriptors (which are basically our custom warnings/errors we can trigger).
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DangerousExpressionUsage);

        /// <summary>
        /// Initialize our diagnostic analyzer.
        /// The important part here is that we register the appropriate callbacks that are to be executed when certain
        /// types of operations are encountered in analysis.
        /// </summary>
        /// <param name="context">An instance of <see cref="AnalysisContext"/>.</param>
        public override void Initialize(AnalysisContext context)
        {
            // To be honest, I'm not entirely sure what these options do but they were in both of the examples.
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            // Register our callbacks for the code we want to analyze.
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        /// <summary>
        /// Analyze a method invocation.
        /// We only care about operations that are instances of <see cref="IInvocationOperation"/>.
        /// </summary>
        /// <param name="context">An instance of <see cref="OperationAnalysisContext"/>.</param>
        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (!(context.Operation is IInvocationOperation invocationOperation))
            {
                return;
            }

            var fullyQualifiedClassName = GetFullyQualifiedClassName(invocationOperation.TargetMethod.ContainingType);

            if (!ClassList
                .Any(f => f.Equals(fullyQualifiedClassName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return;
            }

            for (var argumentIndex = 1; argumentIndex < invocationOperation.Arguments.Length; argumentIndex++)
            {
                var parameter = invocationOperation.Arguments[argumentIndex];

                var fullyQualifiedParameterType = GetFullyQualifiedClassName(parameter.Parameter.Type);

                if (fullyQualifiedParameterType != ExpressionFullyQualifiedTypeName)
                {
                    continue;
                }

                if (CheckForDangerousExpressionUsage(parameter))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DangerousExpressionUsage,
                        context.Operation.Syntax.GetLocation()));

                    return;
                }
            }
        }

        private static bool CheckForDangerousExpressionUsage(
            IArgumentOperation argument)
        {
            if (argument == null)
            {
                return false;
            }

            var paramKind = argument.Value?.Kind;

            return paramKind != OperationKind.Conversion
                   && paramKind != OperationKind.LocalReference
                   && paramKind != OperationKind.ParameterReference;
        }

        private static string GetFullyQualifiedClassName(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return null;
            }

            var symbolDisplayFormat = new SymbolDisplayFormat(
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            return typeSymbol.ToDisplayString(symbolDisplayFormat);
        }
    }
}
