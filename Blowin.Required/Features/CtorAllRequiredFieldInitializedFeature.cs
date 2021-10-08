using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blowin.Required.Features
{
    public class CtorAllRequiredFieldInitializedFeature : IFeature
    {
        public const string DiagnosticId = "RPF003";
 
        public DiagnosticDescriptor DiagnosticDescriptor { get; } = new DiagnosticDescriptor(DiagnosticId,
            "Type can't be used as generic parameter with new() restriction",
            "Type '{0}' can't be used as generic parameter with new() restriction", 
            "Feature", 
            DiagnosticSeverity.Error, 
            isEnabledByDefault: true);
        
        public void Register(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeCtor, OperationKind.ConstructorBody);
        }

        private void AnalyzeCtor(OperationAnalysisContext obj)
        {
            // TODO
        }
    }
}