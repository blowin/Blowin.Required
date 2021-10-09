using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blowin.Required.Extension
{
    public static class SymbolExt
    {
        public static IEnumerable<PropertyDeclarationSyntax> ToPropertyDeclarationSyntax(this ISymbol self)
        {
            if(self == null)
                yield break;

            foreach (var symbolDeclaringSyntaxReference in self.DeclaringSyntaxReferences)
            {
                if (symbolDeclaringSyntaxReference.GetSyntax() is PropertyDeclarationSyntax propertyDeclarationSyntax)
                    yield return propertyDeclarationSyntax;
            }
        }
    }
}