using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Blowin.Required
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BlowinRequiredAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BlowinRequired";

        private static readonly DiagnosticDescriptor ObjectCreationRule = new DiagnosticDescriptor(DiagnosticId,
            "Type name contains lowercase letters",
            "Type name '{0}' contains lowercase letters", 
            "Feature", 
            DiagnosticSeverity.Warning, 
            isEnabledByDefault: true, 
            description: "Type names should be all uppercase.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ObjectCreationRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        }

        private void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            if(!(context.Operation is IObjectCreationOperation objectCreationOperation))
                return;

            var has = HasCtorInitializationRequiredProperty(objectCreationOperation.Constructor,
                context.CancellationToken);
            
            var properties = objectCreationOperation.Type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => HasRequiredAttribute(p))
                .ToArray();
        }

        private bool HasCtorInitializationRequiredProperty(IMethodSymbol symbol, CancellationToken token)
        {
            foreach (var symbolDeclaringSyntaxReference in symbol.DeclaringSyntaxReferences)
            {
                if(!(symbolDeclaringSyntaxReference.GetSyntax(token) is ConstructorDeclarationSyntax constructorDeclarationSyntax))
                    continue;

                var body = constructorDeclarationSyntax.Body ?? (CSharpSyntaxNode)constructorDeclarationSyntax.ExpressionBody;
                if(body == null)
                    continue;

                var allAssignments = body.DescendantNodes(e => !(e is AssignmentExpressionSyntax))
                    .OfType<AssignmentExpressionSyntax>()
                    .ToArray();
            }

            return false;
        }
        
        private static bool HasRequiredAttribute(IPropertySymbol symbol)
        {
            return symbol.GetAttributes().Any(e =>
            {
                var attributeName = e.AttributeClass?.Name;
                return attributeName != null && attributeName.Equals("Required");
            });
        }
        
        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(ObjectCreationRule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
