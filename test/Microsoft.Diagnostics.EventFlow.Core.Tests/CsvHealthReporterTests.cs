// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class CsvHealthReporterTests
    {
        private const string DefaultReporterPrefix = "HealthReport";
        private const string DefaultLogFolder = ".";
        private const string DefaultMinReportLevel = "Message";
        private const string DefaultThrottlingPeriodMsec = "0";

        private const string LogFileFolderKey = "LogFileFolder";
        private const string LogFilePrefixKey = "LogFilePrefix";
        private const string MinReportLevelKey = "MinReportLevel";
        private const string ThrottlingPeriodMsecKey = "ThrottlingPeriodMsec";
        private const int DefaultDelayMsec = 100;

        [Fact]
        public void ConstructorShouldRequireConfigFile()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                CsvHealthReporter target = new CustomHealthReporter(configurationFilePath: null);
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: configurationFilePath", ex.Message);
        }

        [Fact]
        public async void ConstructorShouldHandleWrongFilterLevel()
        {
            // Setup
            var configuration = BuildTestConfigration();
            configuration[MinReportLevelKey] = "WrongLevel";

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                await Task.Delay(DefaultDelayMsec);
                // Verify
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.EndsWith("Failed to parse log level. Please check the value of: WrongLevel. Falling back to default value: Error"))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async void ReportHealthyShouldWriteMessage()
        {
            // Setup
            var configuration = BuildTestConfigration();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportHealthy("Healthy message.", "UnitTest");
                // Verify
                await Task.Delay(DefaultDelayMsec);
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
            var configuration = BuildTestConfigration();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportWarning("Warning message.", "UnitTest");
                // Verify
                await Task.Delay(DefaultDelayMsec);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.Contains("UnitTest,Warning,Warning message."))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async void ReportProblemShouldWriteError()
        {
            var configuration = BuildTestConfigration();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportProblem("Error message.", "UnitTest");
                await Task.Delay(DefaultDelayMsec);
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
            var configuration = BuildTestConfigration();
            configuration[MinReportLevelKey] = "Warning";

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportHealthy("Supposed to be filtered.", "UnitTest");
                // Verify that message is filtered out.
                await Task.Delay(DefaultDelayMsec);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.IsAny<string>()),
                    Times.Exactly(0));

                // Verify that warning is not filtered out.
                target.ReportWarning("Warning message", "UnitTests");
                await Task.Delay(DefaultDelayMsec);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.IsAny<string>()),
                    Times.Exactly(1));

                // Verify that error is not filtered out.
                target.ReportWarning("Error message", "UnitTests");
                await Task.Delay(DefaultDelayMsec);
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
                    Assert.Equal(level, target.ConfigurationWrapper.MinReportLevel);
                }
            }
        }

        [Fact]
        public async void ShouldEscapeCommaInMessage()
        {
            // Setup
            var configuration = BuildTestConfigration();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportProblem("Error message, with comma.", "UnitTest");
                // Verify
                await Task.Delay(DefaultDelayMsec);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.Contains("UnitTest,Error,\"Error message, with comma.\""))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async void ShouldFlushOnceAWhile()
        {
            // Setup
            var configuration = BuildTestConfigration();
            int flushPeriodPlusMsec = 500;

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration, 200))
            {
                target.Activate();
                target.ReportProblem("Error message, with comma.", "UnitTest");
                // Verify
                await Task.Delay(DefaultDelayMsec);
                target.StreamWriterMock.Verify(
                    s => s.Flush(),
                    Times.Never());

                await Task.Delay(flushPeriodPlusMsec);
                target.ReportProblem("Error message, with comma.", "UnitTest");
                await Task.Delay(DefaultDelayMsec);

                target.StreamWriterMock.Verify(
                    s => s.Flush(),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public void ShouldParseRelativePath()
        {
            // Setup
            var configuration = BuildTestConfigration();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                Assert.True(Path.IsPathRooted(target.ConfigurationWrapper.LogFileFolder));
            }
        }

        private IConfiguration BuildTestConfigration()
        {
            return (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                { LogFileFolderKey, DefaultLogFolder },
                { LogFilePrefixKey, DefaultReporterPrefix},
                { MinReportLevelKey, DefaultMinReportLevel},
                { ThrottlingPeriodMsecKey, DefaultThrottlingPeriodMsec}
            }).Build();
        }
    }
}
