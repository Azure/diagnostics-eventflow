// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Moq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.ApplicationInsights.DataContracts;

using Microsoft.Diagnostics.EventFlow.TestHelpers;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class ApplicationInsightsOutputTests
    {
        [Fact]
        public void UsesIsoDateFormat()
        {
            EventData e = new EventData();
            e.Payload.Add("DateTimeProperty", new DateTime(2017, 4, 19, 10, 15, 23, DateTimeKind.Utc));
            e.Payload.Add("DateTimeOffsetProperty", new DateTimeOffset(2017, 4, 19, 10, 16, 07, TimeSpan.Zero));

            var healthReporterMock = new Mock<IHealthReporter>();
            var config = new ApplicationInsightsOutputConfiguration();
            var aiOutput = new ApplicationInsightsOutput(config, healthReporterMock.Object);
            var propertyBag = new PropertyBag();

            aiOutput.AddProperties(propertyBag, e);

            var dateTimeRegex = new Regex("2017-04-19T10:15:23(\\.0+)?Z", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            Assert.Matches(dateTimeRegex, propertyBag.Properties["DateTimeProperty"]);

            var dateTimeOffsetRegex = new Regex("2017-04-19T10:16:07(\\.0+)?\\+00:00", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            Assert.Matches(dateTimeOffsetRegex, propertyBag.Properties["DateTimeOffsetProperty"]);
        }

        [Fact]
        public void InstrumentationKeyOverridesConnectionString()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var config = new ApplicationInsightsOutputConfiguration();
            config.ConnectionString = "InstrumentationKey=d0198460-ce4a-4efa-9e17-3edef2b40f15";
            config.InstrumentationKey = "c8da242c-9f2d-45ab-913c-c9953516e9c2";
            var output = new ApplicationInsightsOutput(config, healthReporterMock.Object);

            VerifyNoErrorsOrWarnings(healthReporterMock);

            var telemetry = new TraceTelemetry();
            output.telemetryClient.InitializeInstrumentationKey(telemetry);
            Assert.Equal("c8da242c-9f2d-45ab-913c-c9953516e9c2", telemetry.Context.InstrumentationKey, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConnectionStringOverridesConfigurationFile()
        {
            const string aiConfiguration = @"
                <?xml version=""1.0"" encoding=""utf-8""?>
                <ApplicationInsights xmlns=""http://schemas.microsoft.com/ApplicationInsights/2013/Settings"">
                    <InstrumentationKey>c8da242c-9f2d-45ab-913c-c9953516e9c2</InstrumentationKey>
                    <TelemetryChannel Type=""Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel, Microsoft.AI.ServerTelemetryChannel""/>
                </ApplicationInsights>
            ";

            var healthReporterMock = new Mock<IHealthReporter>();
            var telemetry = new TraceTelemetry();

            using (var configFile = new TemporaryFile())
            {
                configFile.Write(aiConfiguration);
                var config = new ApplicationInsightsOutputConfiguration();
                config.ConnectionString = "InstrumentationKey=d0198460-ce4a-4efa-9e17-3edef2b40f15";
                config.ConfigurationFilePath = configFile.FilePath;

                var output = new ApplicationInsightsOutput(config, healthReporterMock.Object);

                VerifyNoErrorsOrWarnings(healthReporterMock);
                
                output.telemetryClient.InitializeInstrumentationKey(telemetry);
            }

            Assert.Equal("d0198460-ce4a-4efa-9e17-3edef2b40f15", telemetry.Context.InstrumentationKey, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void InvalidConfigurationResultsInAnError()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var config = new ApplicationInsightsOutputConfiguration();
            var output = new ApplicationInsightsOutput(config, healthReporterMock.Object);

            healthReporterMock.Verify(
                hr => hr.ReportWarning(
                    It.IsRegex("invalid configuration"), 
                    It.Is<string>(ctx => string.Equals(ctx, EventFlowContextIdentifiers.Output, StringComparison.Ordinal))),
                Times.Exactly(1));
        }

        private class PropertyBag: ISupportProperties
        {
            public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();
        }

        private void VerifyNoErrorsOrWarnings(Mock<IHealthReporter> healthReporterMock)
        {
            healthReporterMock.Verify(hr => hr.ReportWarning(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Exactly(0));
            healthReporterMock.Verify(hr => hr.ReportProblem(It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(0));
        }
    }
}
