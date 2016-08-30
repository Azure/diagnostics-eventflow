// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.IO;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Filters;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Diagnostics.EventFlow.Outputs;
using Microsoft.Diagnostics.EventFlow.Tests.Mocks;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Tests
{
    public class DiagnosticsPipelineFactoryTests
    {
        [Fact]
        public void ShouldUseDefaultHealthReporterIfNotSpecified()
        {
            string pipelineConfiguration = @"
                {
                    ""inputs"": [
                        {
                            ""type"": ""EventSource"",
                            ""sources"": [
                                { ""providerName"": ""Microsoft-ServiceFabric-Services"" },
                            ]
                        }
                    ],
                    ""outputs"": [
                        {
                            ""type"": ""StdOutput"",
                        }
                    ],
                    ""schema-version"": ""2016-08-11"",
                }";

            try
            {
                using (var configFile = new TemporaryFile())
                {
                    configFile.Write(pipelineConfiguration);
                    ConfigurationBuilder builder = new ConfigurationBuilder();
                    builder.AddJsonFile(configFile.FilePath);
                    var configuration = builder.Build();

                    using (var pipeline = DiagnosticsPipelineFactory.CreatePipeline(configuration))
                    {
                        Assert.NotNull(pipeline);
                        Assert.True(pipeline.HealthReporter is CsvHealthReporter);
                    }
                }
            }
            finally
            {
                TryDeleteFile(CsvHealthReporter.DefaultHealthReportName);
            }

        }

        [Fact]
        public void ShouldUse3rdPartyHealthReporterIfSpecified()
        {
            string pipelineConfiguration = @"
                {
                    ""inputs"": [
                        {
                            ""type"": ""EventSource"",
                            ""sources"": [
                                { ""providerName"": ""Microsoft-ServiceFabric-Services"" },
                            ]
                        }
                    ],
                    ""outputs"": [
                        {
                            ""type"": ""StdOutput"",
                        }
                    ],
                    ""schema-version"": ""2016-08-11"",

                    ""extensions"": [
                        {
                            ""category"": ""healthReporter"",
                            ""type"": ""HealthReporterMock"",
                            ""assemblyQualifiedName"": ""Microsoft.Diagnostics.EventFlow.Tests.Mocks.HealthReporterMock, Microsoft.Diagnostics.EventFlow.Core.Tests""
                        }
                    ],
                    ""healthReporter"": {
                        ""type"": ""HealthReporterMock"",
                    }
                }";

            using (var configFile = new TemporaryFile())
            {
                configFile.Write(pipelineConfiguration);
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(configFile.FilePath);
                var configuration = builder.Build();

                var mocked = new Mock<IHealthReporter>();
                var isHP = mocked is IHealthReporter;
                using (var pipeline = DiagnosticsPipelineFactory.CreatePipeline(configuration))
                {
                    Assert.NotNull(pipeline);
                    Assert.True(pipeline.HealthReporter is HealthReporterMock);
                }
            }
        }

        [Fact]
        public void ShouldInstantiatePipelineFromValidConfiguration()
        {
            string pipelineConfiguration = @"
                {
                    ""inputs"": [
                        {  ""type"": ""EventSource"",
                            ""sources"": [
                                { ""providerName"": ""Microsoft-ServiceFabric-Services"" },
                                { ""providerName"": ""MyCompany-AirTrafficControlApplication-Frontend"" }
                            ]
                        }
                    ],

                    ""filters"": [
                        {
                            ""type"": ""metadata"",
                            ""metadata"": ""importance"",
                            ""include"": ""Level == Verbose"",
                            ""importance"": ""can be discarded""
                        }
                    ],

                    ""outputs"": [
                        {
                            ""type"": ""StdOutput"",

                            ""filters"": [
                                {
                                    ""type"": ""metadata"",
                                    ""metadata"": ""metric"",
                                    ""include"": ""ProviderName == Microsoft-ServiceFabric-Services && EventName == StatefulRunAsyncFailure"",
                                    ""metricName"": ""StatefulRunAsyncFailure"",
                                    ""metricValue"": ""1.0""
                                }
                            ]
                        }
                    ],

                    ""schema-version"": ""2016-08-11"",
                }";

            try
            {
                using (var configFile = new TemporaryFile())
                {
                    configFile.Write(pipelineConfiguration);
                    ConfigurationBuilder builder = new ConfigurationBuilder();
                    builder.AddJsonFile(configFile.FilePath);
                    var configuration = builder.Build();

                    using (var pipeline = DiagnosticsPipelineFactory.CreatePipeline(configuration))
                    {
                        Assert.NotNull(pipeline);

                        Assert.Equal(pipeline.Inputs.Count, 1);
                        var eventSourceInput = pipeline.Inputs.First() as EventSourceInput;
                        Assert.NotNull(eventSourceInput);

                        var expectedEventSources = new EventSourceConfiguration[2];
                        expectedEventSources[0] = new EventSourceConfiguration { ProviderName = "Microsoft-ServiceFabric-Services" };
                        expectedEventSources[1] = new EventSourceConfiguration { ProviderName = "MyCompany-AirTrafficControlApplication-Frontend" };
                        Assert.True(eventSourceInput.EventSources.SequenceEqual(expectedEventSources));

                        Assert.Equal(pipeline.Sinks.Count, 1);
                        EventSink sink = pipeline.Sinks.First();

                        var stdSender = sink.Output as StdOutput;
                        Assert.NotNull(stdSender);

                        var expectedFilters = new EventMetadataFilter[2];
                        var metadata = new EventMetadata("importance");
                        metadata.IncludeCondition = "Level == Verbose";
                        metadata.Properties.Add("importance", "can be discarded");
                        expectedFilters[0] = new EventMetadataFilter(metadata);

                        metadata = new EventMetadata("metric");
                        metadata.IncludeCondition = "ProviderName == Microsoft-ServiceFabric-Services && EventName == StatefulRunAsyncFailure";
                        metadata.Properties.Add("metricName", "StatefulRunAsyncFailure");
                        metadata.Properties.Add("metricValue", "1.0");
                        expectedFilters[1] = new EventMetadataFilter(metadata);

                        Assert.True(sink.Filters.SequenceEqual(expectedFilters));
                    }
                }
            }
            finally
            {
                TryDeleteFile(CsvHealthReporter.DefaultHealthReportName);
            }
        }

        private static void TryDeleteFile(string fileName)
        {
            // Clean up
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}
