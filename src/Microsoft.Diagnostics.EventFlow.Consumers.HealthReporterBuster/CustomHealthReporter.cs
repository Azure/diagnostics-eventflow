using System.IO;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Consumers.HealthReporterBuster
{
    internal class CustomHealthReporter : CsvHealthReporter
    {
        static volatile int count = 0;
        public CustomHealthReporter(IConfiguration configuration, StreamWriter streamWriter = null) : base(configuration, streamWriter)
        {
        }

        public override string GetReportFileName(string prefix)
        {
            return prefix + count++.ToString() + ".csv";
        }
    }
}
