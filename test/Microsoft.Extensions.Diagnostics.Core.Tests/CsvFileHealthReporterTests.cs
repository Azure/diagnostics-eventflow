// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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
                                It.Is<string>(msg => msg.EndsWith("Failed to parse log level. Please check the value of: WrongLevel.\r\n"))),
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
        public void ShouldWriteMessage()
        {
            string logFileKey = "healthReporter:logFilePath";
            string logLevelKey = "healthReporter:logLevel";
            string healthReporter = "HealthReport.csv";

            // Setup
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c[logLevelKey]).Returns("Message");
            configMock.Setup(c => c[logFileKey]).Returns(healthReporter);

            // Exercise
            try
            {
                using (Stream memoryStream = new MemoryStream())
                {
                    var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                    using (CsvFileHealthReporter target = new CsvFileHealthReporter(configMock.Object, streamWriterMock.Object))
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
        public void ShouldWriteWarning()
        {
            string logFileKey = "healthReporter:logFilePath";
            string logLevelKey = "healthReporter:logLevel";
            string healthReporter = "HealthReport.csv";

            // Setup
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c[logLevelKey]).Returns("Message");
            configMock.Setup(c => c[logFileKey]).Returns(healthReporter);

            // Exercise
            try
            {
                using (Stream memoryStream = new MemoryStream())
                {
                    var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                    using (CsvFileHealthReporter target = new CsvFileHealthReporter(configMock.Object, streamWriterMock.Object))
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
        public void ShouldWriteError()
        {
            string logFileKey = "healthReporter:logFilePath";
            string logLevelKey = "healthReporter:logLevel";
            string healthReporter = "HealthReport.csv";

            // Setup
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c[logLevelKey]).Returns("Message");
            configMock.Setup(c => c[logFileKey]).Returns(healthReporter);

            // Exercise
            try
            {
                using (Stream memoryStream = new MemoryStream())
                {
                    var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                    using (CsvFileHealthReporter target = new CsvFileHealthReporter(configMock.Object, streamWriterMock.Object))
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
        public void ShouldFilterOutMessage()
        {
            string logFileKey = "healthReporter:logFilePath";
            string logLevelKey = "healthReporter:logLevel";
            string healthReporter = "HealthReport.csv";

            // Setup
            Mock<IConfiguration> configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c[logLevelKey]).Returns("Warning");
            configMock.Setup(c => c[logFileKey]).Returns(healthReporter);

            // Exercise
            try
            {
                using (Stream memoryStream = new MemoryStream())
                {
                    var streamWriterMock = new Mock<StreamWriter>(memoryStream);
                    using (CsvFileHealthReporter target = new CsvFileHealthReporter(configMock.Object, streamWriterMock.Object))
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
