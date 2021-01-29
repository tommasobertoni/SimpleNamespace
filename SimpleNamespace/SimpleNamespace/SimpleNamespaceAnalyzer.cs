using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimpleNamespace
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SimpleNamespaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SimpleNamespace";

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
            nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Description = new LocalizableResourceString(
            nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeNamespace, SymbolKind.NamedType);
        }

        private static void AnalyzeNamespace(SymbolAnalysisContext context)
        {
            var typeSymbol = (INamedTypeSymbol)context.Symbol;

            if (!IsPublic(typeSymbol)) return;
            if (HasParentType(typeSymbol)) return;
            if (HasEasyNamespace(typeSymbol)) return;
            if (HasParentNamespace(typeSymbol)) return;

            var @namespace = typeSymbol.ContainingNamespace.ToString();
            var diagnosticLocation = typeSymbol.Locations[0];
            var diagnostic = Diagnostic.Create(Rule, diagnosticLocation, @namespace, typeSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsPublic(INamedTypeSymbol typeSymbol) =>
            typeSymbol.DeclaredAccessibility == Accessibility.Public ||
            typeSymbol.DeclaredAccessibility == Accessibility.Protected;

        private static bool HasParentType(INamedTypeSymbol typeSymbol) =>
            typeSymbol.ContainingType is not null;

        private static bool HasEasyNamespace(INamedTypeSymbol typeSymbol)
        {
            var namespaceSymbol = typeSymbol.ContainingNamespace;
            if (namespaceSymbol.IsGlobalNamespace) return true;

            var @namespace = namespaceSymbol.OriginalDefinition.ToString();
            return @namespace.IndexOf('.') == -1;
        }

        private static bool HasParentNamespace(INamedTypeSymbol typeSymbol)
        {
            var namespaceSymbol = typeSymbol.ContainingNamespace;
            var parentNamespaceSymbol = namespaceSymbol.ContainingNamespace;

            if (parentNamespaceSymbol is null) return false;

            // A namespace can be contained within another namespace in two ways:
            // as a dot-separated namespace:
            //   namespace Foo.Bar
            //   {
            //   }
            // 
            // as an explicit nested declaration:
            //   namespace Foo
            //   {
            //       namespace Bar
            //       {
            //       }
            //   }

            return IsNestedIn(child: namespaceSymbol, parent: parentNamespaceSymbol);
        }

        private static bool IsNestedIn(INamespaceSymbol child, INamespaceSymbol parent)
        {
            var childStartPosition = child.Locations[0].SourceSpan.Start;
            var parentEndPosition = parent.Locations[0].SourceSpan.End;
            return parentEndPosition + 1 /* the dot separating the tokens */ != childStartPosition;
        }
    }
}
