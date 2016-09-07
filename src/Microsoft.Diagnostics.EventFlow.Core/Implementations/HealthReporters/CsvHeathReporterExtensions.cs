using Microsoft.Extensions.Configuration;


namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    internal static class CsvHeathReporterExtensions
    {
        public static CsvHealthReporterConfiguration ToCsvHealthReporterConfiguration(this IConfiguration configuration)
        {
            Validation.Requires.NotNull(configuration, nameof(configuration));
            CsvHealthReporterConfiguration boundConfiguration = new CsvHealthReporterConfiguration();
            configuration.Bind(boundConfiguration);
            return boundConfiguration;
        }
    }
}
