// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
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
        private const string SingleLogFileMaximumSizeInMBytesKey = "SingleLogFileMaximumSizeInMBytes";
        private const string LogRetentionInDaysKey = "LogRetentionInDays";
        private const string EnsureOutputCanBeSavedKey = "EnsureOutputCanBeSaved";
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

        [Fact]
        public void ShouldExpandEnvironmentVariablesForLogFolder()
        {
            var configuration = BuildTestConfigration();
            Environment.SetEnvironmentVariable("WarsawUnitTestPath", @"x:\temp");
            configuration[LogFileFolderKey] = @"%WarsawUnitTestPath%\logs";
            string expected = Environment.ExpandEnvironmentVariables(@"%WarsawUnitTestPath%\logs");

            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                Assert.Equal(expected, target.ConfigurationWrapper.LogFileFolder);
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
        public void EscapeCommaShouldHandleNullOrEmptyString()
        {
            using (CustomHealthReporter target = new CustomHealthReporter(BuildTestConfigration()))
            {
                string actual = target.EscapeComma(null);
                Assert.Null(actual);

                actual = target.EscapeComma(string.Empty);
                Assert.Equal(string.Empty, actual);
            }
        }

        [Fact]
        public async void ShouldEscapeQuotesWhenThereIsCommaInMessage()
        {
            // Setup
            var configuration = BuildTestConfigration();

            // Exercise
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                target.Activate();
                target.ReportProblem("Error \"message\", with comma and quotes.", "UnitTest");
                // Verify
                await Task.Delay(DefaultDelayMsec);
                target.StreamWriterMock.Verify(
                    s => s.WriteLine(
                        It.Is<string>(msg => msg.Contains("UnitTest,Error,\"Error \"\"message\"\", with comma and quotes.\""))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public void ShouldHaveDefaultLogFileSizeLimitWhenNotSetInConfigure()
        {
            var configuration = BuildTestConfigration();
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                const long DefaultSizeInBytes = (long)8192 * 1024 * 1024;
                // Accepts 0
                Assert.Equal(0, target.ConfigurationWrapper.SingleLogFileMaximumSizeInMBytes);
                // Update to default limit & convert to bytes
                Assert.Equal(DefaultSizeInBytes, target.SingleLogFileMaximumSizeInBytes);
            }
        }

        [Fact]
        public void ShouldSetLogFileSizeLimitToDefaultWhenConfigUnderflow()
        {
            var configuration = BuildTestConfigration(logFileMaxInMB: -1);

            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                const long DefaultSizeInBytes = (long)8192 * 1024 * 1024;
                Assert.Equal(-1, target.ConfigurationWrapper.SingleLogFileMaximumSizeInMBytes);
                Assert.Equal(DefaultSizeInBytes, target.SingleLogFileMaximumSizeInBytes);
            }
        }

        [Fact]
        public void ShouldSetLogFileRetentionToDefaultWhenConfigUnderFlow()
        {
            var configuration = BuildTestConfigration(logRetention: -1);

            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                const int DefaultRetention = 30;
                Assert.Equal(DefaultRetention, target.ConfigurationWrapper.LogRetentionInDays);
            }
        }

        [Fact]
        public void ShouldHaveDefaultLogFileRetentionWhenNotSetInConfig()
        {
            var configuration = BuildTestConfigration();
            Assert.Equal(0, configuration.ToCsvHealthReporterConfiguration().LogRetentionInDays);
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                const int DefaultRetentionDays = 30;
                Assert.Equal(DefaultRetentionDays, target.ConfigurationWrapper.LogRetentionInDays);
            }
        }

        [Fact]
        public void ShouldHandleUnauthorizedAccessWhenCreatingTheFileStream()
        {
            var configuration = BuildTestConfigration();
            string exceptionMessage = "Simulate no permission to write the file.";
            using (CustomHealthReporter target = new CustomHealthReporter(configuration, 1000, customCreateFileStream: () => throw new UnauthorizedAccessException(exceptionMessage)))
            {
                // Exception throws within stream writer.
                Exception ex = Assert.Throws<UnauthorizedAccessException>(() => { target.CreateNewFileWriter(@"c:\log.log"); });
                Assert.Equal(exceptionMessage, ex.Message);

                target.Activate();
                // Test to making sure no UnauthorizedAccessException thrown on activation.
                Assert.True(true);
            }
        }

        [Fact]
        public void ShouldThrowUnauthorizedAccessWhenEnsureOutputCanBeSavedIsOn()
        {
            var configuration = BuildTestConfigration();
            configuration[EnsureOutputCanBeSavedKey] = "true";
            string exceptionMessage = "Simulate no permission to write the file.";
            using (CustomHealthReporter target = new CustomHealthReporter(configuration, 1000, setNewStreamWriter: () => throw new UnauthorizedAccessException(exceptionMessage)))
            {
                Exception ex = Assert.Throws<UnauthorizedAccessException>(() => target.Activate());
                // Test to making sure no UnauthorizedAccessException thrown on activation.
                Assert.Equal(exceptionMessage, ex.Message);
            }
        }

        [Fact]
        public void ShouldNotRotateLogFileWhenFileDoesNotExist()
        {
            var configuration = BuildTestConfigration();
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                bool rotate = false;
                string fileName = target.RotateLogFileImp(".", path => false, path => { }, (from, to) =>
                {
                    rotate = true;
                });
                Assert.False(rotate);
            }
        }

        [Fact]
        public void ShouldRotateLogFileWhenFileDoesExist()
        {
            var configuration = BuildTestConfigration();
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                bool rotate = false;
                string fileName = target.RotateLogFileImp(".", path => true, path => { }, (from, to) =>
                {
                    rotate = true;
                });
                Assert.True(rotate);
            }
        }

        [Fact]
        public void ShouldCleanUpExistingLogsPerRetentionPolicy()
        {
            var configuration = BuildTestConfigration(logRetention: 1);
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                int removedItemCount = 0;
                target.CleanupExistingLogs(info =>
                {
                    removedItemCount++;
                });
                Assert.Equal(2, removedItemCount);
            }
        }

        [Fact]
        public void ShouldSetEnsureOutputCanBeSavedToFalseByDefault()
        {
            var configuration = BuildTestConfigration();
            Assert.Null(configuration[EnsureOutputCanBeSavedKey]);
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                Assert.False(target.EnsureOutputCanBeSaved);
            }
        }

        [Fact]
        public void ShouldSetEnsureOutputCanBeSavedByConfiguration()
        {
            var configuration = BuildTestConfigration();
            configuration[EnsureOutputCanBeSavedKey] = "true";
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                Assert.True(target.EnsureOutputCanBeSaved);
            }

            configuration[EnsureOutputCanBeSavedKey] = "false";
            using (CustomHealthReporter target = new CustomHealthReporter(configuration))
            {
                Assert.False(target.EnsureOutputCanBeSaved);
            }
        }

        private IConfiguration BuildTestConfigration(
            long? logFileMaxInMB = null,
            int? logRetention = null)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>() {
                { LogFileFolderKey, DefaultLogFolder },
                { LogFilePrefixKey, DefaultReporterPrefix},
                { MinReportLevelKey, DefaultMinReportLevel},
                { ThrottlingPeriodMsecKey, DefaultThrottlingPeriodMsec}
            };

            if (logFileMaxInMB != null && logFileMaxInMB.HasValue)
            {
                dictionary.Add(SingleLogFileMaximumSizeInMBytesKey, logFileMaxInMB.Value.ToString());
            }

            if (logRetention != null && logRetention.HasValue)
            {
                dictionary.Add(LogRetentionInDaysKey, logRetention.Value.ToString());
            }

            return (new ConfigurationBuilder()).AddInMemoryCollection(dictionary).Build();
        }
    }
}
