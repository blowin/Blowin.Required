using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blowin.Required.Extension
{
    public static class TypeSymbolExt
    {
        public static IEnumerable<PropertyDeclarationSyntax> AllRequiredProperty(this ITypeSymbol self)
        {
            if (self == null)
                return Enumerable.Empty<PropertyDeclarationSyntax>();
            
            return self.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.HasRequiredAttribute())
                .SelectMany(p => p.ToPropertyDeclarationSyntax());
        }
    }
}