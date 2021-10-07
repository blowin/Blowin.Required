using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blowin.Required
{
    public static class Extension
    {
        public static bool HasRequiredAttribute(this IPropertySymbol self)
        {
            foreach (var attributeData in self.GetAttributes())
            {
                var attributeName = attributeData.AttributeClass?.Name;
                switch (attributeName)
                {
                    case null:
                        continue;
                    case "Required":
                    case "RequiredAttribute":
                        return true;
                }
            }

            return false;
        }
        
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