// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Diagnostics.EventFlow.ApplicationInsights;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class ApplicationInsightsInputTests
    {
        [Fact]
        public void ReportsAllTelemetryTypes()
        {
            string pipelineConfiguration = @"
                {
                    ""inputs"": [
                        { ""type"": ""ApplicationInsights"" }
                    ],
                    ""outputs"": [
                        { 
                            ""type"": ""UnitTestOutput"",
                            ""preserveEvents"": ""true""
                        }
                    ],

                    ""schemaVersion"": ""2016-08-11"",

                    ""extensions"": [
                         {
                            ""category"": ""outputFactory"",
                            ""type"": ""UnitTestOutput"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.TestHelpers.UnitTestOutputFactory, Microsoft.Diagnostics.EventFlow.TestHelpers""
                        },
                        {
                            ""category"": ""healthReporter"",
                            ""type"": ""CustomHealthReporter"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.TestHelpers.CustomHealthReporter, Microsoft.Diagnostics.EventFlow.TestHelpers""
                        }
                    ]
                }";

            using (var configFile = new TemporaryFile())
            {
                configFile.Write(pipelineConfiguration);
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(configFile.FilePath);
                var configuration = builder.Build();

                UnitTestOutput unitTestOutput = null;
                using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(configuration))
                {
                    unitTestOutput = pipeline.Sinks.First().Output as UnitTestOutput;

                    string configurationDoc = File.ReadAllText("ApplicationInsights.config");
                    TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateFromConfiguration(configurationDoc);
                    EventFlowTelemetryProcessor efTelemetryProcessor = telemetryConfiguration.TelemetryProcessors.OfType<EventFlowTelemetryProcessor>().First();
                    efTelemetryProcessor.Pipeline = pipeline;

                    TelemetryClient client = new TelemetryClient(telemetryConfiguration);
                    client.TrackTrace("This is a trace");
                    client.TrackRequest("DoStuff", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(20), "200 OK", success: true);
                    client.TrackEvent("ImportantEvent", new Dictionary<string, string> { { "eventProp", "foo" } });
                    client.TrackDependency("otherService", "inquire", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(57), success: true);
                    client.TrackMetric("rps", 340.7);
                    try
                    {
                        throw new Exception("Oops!");
                    }
                    catch (Exception e)
                    {
                        client.TrackException(e);
                    }
                    client.TrackPageView("Home page");
                    client.TrackAvailability("frontend-service-ping", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(73.5), "Munich-DE", success: true);
                }

                Assert.Equal(8, unitTestOutput.EventCount);
                Assert.Collection(unitTestOutput.CapturedEvents, 
                    e => Assert.Equal("trace", e.Payload[EventFlowTelemetryProcessor.TelemetryTypeProperty]),
                    e => Assert.Equal("request", e.Payload[EventFlowTelemetryProcessor.TelemetryTypeProperty]),
                    e => Assert.Equal("event", e.Payload[EventFlowTelemetryProcessor.TelemetryTypeProperty]),
                    e => Assert.Equal("dependency", e.Payload[EventFlowTelemetryProcessor.TelemetryTypeProperty]),
                    e => Assert.Equal("metric", e.Payload[EventFlowTelemetryProcessor.TelemetryTypeProperty]),
                    e => Assert.Equal("exception", e.Payload[EventFlowTelemetryProcessor.TelemetryTypeProperty]),
                    e => Assert.Equal("page_view", e.Payload[EventFlowTelemetryProcessor.TelemetryTypeProperty]),
                    e => Assert.Equal("availability", e.Payload[EventFlowTelemetryProcessor.TelemetryTypeProperty]));
            }
        }
    }
}
