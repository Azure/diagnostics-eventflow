// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Configuration;
using Microsoft.Extensions.Diagnostics.Filters;
using Microsoft.Extensions.Diagnostics.Inputs;
using Microsoft.Extensions.Diagnostics.Metadata;
using Microsoft.Extensions.Diagnostics.Outputs;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class DiagnosticsPipelineFactoryTests
    {
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

            using (var configFile = new TemporaryFile())
            {
                configFile.Write(pipelineConfiguration);
                ConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(configFile.FilePath);
                var configuration = builder.Build();

                var healthReporterMock = new Mock<IHealthReporter>();

                var pipeline = DiagnosticsPipelineFactory.CreatePipeline(configuration, healthReporterMock.Object) as DiagnosticsPipeline;
                Assert.NotNull(pipeline);

                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(0));
                healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(0));

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
}
