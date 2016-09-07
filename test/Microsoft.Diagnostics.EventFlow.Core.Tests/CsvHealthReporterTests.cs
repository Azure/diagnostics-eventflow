// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    [Collection("Core Unit Tests")]
    public class CsvHealthReporterTests
    {
        private const string HealthReporter = "HealthReport";
        private const string LogFileFolderKey = "LogFileFolder";
        private const string LogFilePrefixKey = "LogFilePrefix";
        private const string LogLevelKey = "MinReportLevel";
        private const string ThrottleTimeSpanKey = "ThrottlingPeriodMsec";
        private const int DefaultDelay = 100;

        [Fact]
        public void ConstructorShouldRequireConfigFile()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                CsvHealthReporter target = new CustomHealthReporter(configurationFilePath: null);
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: configurationFilePath", ex.Message);
        }

        public void ConstructorShouldHandleWrongFilterLevel()
        {
            // Setup
            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { LogFileFolderKey, "." },
                { LogFilePrefixKey, HealthReporter},
                { LogLevelKey, "WrongLevel"}
            }).Build();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                // Verify
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.EndsWith("Failed to parse log level. Please check the value of: WrongLevel."))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async void ReportHealthyShouldWriteMessage()
        {
            // Setup
            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { LogFileFolderKey, "." },
                { LogFilePrefixKey, HealthReporter},
                { LogLevelKey, "Message"}
            }).Build();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportHealthy("Healthy message.", "UnitTest");
                // Verify
                await Task.Delay(DefaultDelay);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.Contains("UnitTest,Message,Healthy message."))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async void ReportWarningShouldWriteWarning()
        {
            // Setup
            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { LogFileFolderKey, "." },
                { LogFilePrefixKey, HealthReporter},
                { LogLevelKey, "Message"}
            }).Build();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportWarning("Warning message.", "UnitTest");
                // Verify
                await Task.Delay(DefaultDelay);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.Contains("UnitTest,Warning,Warning message."))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async void ReportProblemShouldWriteError()
        {
            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { LogFileFolderKey, "." },
                { LogFilePrefixKey, HealthReporter},
                { LogLevelKey, "Message"}
            }).Build();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportProblem("Error message.", "UnitTest");
                await Task.Delay(DefaultDelay);
                // Verify
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.Contains("UnitTest,Error,Error message."))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async void ReporterShouldFilterOutMessage()
        {
            // Setup
            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { LogFileFolderKey, "." },
                { LogFilePrefixKey, HealthReporter},
                { LogLevelKey, "Warning"},
                { ThrottleTimeSpanKey, "0"}
            }).Build();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportHealthy("Supposed to be filtered.", "UnitTest");
                // Verify that message is filtered out.
                await Task.Delay(DefaultDelay);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.IsAny<string>()),
                    Times.Exactly(0));

                // Verify that warning is not filtered out.
                target.ReportWarning("Warning message", "UnitTests");
                await Task.Delay(DefaultDelay);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.IsAny<string>()),
                    Times.Exactly(1));

                // Verify that error is not filtered out.
                target.ReportWarning("Error message", "UnitTests");
                await Task.Delay(DefaultDelay);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.IsAny<string>()),
                    Times.Exactly(2));
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
            {
                configFile.Write(configJsonString);
                using (CustomHealthReporter target = new CustomHealthReporter(configFile.FilePath))
                {
                    Assert.Equal(level, target.LogLevel.ToString());
                }
            }
        }

        [Fact]
        public async void ShouldEscapeCommaInMessage()
        {
            // Setup
            var configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { LogFileFolderKey, "." },
                { LogFilePrefixKey, HealthReporter},
                { LogLevelKey, "Message"}
            }).Build();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportProblem("Error message, with comma.", "UnitTest");
                // Verify
                await Task.Delay(DefaultDelay);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.Contains("UnitTest,Error,\"Error message, with comma.\""))),
                    Times.Exactly(1));
            }
        }
    }
}

