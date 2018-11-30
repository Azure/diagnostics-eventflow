// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;

using Xunit;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

using Microsoft.Diagnostics.EventFlow.ApplicationInsights;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Diagnostics.EventFlow.TestHelpers;


namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class ApplicationInsightsInputTests
    {
        private Lazy<IConfiguration> diagnosticPipelineConfiguration_ = new Lazy<IConfiguration>(GetAppInsightsTestPipelineConfiguration, LazyThreadSafetyMode.ExecutionAndPublication);

        [Fact]
        public void ReportsAllTelemetryTypes()
        {
            UnitTestOutput unitTestOutput = null;
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(diagnosticPipelineConfiguration_.Value))
            {
                unitTestOutput = pipeline.Sinks.First().Output as UnitTestOutput;

                TelemetryConfiguration telemetryConfiguration = GetAppInsightsTestTelemetryConfiguration();
                EventFlowTelemetryProcessor efTelemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<EventFlowTelemetryProcessor>().First();
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

        [Fact]
        public void AddsMetadataToRequestTelemetry()
        {
            UnitTestOutput unitTestOutput = null;
            DateTimeOffset now = DateTimeOffset.Now;

            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(diagnosticPipelineConfiguration_.Value))
            {
                unitTestOutput = pipeline.Sinks.First().Output as UnitTestOutput;

                TelemetryConfiguration telemetryConfiguration = GetAppInsightsTestTelemetryConfiguration();
                EventFlowTelemetryProcessor efTelemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<EventFlowTelemetryProcessor>().First();
                efTelemetryProcessor.Pipeline = pipeline;

                TelemetryClient client = new TelemetryClient(telemetryConfiguration);
                client.TrackRequest("rqName", now, TimeSpan.FromMilliseconds(123), "418 I am a teapot", false);
            }

            Assert.Equal(1, unitTestOutput.EventCount);
            Assert.True(unitTestOutput.CapturedEvents.TryDequeue(out EventData eventData));
            Assert.Equal(now, eventData.Timestamp);

            Assert.True(eventData.TryGetMetadata(RequestData.RequestMetadataKind, out IReadOnlyCollection<EventMetadata> eventMetadata));
            Assert.Equal(1, eventMetadata.Count);

            EventMetadata requestMetadata = eventMetadata.ElementAt(0);
            Assert.Equal(DataRetrievalResult.Success, RequestData.TryGetData(eventData, requestMetadata, out RequestData requestData));

            Assert.Equal("rqName", requestData.RequestName);
            Assert.False(requestData.IsSuccess);
            Assert.Equal(TimeSpan.FromMilliseconds(123), requestData.Duration);
            Assert.Equal("418 I am a teapot", requestData.ResponseCode);
        }

        [Fact]
        public void AddsMetadataDependencyTelemetry()
        {
            UnitTestOutput unitTestOutput = null;
            DateTimeOffset now = DateTimeOffset.Now;

            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(diagnosticPipelineConfiguration_.Value))
            {
                unitTestOutput = pipeline.Sinks.First().Output as UnitTestOutput;

                TelemetryConfiguration telemetryConfiguration = GetAppInsightsTestTelemetryConfiguration();
                EventFlowTelemetryProcessor efTelemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<EventFlowTelemetryProcessor>().First();
                efTelemetryProcessor.Pipeline = pipeline;

                TelemetryClient client = new TelemetryClient(telemetryConfiguration);
                client.TrackDependency("FlawlessDependency", "https://flawless.microsoft.com", "dpName", "payload",
                    now, TimeSpan.FromMilliseconds(178), "201 Created", success: true);
            }

            Assert.Equal(1, unitTestOutput.EventCount);
            Assert.True(unitTestOutput.CapturedEvents.TryDequeue(out EventData eventData));
            Assert.Equal(now, eventData.Timestamp);

            Assert.True(eventData.TryGetMetadata(DependencyData.DependencyMetadataKind, out IReadOnlyCollection<EventMetadata> eventMetadata));
            Assert.Equal(1, eventMetadata.Count);

            EventMetadata dependencyMetadata = eventMetadata.ElementAt(0);
            Assert.Equal(DataRetrievalResult.Success, DependencyData.TryGetData(eventData, dependencyMetadata, out DependencyData dependencyData));

            Assert.True(dependencyData.IsSuccess);
            Assert.Equal(TimeSpan.FromMilliseconds(178), dependencyData.Duration);
            Assert.Equal("201 Created", dependencyData.ResponseCode);
            Assert.Equal("https://flawless.microsoft.com", dependencyData.Target);
            Assert.Equal("FlawlessDependency", dependencyData.DependencyType);
        }

        [Fact]
        public void AddsMetadataToMetricTelemetry()
        {
            UnitTestOutput unitTestOutput = null;
            DateTimeOffset now = DateTimeOffset.Now;

            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(diagnosticPipelineConfiguration_.Value))
            {
                unitTestOutput = pipeline.Sinks.First().Output as UnitTestOutput;

                TelemetryConfiguration telemetryConfiguration = GetAppInsightsTestTelemetryConfiguration();
                EventFlowTelemetryProcessor efTelemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<EventFlowTelemetryProcessor>().First();
                efTelemetryProcessor.Pipeline = pipeline;

                TelemetryClient client = new TelemetryClient(telemetryConfiguration);

                MetricTelemetry metric = new MetricTelemetry("rps", 223.7d);
                metric.Timestamp = now;
                client.TrackMetric(metric);
            }

            Assert.Equal(1, unitTestOutput.EventCount);
            Assert.True(unitTestOutput.CapturedEvents.TryDequeue(out EventData eventData));
            Assert.Equal(now, eventData.Timestamp);

            Assert.True(eventData.TryGetMetadata(MetricData.MetricMetadataKind, out IReadOnlyCollection<EventMetadata> eventMetadata));
            Assert.Equal(1, eventMetadata.Count);

            EventMetadata metricMetadata = eventMetadata.ElementAt(0);
            Assert.Equal(DataRetrievalResult.Success, MetricData.TryGetData(eventData, metricMetadata, out MetricData metricData));

            Assert.Equal("rps", metricData.MetricName);
            Assert.Equal(223.7d, metricData.Value);
        }

        [Fact]
        public void AddsMetadataToExceptionTelemetry()
        {
            UnitTestOutput unitTestOutput = null;
            DateTimeOffset now = DateTimeOffset.Now;

            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(diagnosticPipelineConfiguration_.Value))
            {
                unitTestOutput = pipeline.Sinks.First().Output as UnitTestOutput;

                TelemetryConfiguration telemetryConfiguration = GetAppInsightsTestTelemetryConfiguration();
                EventFlowTelemetryProcessor efTelemetryProcessor = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessors.OfType<EventFlowTelemetryProcessor>().First();
                efTelemetryProcessor.Pipeline = pipeline;

                TelemetryClient client = new TelemetryClient(telemetryConfiguration);

                try
                {
                    throw new Exception("Bummer!");
                }
                catch (Exception e)
                {
                    ExceptionTelemetry et = new ExceptionTelemetry(e);
                    et.Timestamp = now;
                    client.TrackException(et);
                }
                
            }

            Assert.Equal(1, unitTestOutput.EventCount);
            Assert.True(unitTestOutput.CapturedEvents.TryDequeue(out EventData eventData));
            Assert.Equal(now, eventData.Timestamp);

            Assert.True(eventData.TryGetMetadata(ExceptionData.ExceptionMetadataKind, out IReadOnlyCollection<EventMetadata> eventMetadata));
            Assert.Equal(1, eventMetadata.Count);

            EventMetadata exceptionMetadata = eventMetadata.ElementAt(0);
            Assert.Equal(DataRetrievalResult.Success, ExceptionData.TryGetData(eventData, exceptionMetadata, out ExceptionData exceptionData));

            Assert.Equal("Bummer!", exceptionData.Exception.Message);
        }

        private TelemetryConfiguration GetAppInsightsTestTelemetryConfiguration()
        {
            var config = new TelemetryConfiguration();

            config.InstrumentationKey = "58ce4b98-2e30-4ac1-982b-e47c85b8d31d"; // Fake, but needs to be there in order for TelemetryClient to call the processor chain.

            var channel = new TestTelemetryChannel();
            config.DefaultTelemetrySink.TelemetryChannel = channel;

            var chainbuilder = new TelemetryProcessorChainBuilder(config, config.DefaultTelemetrySink);
            chainbuilder.Use((next) =>
            {
                var p1 = new EventFlowTelemetryProcessor(next);
                return p1;
            });
            config.DefaultTelemetrySink.TelemetryProcessorChainBuilder = chainbuilder;
            chainbuilder.Build();

            return config;
        }

        private static IConfiguration GetAppInsightsTestPipelineConfiguration()
        {
            const string diagnosticPipelineConfiguration = @"
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
                configFile.Write(diagnosticPipelineConfiguration);
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(configFile.FilePath);
                var configuration = builder.Build();
                return configuration;
            }
        }
    }
}
