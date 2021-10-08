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
        public const string DiagnosticObjectCreationRuleId = "BlowinRequired_Initializer";

        private static readonly DiagnosticDescriptor ObjectCreationRule = new DiagnosticDescriptor(DiagnosticObjectCreationRuleId,
            "Required property must be initialized",
            "Required property '{0}' must be initialized", 
            "Feature", 
            DiagnosticSeverity.Error, 
            isEnabledByDefault: true);
        
        public const string DiagnosticGenericRuleId = "BlowinRequired_GenericRestriction";

        private static readonly DiagnosticDescriptor GenericRule = new DiagnosticDescriptor(DiagnosticGenericRuleId,
            "Type can't be used as generic parameter with new() restriction",
            "Type '{0}' can't be used as generic parameter with new() restriction", 
            "Feature", 
            DiagnosticSeverity.Error, 
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();
                builder.Add(ObjectCreationRule);
                builder.Add(GenericRule);
                return builder.ToImmutable();
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
            context.RegisterSyntaxNodeAction(AnalyzeTypeArgumentList, SyntaxKind.TypeArgumentList);
            
            // TODO: Analyze initialization required fields
            //context.RegisterSyntaxNodeAction(AnalyzeCtor, SyntaxKind.ConstructorDeclaration);
        }

        private ImmutableArray<ITypeParameterSymbol> GetTypeParameters(TypeArgumentListSyntax typeArgumentListSyntax, 
            SyntaxNodeAnalysisContext context)
        {
            var parentSymbol = context.SemanticModel.GetSymbolInfo(typeArgumentListSyntax.Parent).Symbol;
            switch (parentSymbol)
            {
                case INamedTypeSymbol namedTypeSymbol:
                    return namedTypeSymbol.TypeParameters;
                case IMethodSymbol methodSymbol:
                    return methodSymbol.TypeParameters;
                default:
                    return ImmutableArray<ITypeParameterSymbol>.Empty;
            }
        }

        private void AnalyzeTypeArgumentList(SyntaxNodeAnalysisContext context)
        {
            if(!(context.Node is TypeArgumentListSyntax typeArgumentListSyntax))
                return;

            var array = GetTypeParameters(typeArgumentListSyntax, context);
            for (var i = 0; i < array.Length; i++)
            {
                var typeParameterSymbol = array[i];
                if(!typeParameterSymbol.HasConstructorConstraint)
                    continue;

                //                 ↓
                // GenericHolder<Person>
                var typeSyntax = typeArgumentListSyntax.Arguments[i];
                var genericType = context.SemanticModel.GetTypeInfo(typeSyntax).Type;
                if(genericType == null)
                    continue;
                
                if(!genericType.AllRequiredProperty().Any())
                    continue;
                
                var diagnostic = Diagnostic.Create(GenericRule, typeSyntax.GetLocation(), typeSyntax.ToString());
                context.ReportDiagnostic(diagnostic);
            }
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
                foreach (var assignmentExpressionSyntax in body.DescendantNodes(e => !(e is AssignmentExpressionSyntax)).OfType<AssignmentExpressionSyntax>())
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(assignmentExpressionSyntax.Left);
                    foreach (var propertyDeclarationSyntax in symbolInfo.Symbol.ToPropertyDeclarationSyntax())
                        yield return propertyDeclarationSyntax;
                }
            }
        }
    }
}
