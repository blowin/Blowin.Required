using System.Collections.Generic;
using System.Linq;
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

            var closeBraceTokenLocation = constructorDeclarationSyntax.Body.CloseBraceToken.GetLocation();
            
            var initializationIfStore = new HashSet<string>();
            var initializationElseStore = new HashSet<string>();
            foreach (var ifStatementSyntax in constructorDeclarationSyntax.Body.DescendantNodes().OfType<IfStatementSyntax>())
            {
                initializationIfStore.Clear();
                initializationElseStore.Clear();

                foreach (var propertyName in AllRequiredInIfInitialization(ifStatementSyntax.Statement, context))
                    initializationIfStore.Add(propertyName);

                if (ifStatementSyntax.Else != null)
                {
                    foreach (var propertyName in AllRequiredInIfInitialization(ifStatementSyntax.Else, context))
                        initializationElseStore.Add(propertyName);   
                }

                ReportAllFail(context, initializationIfStore, initializationElseStore, closeBraceTokenLocation);
                ReportAllFail(context, initializationElseStore, initializationIfStore, closeBraceTokenLocation);
            }
        }

        private void ReportAllFail(OperationAnalysisContext context, 
            HashSet<string> initializationFirstStore,
            HashSet<string> initializationSecondStore, 
            Location closeBraceTokenLocation)
        {
            foreach (var propertyName in initializationFirstStore)
            {
                if (initializationSecondStore.Contains(propertyName)) 
                    continue;
                
                var diagnostic = Diagnostic.Create(DiagnosticDescriptor, closeBraceTokenLocation, propertyName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private IEnumerable<string> AllRequiredInIfInitialization(SyntaxNode node, OperationAnalysisContext context)
        {
            return node
                .DescendantNodes(e => !(e is IfStatementSyntax))
                .OfType<AssignmentExpressionSyntax>()
                .Select(e =>
                {
                    var symbol = context.Operation.SemanticModel.GetSymbolInfo(e.Left);
                    return symbol.Symbol;
                })
                .Where(e => e is IPropertySymbol propertySymbol && propertySymbol.HasRequiredAttribute())
                .Select(e => e.Name);
        }
    }
}