// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class CsvHealthReporterTests
    {
        [Fact]
        public void ConstructorShouldRequireConfigFile()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                CsvHealthReporter target = new CsvHealthReporter(configurationFilePath: null);
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: configurationFilePath", ex.Message);
        }

        [Fact]
        public void ConstructorShouldAcceptOptinalStreamWriter()
        {
            var configurationMock = new Mock<IConfiguration>();
            using (MemoryStream ms = new MemoryStream())
            {
                var streamWriterMock = new Mock<StreamWriter>(ms);
                // When stream wrtier is providered.
                CsvHealthReporter target;
                using (target = new CsvHealthReporter(configurationMock.Object, streamWriterMock.Object))
                {
                    Assert.NotNull(target);
                }
                // When stream writer is not providered.
                try
                {
                    using (target = new CsvHealthReporter(configurationMock.Object))
                    {
                        Assert.NotNull(target);
                    }
                }
                finally
                {
                    TryDeleteFiles(CsvHealthReporter.DefaultHealthReporterPrefix);
                }
            }
        }

        [Fact]
        public void ConstructorShouldAcceptOptionalStreamWriter2()
        {
            using (TemporaryFile configFile = new TemporaryFile())
            {
                configFile.Write("{}");
                using (MemoryStream ms = new MemoryStream())
                {
                    var streamWriterMock = new Mock<StreamWriter>(ms);
                    // When stream wrtier is providered.
                    CsvHealthReporter target;
                    using (target = new CsvHealthReporter(configFile.FilePath, streamWriterMock.Object))
                    {
                        Assert.NotNull(target);
                    }

                    // When stream writer is not providered.
                    try
                    {
                        using (target = new CsvHealthReporter(configFile.FilePath))
                        {
                            Assert.NotNull(target);
                        }
                    }
                    finally
                    {
                        // Clean up
                        TryDeleteFiles(CsvHealthReporter.DefaultHealthReporterPrefix);
                    }
                }
            }
        }

        // TEMPORARILY DISABLED [Fact]
        public void ConstructorShouldHandleWrongFilterLevel()
        {
            // Setup
            string logFileFolderKey = "LogFileFolder";
            string logFilePrefixKey = "LogFilePrefix";
            string logLevelKey = "MinReportLevel";
            string healthReporter = "HealthReport";

            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { logFileFolderKey, string.Empty },
                { logFilePrefixKey, healthReporter},
                { logLevelKey, "WrongLevel"}
            }).Build();

            // Exercise
            using (Stream memoryStream = new MemoryStream())
            {
                var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                using (CsvHealthReporter target = new CsvHealthReporter(configuration, streamWriterMock.Object))
                {
                    // Verify
                    streamWriterMock.Verify(
                        s => s.WriteLine(
                            It.Is<string>(msg => msg.EndsWith("Failed to parse log level. Please check the value of: WrongLevel.\r\n"))),
                        Times.Exactly(1));
                }
            }
        }

        // TEMPORARILY DISABLED [Fact]
        public void ReportHealthyShouldWriteMessage()
        {
            // Setup
            string logFileFolderKey = "LogFileFolder";
            string logFilePrefixKey = "LogFilePrefix";
            string logLevelKey = "MinReportLevel";
            string healthReporter = "HealthReport";

            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { logFileFolderKey, string.Empty },
                { logFilePrefixKey, healthReporter},
                { logLevelKey, "Message"}
            }).Build();

            // Exercise
            try
            {
                using (Stream memoryStream = new MemoryStream())
                {
                    var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                    using (CsvHealthReporter target = new CsvHealthReporter(configuration, streamWriterMock.Object))
                    {
                        target.ReportHealthy("Healthy message.", "UnitTest");
                        // Verify
                        streamWriterMock.Verify(
                            s => s.WriteLine(
                                It.Is<string>(msg => msg.Contains("UnitTest,Message,Healthy message."))),
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

        [Fact]
        public void ReportWarningShouldWriteWarning()
        {
            // Setup
            string logFileFolderKey = "LogFileFolder";
            string logFilePrefixKey = "LogFilePrefix";
            string logLevelKey = "MinReportLevel";
            string healthReporter = "HealthReport";

            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { logFileFolderKey, string.Empty },
                { logFilePrefixKey, healthReporter},
                { logLevelKey, "Message"}
            }).Build();

            // Exercise
            using (Stream memoryStream = new MemoryStream())
            {
                var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                using (CsvHealthReporter target = new CsvHealthReporter(configuration, streamWriterMock.Object))
                {
                    target.ReportWarning("Warning message.", "UnitTest");
                    // Verify
                    streamWriterMock.Verify(
                        s => s.WriteLine(
                            It.Is<string>(msg => msg.Contains("UnitTest,Warning,Warning message."))),
                        Times.Exactly(1));
                }
            }
        }

        // TEMPORARILY DISABLED [Fact]
        public void ReportProblemShouldWriteError()
        {
            string logFileKey = "healthReporter:logFilePath";
            string logLevelKey = "healthReporter:logLevel";
            string healthReporter = "HealthReport.csv";

            // Setup
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c[logLevelKey]).Returns("Message");
            configMock.Setup(c => c[logFileKey]).Returns(healthReporter);

            // Exercise
            using (Stream memoryStream = new MemoryStream())
            {
                var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                using (CsvHealthReporter target = new CsvHealthReporter(configMock.Object, streamWriterMock.Object))
                {
                    target.ReportProblem("Error message.", "UnitTest");
                    // Verify
                    streamWriterMock.Verify(
                        s => s.WriteLine(
                            It.Is<string>(msg => msg.Contains("UnitTest,Error,Error message."))),
                        Times.Exactly(1));
                }
            }
        }

        // TEMPORARILY DISABLED [Fact]
        public void ReporterShouldFilterOutMessage()
        {
            // Setup
            string logFileFolderKey = "LogFileFolder";
            string logFilePrefixKey = "LogFilePrefix";
            string logLevelKey = "MinReportLevel";
            string healthReporter = "HealthReport";

            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { logFileFolderKey, string.Empty },
                { logFilePrefixKey, healthReporter},
                { logLevelKey, "Warning"}
            }).Build();

            // Exercise
            using (Stream memoryStream = new MemoryStream())
            {
                var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                using (CsvHealthReporter target = new CsvHealthReporter(configuration, streamWriterMock.Object))
                {
                    target.ReportHealthy("Supposed to be filtered.", "UnitTest");
                    // Verify that message is filtered out.
                    streamWriterMock.Verify(
                        s => s.WriteLine(
                            It.IsAny<string>()),
                        Times.Exactly(0));

                    // Verify that warning is not filtered out.
                    target.ReportWarning("Warning message", "UnitTests");
                    streamWriterMock.Verify(
                        s => s.WriteLine(
                            It.IsAny<string>()),
                        Times.Exactly(1));

                    // Verify that error is not filtered out.
                    target.ReportWarning("Error message", "UnitTests");
                    streamWriterMock.Verify(
                        s => s.WriteLine(
                            It.IsAny<string>()),
                        Times.Exactly(2));
                }
            }
        }

        [Fact]
        public void ShouldSetExternalStreamTrue()
        {
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            using (Stream memoryStream = new MemoryStream())
            using (CsvHealthReporter target = new CsvHealthReporter(configMock.Object, new Mock<StreamWriter>(memoryStream).Object))
            {
                Assert.True(target.IsExternalStreamWriter);
            }
        }

        [Fact]
        public void ShouldSetExternalStreamFalse()
        {
            try
            {
                Mock<IConfiguration> configMock = new Mock<IConfiguration>();
                using (CsvHealthReporter target = new CsvHealthReporter(configMock.Object))
                {
                    Assert.False(target.IsExternalStreamWriter);
                }
            }
            finally
            {
                TryDeleteFiles(CsvHealthReporter.DefaultHealthReporterPrefix);
            }
        }

        [Fact]
        public void ShouldSetExternalStream()
        {
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            using (Stream memoryStream = new MemoryStream())
            {
                Mock<StreamWriter> streamWriterMock = new Mock<StreamWriter>(memoryStream);
                using (CsvHealthReporter target = new CsvHealthReporter(configMock.Object, streamWriterMock.Object))
                {
                    Assert.Same(streamWriterMock.Object, target.StreamWriter);
                }
            }
        }

        [Theory]
        [InlineData("Message")]
        [InlineData("Warning")]
        [InlineData("Error")]
        public void ShouldParseConfigfileCorrect(string level)
        {
            string configJsonString = @"
{
  ""noise"": [
    {
      ""type"": ""EventSource"",
      ""sources"": [
        { ""providerName"": ""Microsoft-ServiceFabric-Services"" },
        { ""providerName"": ""MyCompany-AirTrafficControlApplication-Frontend"" }
      ]
    }
  ],
  ""healthReporter"": {
    ""logFileFolder"": ""..\\App_Data\\"",
    ""logFilePrefix"": ""TestHealthReport"",
    ""minReportLevel"": ""@Level""
  }
}
";
            configJsonString = configJsonString.Replace("@Level", level);
            using (var configFile = new TemporaryFile())
            using (var ms = new MemoryStream())
            {
                configFile.Write(configJsonString);
                Mock<StreamWriter> streamWriterMock = new Mock<StreamWriter>(ms);
                using (CsvHealthReporter target = new CsvHealthReporter(configFile.FilePath, streamWriterMock.Object))
                {
                    Assert.Equal(level, target.LogLevel.ToString());
                }
            }
        }

        // TEMPORARILY DISABLED [Fact]
        public void ShouldEscapeCommaInMessage()
        {
            string logFileKey = "healthReporter:logFilePath";
            string logLevelKey = "healthReporter:logLevel";
            string healthReporter = "HealthReport.csv";

            // Setup
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c[logLevelKey]).Returns("Message");
            configMock.Setup(c => c[logFileKey]).Returns(healthReporter);

            // Exercise
            using (Stream memoryStream = new MemoryStream())
            {
                var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                using (CsvHealthReporter target = new CsvHealthReporter(configMock.Object, streamWriterMock.Object))
                {
                    target.ReportProblem("Error message, with comma.", "UnitTest");
                    // Verify
                    streamWriterMock.Verify(
                        s => s.WriteLine(
                            It.Is<string>(msg => msg.Contains("UnitTest,Error,\"Error message, with comma.\""))),
                        Times.Exactly(1));
                }
            }
        }

        private static void TryDeleteFiles(string startWith, string extension = ".csv")
        {
            // Clean up
            string[] targets = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{startWith}*{extension}");
            foreach (string file in targets)
            {
                File.Delete(file);
            }
        }
    }
}
