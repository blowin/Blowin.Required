using System.Collections.Generic;
using System.Linq;
using Blowin.Required.Extension;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blowin.Required
{
    public class RequiredPropertySyntaxNodeAnalyzer
    {
        public static RequiredPropertySyntaxNodeAnalyzer Instance { get; } =
            new RequiredPropertySyntaxNodeAnalyzer();
        
        public (HashSet<IPropertySymbol> NotInitialized, HashSet<SyntaxNode> UnreachableNodes) NotAlwaysInitializedProperties(ITypeSymbol holderType, SyntaxNode analyzeNode, SemanticModel semanticModel)
        {
            var unreachableNodes = AllUnreachableNodes(analyzeNode).ToHashSet();
            
            var initializationIfStore = new HashSet<IPropertySymbol>();
            var initializationElseStore = new HashSet<IPropertySymbol>();
            var invalidProperties = new HashSet<IPropertySymbol>();
            
            var ifStatements = analyzeNode
                .DescendantNodes(n => !unreachableNodes.Contains(n))
                .OfType<IfStatementSyntax>();
        
            foreach (var ifStatementSyntax in ifStatements)
            {
                initializationIfStore.Clear();
                initializationElseStore.Clear();

                foreach (var propertyName in AllRequiredInitializationNames(ifStatementSyntax.Statement))
                    initializationIfStore.Add(propertyName);

                if (ifStatementSyntax.Else != null)
                {
                    foreach (var propertyName in AllRequiredInitializationNames(ifStatementSyntax.Else))
                        initializationElseStore.Add(propertyName);   
                }

                AddInvalidProperty(initializationIfStore, initializationElseStore);
                AddInvalidProperty(initializationElseStore, initializationIfStore);
            }

            return (invalidProperties, unreachableNodes);

            IEnumerable<IPropertySymbol> AllRequiredInitializationNames(SyntaxNode node)
                => AllRequiredInitialization(node, holderType, semanticModel, unreachableNodes);
            
            void AddInvalidProperty(HashSet<IPropertySymbol> initializationFirstStore, HashSet<IPropertySymbol> initializationSecondStore)
            {
                foreach (var propertyName in initializationFirstStore)
                {
                    if (initializationSecondStore.Contains(propertyName)) 
                        continue;

                    invalidProperties.Add(propertyName);
                }
            }
        }
        
        private static IEnumerable<IPropertySymbol> AllRequiredInitialization(SyntaxNode node, ITypeSymbol holderType,
            SemanticModel model, HashSet<SyntaxNode> unreachableNodes)
        {
            return node
                .DescendantNodes(e => !(e is IfStatementSyntax) && !unreachableNodes.Contains(e))
                .OfType<AssignmentExpressionSyntax>()
                .Where(e => !unreachableNodes.Contains(e))
                .Select(e => model.GetSymbolInfo(e.Left).Symbol)
                .OfType<IPropertySymbol>()
                .Where(e => SymbolEqualityComparer.Default.Equals(holderType, e.ContainingType) && e.HasRequiredAttribute());
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
    }
}