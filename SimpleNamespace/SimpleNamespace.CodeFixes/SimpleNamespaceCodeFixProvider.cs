using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimpleNamespace
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SimpleNamespaceCodeFixProvider)), Shared]
    public class SimpleNamespaceCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(SimpleNamespaceAnalyzer.DiagnosticId);

        public sealed override FixAllProvider? GetFixAllProvider() => null;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var action = CodeAction.Create(
                title: CodeFixResources.CodeFixTitle,
                createChangedDocument: _ => MakeSimpleNamespace(root, context),
                equivalenceKey: nameof(CodeFixResources.CodeFixTitle));

            var diagnostic = context.Diagnostics.First();

            context.RegisterCodeFix(action, diagnostic);
        }

        private Task<Document> MakeSimpleNamespace(
            SyntaxNode root,
            CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var namespaceDecl = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().First();

            // Compute new simple name.
            var @namespace = namespaceDecl.Name.ToString();
            var namespaceTokens = @namespace.Split('.');
            var newName = namespaceTokens[0];

            // Replace namespace declaration.
            var newNamespaceDecl = namespaceDecl.WithName(SyntaxFactory.IdentifierName(newName));
            var newRoot = root.ReplaceNode(namespaceDecl, newNamespaceDecl);

            var document = context.Document;
            var newDocument = document.WithSyntaxRoot(newRoot);

            return Task.FromResult(newDocument);
        }
    }
}
