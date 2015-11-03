using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DncAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DncAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DNC01";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberExpresion = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpresion?.Name?.ToString() != "Match") return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberExpresion).Symbol;
            if (memberSymbol?.ToString() != "System.Text.RegularExpressions.Regex.Match(string, string)") return;

            var argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
            if ((argumentList?.Arguments.Count ?? 0) != 2) return;

            var regexLiteral = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;
            if (regexLiteral == null) return;

            var regexOpt = context.SemanticModel.GetConstantValue(regexLiteral);
            var regex = regexOpt.Value as string;

            try
            {
                System.Text.RegularExpressions.Regex.Match("", regex);
            }
            catch (ArgumentException e)
            {
                var diag = Diagnostic.Create(Rule, regexLiteral.GetLocation(), e.Message);
                context.ReportDiagnostic(diag);
            }
        }
    }
}
