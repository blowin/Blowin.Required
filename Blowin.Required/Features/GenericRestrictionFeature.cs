using System.Collections.Immutable;
using System.Linq;
using Blowin.Required.Extension;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blowin.Required.Features
{
    public sealed class GenericRestrictionFeature : IFeature
    {
        public const string DiagnosticId = "RPF002";
 
        public DiagnosticDescriptor DiagnosticDescriptor { get; } = new DiagnosticDescriptor(DiagnosticId,
            "Type can't be used as generic parameter with new() restriction",
            "Type '{0}' can't be used as generic parameter with new() restriction",
            "Feature",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        public void Register(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeTypeArgumentList, SyntaxKind.TypeArgumentList);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        /// <summary>
        /// Implicit generic parameter
        /// </summary> 
        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if(!(context.SemanticModel.GetSymbolInfo(context.Node).Symbol is IMethodSymbol methodSymbol))
                return;
            
            if(!methodSymbol.IsGenericMethod)
                return;
            
            foreach (var methodSymbolParameter in methodSymbol.Parameters)
            {
                //               ↓
                // void MyMethod<T>(T obj) where T : new() {}
                var typeParameterSymbol = methodSymbolParameter.OriginalDefinition?.Type as ITypeParameterSymbol;
                if(typeParameterSymbol == null)
                    continue;
                
                //                                    ↓
                // void MyMethod<T>(T obj) where T : new() {}
                if(!typeParameterSymbol.HasConstructorConstraint)
                    continue;

                if(methodSymbolParameter.Type == null)
                    continue;
                
                //  Type of parameter
                //           ↓
                // MyMethod(obj);
                if (methodSymbolParameter.Type.AllRequiredProperty().Any())
                {
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptor, context.Node.GetLocation(), methodSymbolParameter.Type.Name);
                    context.ReportDiagnostic(diagnostic);
                }
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
        
        private static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(TypeArgumentListSyntax typeArgumentListSyntax, 
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
    }
}