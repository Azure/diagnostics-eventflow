using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthReporters;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class CsvFileHealthReporterTests
    {
        [Fact(DisplayName = "CsvFileHealthReporter constructor should require configure file path")]
        public void ConstructorShouldRequireConfigFile()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                CsvFileHealthReporter target = new CsvFileHealthReporter(configurationFilePath: null);
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: configurationFilePath", ex.Message);
        }

        [Fact(DisplayName = "CsvFileHealthReporter handles invalid filter level")]
        public void ConfigureWrongFilterLevel()
        {
            string logFileKey = "healthReporter:logFilePath";
            string logLevelKey = "healthReporter:logLevel";
            string healthReporter = "HealthReport.csv";

            // Setup
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c[logLevelKey]).Returns("WrongLevel");
            configMock.Setup(c => c[logFileKey]).Returns(healthReporter);

            // Exercise
            try
            {
                using (Stream memoryStream = new MemoryStream())
                {
                    var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                    using (CsvFileHealthReporter target = new CsvFileHealthReporter(configMock.Object, streamWriterMock.Object))
                    {
                        // Verify
                        streamWriterMock.Verify(
                            s => s.WriteLine(
                                It.Is<string>(msg => msg.EndsWith("Log level parse fail. Please check the value of: WrongLevel."))),
                            Times.Exactly(1));
                    }
                }
            }
            finally
            {
                // Clean
                if (File.Exists(healthReporter))
                {
                    File.Delete(healthReporter);
                }
            }
        }
    }
}
