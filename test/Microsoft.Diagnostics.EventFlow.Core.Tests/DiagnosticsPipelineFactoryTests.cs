// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Filters;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Diagnostics.EventFlow.Outputs;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
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
                    ""schemaVersion"": ""2016-08-11"",
                }";

            try
            {
                using (var configFile = new TemporaryFile())
                {
                    configFile.Write(pipelineConfiguration);
                    ConfigurationBuilder builder = new ConfigurationBuilder();
                    builder.AddJsonFile(configFile.FilePath);
                    var configuration = builder.Build();

                    using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(configuration))
                    {
                        Assert.NotNull(pipeline);
                        Assert.True(pipeline.HealthReporter is CsvHealthReporter);
                    }
                }
            }
            finally
            {
                TryDeleteFile(CsvHealthReporter.DefaultLogFilePrefix, delayMilliseconds: 500);
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
                    ""schemaVersion"": ""2016-08-11"",

                    ""extensions"": [
                        {
                            ""category"": ""healthReporter"",
                            ""type"": ""CustomHealthReporter"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.Core.Tests.CustomHealthReporter, Microsoft.Diagnostics.EventFlow.Core.Tests""
                        }
                    ],
                    ""healthReporter"": {
                        ""type"": ""CustomHealthReporter"",
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
                using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(configuration))
                {
                    Assert.NotNull(pipeline);
                    Assert.True(pipeline.HealthReporter is CustomHealthReporter);
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

                    ""schemaVersion"": ""2016-08-11"",

                    ""settings"": {
                        ""maxConcurrency"": ""2"",
                        ""pipelineCompletionTimeoutMsec"": ""1000""
                    },
                    ""healthReporter"": {
                        ""type"": ""CustomHealthReporter"",
                    },
                    ""extensions"": [
                         {
                            ""category"": ""healthReporter"",
                            ""type"": ""CustomHealthReporter"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.Core.Tests.CustomHealthReporter, Microsoft.Diagnostics.EventFlow.Core.Tests""
                        }
                    ]
                }";

            try
            {
                using (var configFile = new TemporaryFile())
                {
                    configFile.Write(pipelineConfiguration);
                    ConfigurationBuilder builder = new ConfigurationBuilder();
                    builder.AddJsonFile(configFile.FilePath);
                    var configuration = builder.Build();

                    using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(configuration))
                    {
                        Assert.NotNull(pipeline);

                        Assert.Equal(pipeline.Inputs.Count, 1);
                        var eventSourceInput = pipeline.Inputs.First() as EventSourceInput;
                        Assert.NotNull(eventSourceInput);

                        var expectedEventSources = new EventSourceConfiguration[2];
                        expectedEventSources[0] = new EventSourceConfiguration { ProviderName = "Microsoft-ServiceFabric-Services" };
                        expectedEventSources[1] = new EventSourceConfiguration { ProviderName = "MyCompany-AirTrafficControlApplication-Frontend" };
                        Assert.True(eventSourceInput.EventSources.SequenceEqual(expectedEventSources));

                        var metadata = new EventMetadata("importance");
                        metadata.Properties.Add("importance", "can be discarded");
                        var metadataFilter = new EventMetadataFilter(metadata);
                        metadataFilter.IncludeCondition = "Level == Verbose";
                        Assert.True(pipeline.GlobalFilters.Count == 1);
                        Assert.True(pipeline.GlobalFilters.First().Equals(metadataFilter));

                        Assert.Equal(pipeline.Sinks.Count, 1);
                        EventSink sink = pipeline.Sinks.First();

                        var stdSender = sink.Output as StdOutput;
                        Assert.NotNull(stdSender);

                        metadata = new EventMetadata("metric");
                        metadata.Properties.Add("metricName", "StatefulRunAsyncFailure");
                        metadata.Properties.Add("metricValue", "1.0");
                        metadataFilter = new EventMetadataFilter(metadata);
                        metadataFilter.IncludeCondition = "ProviderName == Microsoft-ServiceFabric-Services && EventName == StatefulRunAsyncFailure";

                        Assert.True(sink.Filters.Count == 1);
                        Assert.True(sink.Filters.First().Equals(metadataFilter));
                    }
                }
            }
            finally
            {
                TryDeleteFile(CsvHealthReporter.DefaultLogFilePrefix);
            }
        }

        [Fact]
        public void CanOverrideDefaultPipelineItems()
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

                    ""filters"": [
                        {
                            ""type"": ""metadata"",
                            ""metadata"": ""metric"",
                            ""include"": ""ProviderName == Microsoft-ServiceFabric-Services && EventName == StatefulRunAsyncInvocation"",
                            ""metricName"": ""StatefulRunAsyncInvocation"",
                            ""metricValue"": ""1.0""
                        }
                    ],

                    ""outputs"": [
                        {
                            ""type"": ""StdOutput"",
                        }
                    ],

                    ""schemaVersion"": ""2016-08-11"",

                    ""healthReporter"": {
                        ""type"": ""CustomHealthReporter"",
                    },

                    ""extensions"": [
                         {
                            ""category"": ""healthReporter"",
                            ""type"": ""CustomHealthReporter"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.Core.Tests.CustomHealthReporter, Microsoft.Diagnostics.EventFlow.Core.Tests""
                        },
                        {
                            ""category"": ""inputFactory"",
                            ""type"": ""EventSource"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.Core.Tests.UnitTestInputFactory, Microsoft.Diagnostics.EventFlow.Core.Tests""
                        },
                         {
                            ""category"": ""filterFactory"",
                            ""type"": ""metadata"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.Core.Tests.UnitTestFilterFactory, Microsoft.Diagnostics.EventFlow.Core.Tests""
                        },
                         {
                            ""category"": ""outputFactory"",
                            ""type"": ""StdOutput"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.Core.Tests.UnitTestOutputFactory, Microsoft.Diagnostics.EventFlow.Core.Tests""
                        }
                    ]
                }";

            try
            {
                using (var configFile = new TemporaryFile())
                {
                    configFile.Write(pipelineConfiguration);
                    ConfigurationBuilder builder = new ConfigurationBuilder();
                    builder.AddJsonFile(configFile.FilePath);
                    var configuration = builder.Build();

                    using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(configuration))
                    {
                        Assert.NotNull(pipeline);
                        Assert.True(pipeline.HealthReporter is CustomHealthReporter);
                        Assert.Single(pipeline.Inputs);
                        Assert.IsType(typeof(UnitTestInput), pipeline.Inputs.First());
                        Assert.Single(pipeline.Sinks);
                        Assert.IsType(typeof(UnitTestOutput), pipeline.Sinks.First().Output);
                        Assert.Single(pipeline.GlobalFilters);
                        Assert.IsType(typeof(UnitTestFilter), pipeline.GlobalFilters.First());
                    }
                }
            }
            finally
            {
                TryDeleteFile(CsvHealthReporter.DefaultLogFilePrefix, delayMilliseconds: 500);
            }
        }

        [Fact]
        public void CanCreateAllStandardPipelineItems()
        {
#if NET46
            string pipelineConfiguration = @"
                {
                    ""inputs"": [
                        {
                            ""type"": ""EventSource"",
                            ""sources"": [
                                { ""providerName"": ""Microsoft-ServiceFabric-Services"" },
                            ]
                        },
                        {
                            ""type"": ""Microsoft.Extensions.Logging""
                        },
                        {
                            ""type"": ""Trace""
                        },
                        {
                            ""type"": ""Serilog""
                        },
                        {
                            ""type"": ""PerformanceCounter"",
                            ""counters"": [
                                {
                                    ""counterCategory"": ""Process"",
                                    ""counterName"": ""Private Bytes""
                                }
                            ]
                        }
                    ],

                    ""outputs"": [
                        {
                            ""type"": ""StdOutput"",
                        },
                        {
                            ""type"": ""ElasticSearch"",
                            ""serviceUri"": ""https://myElasticSearchCluster:9200"",
                            ""eventDocumentTypeName"": ""diagData""
                        }, 
                        {
                            ""type"": ""OmsOutput"",
                            ""workspaceId"": ""00000000-0000-0000-0000-000000000000"",
                            ""workspaceKey"": ""Tm90IGEgd29ya3NwYWNlIGtleQ==""
                        }, 
                        {
                            ""type"": ""EventHub"",
                            ""connectionString"": ""unused""
                        }, 
                        {
                            ""type"": ""ApplicationInsights"",
                            ""instrumentationKey"": ""00000000-0000-0000-0000-000000000000""
                        } 
                    ],

                    ""schemaVersion"": ""2016-08-11""
                }";
#else
            string pipelineConfiguration = @"
                {
                    ""inputs"": [
                        {
                            ""type"": ""EventSource"",
                            ""sources"": [
                                { ""providerName"": ""Microsoft-ServiceFabric-Services"" },
                            ]
                        },
                        {
                            ""type"": ""Microsoft.Extensions.Logging""
                        },
                        {
                            ""type"": ""Trace""
                        },
                        {
                            ""type"": ""Serilog""
                        }
                    ],

                    ""outputs"": [
                        {
                            ""type"": ""StdOutput"",
                        },
                        {
                            ""type"": ""ElasticSearch"",
                            ""serviceUri"": ""https://myElasticSearchCluster:9200"",
                            ""eventDocumentTypeName"": ""diagData""
                        }, 
                        {
                            ""type"": ""OmsOutput"",
                            ""workspaceId"": ""00000000-0000-0000-0000-000000000000"",
                            ""workspaceKey"": ""Tm90IGEgd29ya3NwYWNlIGtleQ==""
                        } 
                    ],

                    ""schemaVersion"": ""2016-08-11""
                }";
#endif

            try
            {
                using (var configFile = new TemporaryFile())
                {
                    configFile.Write(pipelineConfiguration);
                    ConfigurationBuilder builder = new ConfigurationBuilder();
                    builder.AddJsonFile(configFile.FilePath);
                    var configuration = builder.Build();

                    using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(configuration))
                    {
                        Assert.NotNull(pipeline);
                                                
                        Assert.Collection(pipeline.Inputs, 
                            i => Assert.IsType(typeof(EventSourceInput), i),
                            i => Assert.IsType(typeof(LoggerInput), i),
                            i => Assert.IsType(typeof(TraceInput), i),
                            i => Assert.IsType(typeof(SerilogInput), i)
#if NET46
                            , i => Assert.IsType(typeof(PerformanceCounterInput), i)
#endif
                        );

                        Assert.Collection(pipeline.Sinks,
                            s => Assert.IsType(typeof(StdOutput), s.Output),
                            s => Assert.IsType(typeof(ElasticSearchOutput), s.Output),
                            s => Assert.IsType(typeof(OmsOutput), s.Output)
#if NET46
                            , s => Assert.IsType(typeof(EventHubOutput), s.Output)
                            , s => Assert.IsType(typeof(ApplicationInsightsOutput), s.Output)
#endif
                        );
                    }
                }
            }
            finally
            {
                TryDeleteFile(CsvHealthReporter.DefaultLogFilePrefix, delayMilliseconds: 500);
            }
        }

        private static async void TryDeleteFile(string startWith, string extension = ".csv", int delayMilliseconds = 0)
        {
            // Clean up
            await Task.Delay(delayMilliseconds);
            string[] targets = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{startWith}*{extension}");
            foreach (string file in targets)
            {
                File.Delete(file);
            }
        }
    }
}
