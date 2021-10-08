using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Blowin.Required.Features
{
    public interface IFeature
    {
        DiagnosticDescriptor DiagnosticDescriptor { get; }
        
        void Register(AnalysisContext context);
    }
}