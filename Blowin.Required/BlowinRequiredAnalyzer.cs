using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
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
            "Required property must be initialized",
            "Required property '{0}' must be initialized", 
            "Feature", 
            DiagnosticSeverity.Error, 
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ObjectCreationRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);

            // TODO validate generic
            //context.RegisterSyntaxNodeAction(AnalyzeCtor, SyntaxKind.ConstructorDeclaration);
        }

        private void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            if(!(context.Operation is IObjectCreationOperation objectCreationOperation))
                return;

            var ctorInitializedProperty = AllCtorInitializedProperty(objectCreationOperation.Constructor, context);

            var initializerProperty = AllInitializerProperty(objectCreationOperation, context);
            
            var notInitializedRequiredProperty = objectCreationOperation.Type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => HasRequiredAttribute(p))
                .SelectMany(p => ToPropertyDeclarationSyntax(p))
                .Except(ctorInitializedProperty)
                .Except(initializerProperty)
                .Select(e => e.Identifier.Text);

            var errorMessage = string.Join(", ", notInitializedRequiredProperty).Trim();
            if(string.IsNullOrWhiteSpace(errorMessage))
                return;

            var diagnostic = Diagnostic.Create(ObjectCreationRule, objectCreationOperation.Syntax.GetLocation(), errorMessage);
            context.ReportDiagnostic(diagnostic);
        }

        private IEnumerable<PropertyDeclarationSyntax> AllInitializerProperty(IObjectCreationOperation objectCreationOperation, OperationAnalysisContext context)
        {
            if(objectCreationOperation.Initializer == null)
                yield break;
            
            var semanticModel = context.Compilation.GetSemanticModel(objectCreationOperation.Syntax.SyntaxTree);
            
            foreach (var assignmentOperation in objectCreationOperation.Initializer.Initializers.OfType<IAssignmentOperation>())
            {
                if(!(assignmentOperation.Syntax is AssignmentExpressionSyntax assignmentOperationSyntax))
                    continue;
                
                var symbolInfo = semanticModel.GetSymbolInfo(assignmentOperationSyntax.Left);
                foreach (var propertyDeclarationSyntax in ToPropertyDeclarationSyntax(symbolInfo.Symbol))
                    yield return propertyDeclarationSyntax;
            }
        }

        private IEnumerable<PropertyDeclarationSyntax> AllCtorInitializedProperty(IMethodSymbol symbol, OperationAnalysisContext context)
        {
            foreach (var symbolDeclaringSyntaxReference in symbol.DeclaringSyntaxReferences)
            {
                if(!(symbolDeclaringSyntaxReference.GetSyntax(context.CancellationToken) is ConstructorDeclarationSyntax constructorDeclarationSyntax))
                    continue;

                var body = constructorDeclarationSyntax.Body ?? (CSharpSyntaxNode)constructorDeclarationSyntax.ExpressionBody;
                if(body == null)
                    continue;
                
                var semanticModel = context.Compilation.GetSemanticModel(constructorDeclarationSyntax.Parent.SyntaxTree);
                foreach (var assignmentExpressionSyntax in body.DescendantNodes(e => !(e is AssignmentExpressionSyntax)).OfType<AssignmentExpressionSyntax>())
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(assignmentExpressionSyntax.Left);
                    foreach (var propertyDeclarationSyntax in ToPropertyDeclarationSyntax(symbolInfo.Symbol))
                        yield return propertyDeclarationSyntax;
                }
            }
        }

        private IEnumerable<PropertyDeclarationSyntax> ToPropertyDeclarationSyntax(ISymbol symbol)
        {
            if(symbol == null)
                yield break;

            foreach (var symbolDeclaringSyntaxReference in symbol.DeclaringSyntaxReferences)
            {
                if (symbolDeclaringSyntaxReference.GetSyntax() is PropertyDeclarationSyntax propertyDeclarationSyntax)
                    yield return propertyDeclarationSyntax;
            }
        }

        private static bool HasRequiredAttribute(IPropertySymbol symbol)
        {
            return symbol.GetAttributes().Any(e =>
            {
                var attributeName = e.AttributeClass?.Name;
                return attributeName != null && (
                    attributeName.Equals("Required") || attributeName.Equals("RequiredAttribute")
                );
            });
        }
    }
}
