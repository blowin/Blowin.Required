using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Blowin.Required.Extension;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Blowin.Required.Features
{
    public sealed class RequiredInitializerFeature : IFeature
    {
        public const string DiagnosticId = "RPF001";

        public DiagnosticDescriptor DiagnosticDescriptor { get; } = new DiagnosticDescriptor(DiagnosticId,
            "Required property must be initialized",
            "Required property '{0}' must be initialized", 
            Constants.Category, 
            DiagnosticSeverity.Error, 
            isEnabledByDefault: true);
        
        public void Register(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        }
        
        private void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            if(!(context.Operation is IObjectCreationOperation objectCreationOperation))
                return;

            var ctorInitializedProperty = AllCtorInitializedProperty(objectCreationOperation.Constructor, context);

            var initializerProperty = AllInitializerProperty(objectCreationOperation, context);
            
            var notInitializedRequiredProperty = objectCreationOperation.Type
                .AllRequiredProperty()
                .Except(ctorInitializedProperty)
                .Except(initializerProperty)
                .Select(e => e.Identifier.Text);

            var errorMessage = string.Join(", ", notInitializedRequiredProperty).Trim();
            if(string.IsNullOrWhiteSpace(errorMessage))
                return;

            var diagnostic = Diagnostic.Create(DiagnosticDescriptor, 
                objectCreationOperation.Syntax.GetLocation(), 
                DiagnosticSeverity.Error,
                Enumerable.Empty<Location>(), 
                ImmutableDictionary<string, string>.Empty,
                errorMessage);
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
                
                var symbolInfo = ModelExtensions.GetSymbolInfo(semanticModel, assignmentOperationSyntax.Left);
                foreach (var propertyDeclarationSyntax in symbolInfo.Symbol.ToPropertyDeclarationSyntax())
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
                
                var (skipNodes, unreachableNodes) = constructorDeclarationSyntax.Body != null
                    ? RequiredPropertySyntaxNodeAnalyzer.Instance.NotAlwaysInitializedProperties(symbol.ContainingType, constructorDeclarationSyntax.Body, semanticModel)
                    : (null, null);
                
                foreach (var assignmentExpressionSyntax in body.DescendantNodes(e => (unreachableNodes == null || !unreachableNodes.Contains(e)) && !(e is AssignmentExpressionSyntax)).OfType<AssignmentExpressionSyntax>())
                {
                    if(unreachableNodes != null && unreachableNodes.Contains(assignmentExpressionSyntax))
                        continue;
                    
                    var symbolInfo = ModelExtensions.GetSymbolInfo(semanticModel, assignmentExpressionSyntax.Left);
                    if (!(symbolInfo.Symbol is IPropertySymbol propertySymbol) || (skipNodes != null && skipNodes.Contains(propertySymbol)))
                        continue;
                    
                    foreach (var propertyDeclarationSyntax in symbolInfo.Symbol.ToPropertyDeclarationSyntax())
                        yield return propertyDeclarationSyntax;
                }
            }
        }
    }
}