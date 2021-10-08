using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blowin.Required.Features
{
    public sealed class GenericRestrictionFeature : IFeature
    {
        public const string DiagnosticId = "BlowinRequired_GenericRestriction";
 
        public DiagnosticDescriptor DiagnosticDescriptor { get; } = new DiagnosticDescriptor(DiagnosticId,
            "Type can't be used as generic parameter with new() restriction",
            "Type '{0}' can't be used as generic parameter with new() restriction", 
            "Feature", 
            DiagnosticSeverity.Error, 
            isEnabledByDefault: true);
        
        public void Register(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeTypeArgumentList, SyntaxKind.TypeArgumentList);
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
                
                var diagnostic = Diagnostic.Create(DiagnosticDescriptor, typeSyntax.GetLocation(), typeSyntax.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}