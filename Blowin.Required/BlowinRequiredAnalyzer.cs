using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Blowin.Required.Features;

namespace Blowin.Required
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BlowinRequiredAnalyzer : DiagnosticAnalyzer
    {
        private static readonly IFeature[] Features = 
        {
            new RequiredInitializerFeature(),
            new GenericRestrictionFeature(),
            new CtorAllRequiredFieldInitializedFeature(),
        };
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();
                
                foreach (var feature in Features)
                    builder.Add(feature.DiagnosticDescriptor);
                
                return builder.ToImmutable();
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            foreach (var feature in Features)
                feature.Register(context);
        }
    }
}
