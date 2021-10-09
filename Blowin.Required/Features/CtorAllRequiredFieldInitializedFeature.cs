using System.Collections.Generic;
using System.Linq;
using Blowin.Required.Extension;
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
            
            var closeBraceTokenLocation = constructorDeclarationSyntax.Body.CloseBraceToken.GetLocation();
            
            var initializationIfStore = new HashSet<string>();
            var initializationElseStore = new HashSet<string>();
            var invalidProperties = new HashSet<string>();
            
            var unreachableNodes = AllUnreachableNodes(constructorDeclarationSyntax.Body).ToHashSet();
            var ifStatements = constructorDeclarationSyntax.Body
                .DescendantNodes(n => !unreachableNodes.Contains(n))
                .OfType<IfStatementSyntax>();
        
            foreach (var ifStatementSyntax in ifStatements)
            {
                initializationIfStore.Clear();
                initializationElseStore.Clear();

                foreach (var propertyName in AllRequiredInIfInitialization(ifStatementSyntax.Statement, holderType, context.Operation.SemanticModel, unreachableNodes))
                    initializationIfStore.Add(propertyName);

                if (ifStatementSyntax.Else != null)
                {
                    foreach (var propertyName in AllRequiredInIfInitialization(ifStatementSyntax.Else, holderType, context.Operation.SemanticModel, unreachableNodes))
                        initializationElseStore.Add(propertyName);   
                }

                AddInvalidProperty(initializationIfStore, initializationElseStore, invalidProperties);
                AddInvalidProperty(initializationElseStore, initializationIfStore, invalidProperties);
            }
            
            foreach (var invalidProperty in invalidProperties)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptor, closeBraceTokenLocation, invalidProperty);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<SyntaxNode> AllUnreachableNodes(SyntaxNode node)
        {
            var nodesForVisit = new Queue<(int Level, SyntaxNode Node)>();
            foreach (var syntaxNode in node.ChildNodes())
                nodesForVisit.Enqueue((1, syntaxNode));

            while (nodesForVisit.Count > 0)
            {
                var (level, checkNode) = nodesForVisit.Dequeue();
                if (!(checkNode is ReturnStatementSyntax))
                {
                    var newLevel = level + 1;
                    foreach (var syntaxNode in checkNode.ChildNodes())
                        nodesForVisit.Enqueue((newLevel, syntaxNode));
                }
                else
                {
                    yield return checkNode;
                    while (nodesForVisit.Count > 0 && nodesForVisit.Peek().Level == level)
                        yield return nodesForVisit.Dequeue().Node;
                }
            }
        }

        private void AddInvalidProperty(HashSet<string> initializationFirstStore, HashSet<string> initializationSecondStore, 
            HashSet<string> invalidProperty)
        {
            foreach (var propertyName in initializationFirstStore)
            {
                if (initializationSecondStore.Contains(propertyName)) 
                    continue;

                invalidProperty.Add(propertyName);
            }
        }

        private IEnumerable<string> AllRequiredInIfInitialization(SyntaxNode node, ITypeSymbol holderType,
            SemanticModel model, HashSet<SyntaxNode> unreachableNodes)
        {
            return node
                .DescendantNodes(e => !(e is IfStatementSyntax) && !unreachableNodes.Contains(e))
                .OfType<AssignmentExpressionSyntax>()
                .Where(e => !unreachableNodes.Contains(e))
                .Select(e => model.GetSymbolInfo(e.Left).Symbol)
                .Where(e => e is IPropertySymbol propertySymbol && SymbolEqualityComparer.Default.Equals(holderType, propertySymbol.ContainingType) && propertySymbol.HasRequiredAttribute())
                .Select(e => e.Name);
        }
    }
}