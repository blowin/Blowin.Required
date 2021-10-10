using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blowin.Required.Features
{
    public class CtorAllRequiredFieldInitializedFeature : IFeature
    {
        public const string DiagnosticId = "RPF003";
 
        public DiagnosticDescriptor DiagnosticDescriptor { get; } = new DiagnosticDescriptor(DiagnosticId,
            "Constructor contain initialization of required property",
            "If constructor initialization of required property '{0}', it should be initialized in any execution path", 
            "Feature", 
            DiagnosticSeverity.Error, 
            isEnabledByDefault: true);
        
        public void Register(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeCtor, OperationKind.ConstructorBody);
        }
        
        private void AnalyzeCtor(OperationAnalysisContext context)
        {
            if(!(context.Operation.Syntax is ConstructorDeclarationSyntax constructorDeclarationSyntax))
                return;
            
            if(constructorDeclarationSyntax.Body == null)
                return;
            
            var holderType = context.ContainingSymbol.ContainingType as ITypeSymbol;
            var (invalidProperties, _) = RequiredPropertySyntaxNodeAnalyzer.Instance.NotAlwaysInitializedProperties(holderType, 
                constructorDeclarationSyntax.Body,
                context.Operation.SemanticModel);
           
            var closeBraceTokenLocation = constructorDeclarationSyntax.Body.CloseBraceToken.GetLocation();
            foreach (var invalidProperty in invalidProperties)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptor, closeBraceTokenLocation, invalidProperty.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}