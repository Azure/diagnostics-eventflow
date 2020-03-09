// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Moq;
using Xunit;

using Microsoft.Diagnostics.EventFlow.TestHelpers;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class StdOutputTests
    {
        [Fact]
        public void ConstructorShouldCreateTheInstance()
        {
            // Setup
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();

            // Execute
            StdOutput sender = new StdOutput(healthReporterMock.Object);

            // Verify
            Assert.NotNull(sender);
        }

        [Fact]
        public void ConstructorShouldRequireHealthReporter()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            {
                StdOutput target = new StdOutput(null);
            });

            Assert.Equal("healthReporter", ex.ParamName);
        }

        [Fact]
        public async Task MethodInfoIsSerializedAsFullyQualifiedName()
        {
            var healthReporter = new Mock<IHealthReporter>();

            EventData e = new EventData();
            e.ProviderName = "TestProvider";
            e.Timestamp = new DateTimeOffset(2018, 1, 2, 14, 12, 0, TimeSpan.Zero);
            e.Level = LogLevel.Warning;
            e.Payload.Add("Method", typeof(StdOutputTests).GetMethod(nameof(MethodInfoIsSerializedAsFullyQualifiedName)));

            string actualOutput = null;
            StdOutput stdOutput = new StdOutput(healthReporter.Object, s => actualOutput = s);
            await stdOutput.SendEventsAsync(new EventData[] { e }, 34, CancellationToken.None);

            string expecteOutput = @"[34]
                {
                    ""Timestamp"":""2018-01-02T14:12:00+00:00"",
                    ""ProviderName"":""TestProvider"",
                    ""Level"":3,
                    ""Keywords"":0,
                    ""Payload"":{""Method"":""Microsoft.Diagnostics.EventFlow.Outputs.Tests.StdOutputTests.MethodInfoIsSerializedAsFullyQualifiedName""}
                }
            ";
            Assert.Equal(expecteOutput.RemoveAllWhitespace(), actualOutput.RemoveAllWhitespace());
            healthReporter.Verify(hr => hr.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            healthReporter.Verify(hr => hr.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UsesCustomJsonSerializerSettings()
        {
            var healthReporter = new Mock<IHealthReporter>();

            EventData e = new EventData();
            e.ProviderName = "TestProvider";
            e.Timestamp = new DateTimeOffset(2018, 1, 2, 14, 12, 0, TimeSpan.Zero);
            e.Level = LogLevel.Warning;
            e.Payload.Add("InfinityProperty", Double.PositiveInfinity);

            string actualOutput = null;
            StdOutput stdOutput = new StdOutput(healthReporter.Object, s => actualOutput = s);
            await stdOutput.SendEventsAsync(new EventData[] { e }, 34, CancellationToken.None);

            string expecteOutput = @"[34]
                {
                    ""Timestamp"":""2018-01-02T14:12:00+00:00"",
                    ""ProviderName"":""TestProvider"",
                    ""Level"":3,
                    ""Keywords"":0,
                    ""Payload"":{""InfinityProperty"":""Infinity""}
                }
            ";
            Assert.Equal(expecteOutput.RemoveAllWhitespace(), actualOutput.RemoveAllWhitespace());
            healthReporter.Verify(hr => hr.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            healthReporter.Verify(hr => hr.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // Now verify changing serializer settings is effective
            stdOutput.SerializerSettings.FloatFormatHandling = FloatFormatHandling.DefaultValue;
            await stdOutput.SendEventsAsync(new EventData[] { e }, 36, CancellationToken.None);

            expecteOutput = @"[36]
                {
                    ""Timestamp"":""2018-01-02T14:12:00+00:00"",
                    ""ProviderName"":""TestProvider"",
                    ""Level"":3,
                    ""Keywords"":0,
                    ""Payload"":{""InfinityProperty"":0.0}
                }
            ";
            Assert.Equal(expecteOutput.RemoveAllWhitespace(), actualOutput.RemoveAllWhitespace());
            healthReporter.Verify(hr => hr.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            healthReporter.Verify(hr => hr.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
