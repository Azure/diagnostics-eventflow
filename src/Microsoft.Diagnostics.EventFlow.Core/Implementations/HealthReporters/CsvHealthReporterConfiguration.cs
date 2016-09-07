namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    public class CsvHealthReporterConfiguration
    {
        public string LogFileFolder { get; set; }
        public string LogFilePrefix { get; set; }
        public string MinReportLevel { get; set; }
        public int? ThrottlingPeriodMsec { get; set; }
    }
}
