using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs.DiagnosticSource
{
    public sealed class DiagnosticSourceInputFactory : IPipelineItemFactory<DiagnosticSourceInput>
    {
        public DiagnosticSourceInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (healthReporter == null)
            {
                throw new ArgumentNullException(nameof(healthReporter));
            }

            return new DiagnosticSourceInput(GetSources(configuration, healthReporter), healthReporter);
        }

        private static IReadOnlyCollection<DiagnosticSourceConfiguration> GetSources(IConfiguration configuration, IHealthReporter healthReporter)
        {
            var sources = new List<DiagnosticSourceConfiguration>();

            var sourcesConfiguration = configuration.GetSection("sources");
            if (sourcesConfiguration == null)
            {
                healthReporter.ReportProblem($"{nameof(DiagnosticSourceInput)}: required configuration section 'sources' is missing");
                return sources;
            }

            try
            {
                sourcesConfiguration.Bind(sources);
            }
            catch
            {
                healthReporter.ReportProblem($"{nameof(DiagnosticSourceInput)}: configuration is invalid", EventFlowContextIdentifiers.Configuration);
            }

            return sources;
        }
    }
}