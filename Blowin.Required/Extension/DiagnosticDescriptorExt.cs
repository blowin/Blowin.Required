using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Blowin.Required.Extension
{
    public static class DiagnosticDescriptorExt
    {
        public static Diagnostic ToDiagnostic(this DiagnosticDescriptor self, DiagnosticSeverity severity,
            Location location, params object[] parameters)
        {
            return  Diagnostic.Create(self, location, severity,
                Enumerable.Empty<Location>(), 
                ImmutableDictionary<string, string>.Empty, parameters);
        }
    }
}