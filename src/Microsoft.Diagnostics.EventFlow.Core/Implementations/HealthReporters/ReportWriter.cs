using System;
using Validation;
using static Microsoft.Diagnostics.EventFlow.HealthReporters.CsvHealthReporter;

namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    internal class ReportWriter : IHealthReporter
    {
        private Action<HealthReportLevel, string, string> reportText;
        /// <summary>
        /// Create a report write delegate.
        /// </summary>
        /// <param name="action">The implementation to write logs. Sets to null when no action on report.</param>
        public ReportWriter(Action<HealthReportLevel, string, string> action)
        {
            reportText = Requires.NotNull(action, nameof(action));
        }

        public void Dispose()
        {
            // Put implementation in when necessary.
        }

        public void ReportHealthy(string description = null, string context = null)
        {
            this.reportText?.Invoke(HealthReportLevel.Message, description, context);
        }

        public void ReportProblem(string description, string context = null)
        {
            this.reportText?.Invoke(HealthReportLevel.Error, description, context);
        }

        public void ReportWarning(string description, string context = null)
        {
            this.reportText?.Invoke(HealthReportLevel.Warning, description, context);
        }
    }
}
