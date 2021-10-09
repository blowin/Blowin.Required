using Microsoft.CodeAnalysis;

namespace Blowin.Required.Extension
{
    public static class PropertySymbolExt
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
    }
}