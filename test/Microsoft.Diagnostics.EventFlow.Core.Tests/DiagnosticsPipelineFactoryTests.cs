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
using Microsoft.Diagnostics.EventFlow.TestHelpers;
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
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.TestHelpers.CustomHealthReporter, Microsoft.Diagnostics.EventFlow.TestHelpers""
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
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.TestHelpers.CustomHealthReporter, Microsoft.Diagnostics.EventFlow.TestHelpers""
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

                        Assert.Single(pipeline.Inputs);
                        var eventSourceInput = pipeline.Inputs.First() as EventSourceInput;
                        Assert.NotNull(eventSourceInput);

                        var expectedEventSources = new EventSourceConfiguration[3];                        
                        expectedEventSources[0] = new EventSourceConfiguration { ProviderName = "Microsoft-ServiceFabric-Services" };
                        expectedEventSources[1] = new EventSourceConfiguration { ProviderName = "MyCompany-AirTrafficControlApplication-Frontend" };
                        // Microsoft-ApplicationInsights-Data is disabled by default to work around https://github.com/dotnet/coreclr/issues/14434
                        expectedEventSources[2] = new EventSourceConfiguration { DisabledProviderNamePrefix = "Microsoft-ApplicationInsights-Data" };
                        Assert.True(eventSourceInput.EventSources.SequenceEqual(expectedEventSources));

                        var metadata = new EventMetadata("importance");
                        metadata.Properties.Add("importance", "can be discarded");
                        var metadataFilter = new EventMetadataFilter(metadata);
                        metadataFilter.IncludeCondition = "Level == Verbose";
                        Assert.True(pipeline.GlobalFilters.Count == 1);
                        Assert.True(pipeline.GlobalFilters.First().Equals(metadataFilter));

                        Assert.Single(pipeline.Sinks);
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
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.TestHelpers.CustomHealthReporter, Microsoft.Diagnostics.EventFlow.TestHelpers""
                        },
                        {
                            ""category"": ""inputFactory"",
                            ""type"": ""EventSource"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.TestHelpers.UnitTestInputFactory, Microsoft.Diagnostics.EventFlow.TestHelpers""
                        },
                         {
                            ""category"": ""filterFactory"",
                            ""type"": ""metadata"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.TestHelpers.UnitTestFilterFactory, Microsoft.Diagnostics.EventFlow.TestHelpers""
                        },
                         {
                            ""category"": ""outputFactory"",
                            ""type"": ""StdOutput"",
                            ""qualifiedTypeName"": ""Microsoft.Diagnostics.EventFlow.TestHelpers.UnitTestOutputFactory, Microsoft.Diagnostics.EventFlow.TestHelpers""
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
                        Assert.IsType<UnitTestInput>(pipeline.Inputs.First());
                        Assert.Single(pipeline.Sinks);
                        Assert.IsType<UnitTestOutput>(pipeline.Sinks.First().Output);
                        Assert.Single(pipeline.GlobalFilters);
                        Assert.IsType<UnitTestFilter>(pipeline.GlobalFilters.First());
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
#if NET461
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
                            ""type"": ""NLog""
                        },
                        {
                            ""type"": ""Log4net"",
                            ""LogLevel"": ""Verbose""
                        },
                        {
                            ""type"": ""PerformanceCounter"",
                            ""counters"": [
                                {
                                    ""counterCategory"": ""Process"",
                                    ""counterName"": ""Private Bytes""
                                }
                            ]
                        },
                        {
                            ""type"": ""ETW"",
                            ""providers"": [
                                { ""providerName"": ""Microsoft-ServiceFabric-Services"" },
                            ]
                        }                        
                    ],

                    ""outputs"": [
                        {
                            ""type"": ""StdOutput"",
                        },
                        {
                            ""type"": ""OmsOutput"",
                            ""workspaceId"": ""00000000-0000-0000-0000-000000000000"",
                            ""workspaceKey"": ""Tm90IGEgd29ya3NwYWNlIGtleQ==""
                        }, 
                        {
                            ""type"": ""AzureMonitorLogs"",
                            ""workspaceId"": ""00000000-0000-0000-0000-000000000000"",
                            ""workspaceKey"": ""Tm90IGEgd29ya3NwYWNlIGtleQ==""
                        },
                        {
                            ""type"": ""Http"",
                            ""serviceUri"": ""https://example.com/""
                        },
                        {
                            ""type"": ""EventHub"",
                            ""connectionString"": ""Endpoint=sb://unused.servicebus.windows.net/;SharedAccessKeyName=send-only;SharedAccessKey=+lw95uDEcOLYE/zZFycZx3gxgomPgzfFmSdj+iBQiI8=;EntityPath=hub1""
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
                        },
                        {
                            ""type"": ""NLog""
                        },
                        {
                            ""type"": ""Log4net"",
                            ""LogLevel"": ""Verbose""
                        }
                    ],

                    ""outputs"": [
                        {
                            ""type"": ""StdOutput"",
                        },
                        {
                            ""type"": ""ElasticSearch"",
                            ""serviceUri"": ""https://myElasticSearchCluster:9200""
                        }, 
                        {
                            ""type"": ""OmsOutput"",
                            ""workspaceId"": ""00000000-0000-0000-0000-000000000000"",
                            ""workspaceKey"": ""Tm90IGEgd29ya3NwYWNlIGtleQ==""
                        },
                        {
                            ""type"": ""AzureMonitorLogs"",
                            ""workspaceId"": ""00000000-0000-0000-0000-000000000000"",
                            ""workspaceKey"": ""Tm90IGEgd29ya3NwYWNlIGtleQ==""
                        },
                        {
                            ""type"": ""Http"",
                            ""serviceUri"": ""https://example.com/""
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
                            i => Assert.IsType<EventSourceInput>(i),
                            i => Assert.IsType<LoggerInput>(i),
                            i => Assert.IsType<TraceInput>(i),
                            i => Assert.IsType<SerilogInput>(i),
                            i => Assert.IsType<NLogInput>(i),
                            i => Assert.IsType<Log4netInput>(i)
#if NET461
                            , i => Assert.IsType<PerformanceCounterInput>(i)
                            , i => Assert.IsType<EtwInput>(i)
#endif
                        );
                        
                        Assert.Collection(pipeline.Sinks,
                            s => Assert.IsType<StdOutput>(s.Output),
#if (!NET461)
                            s => Assert.IsType<ElasticSearchOutput>(s.Output),
#endif
                            s => Assert.IsType<OmsOutput>(s.Output),
                            s => Assert.IsType<OmsOutput>(s.Output), // Azure Monitor Logs output can be created using the old and the new name
                            s => Assert.IsType<HttpOutput>(s.Output)
#if NET461
                            , s => Assert.IsType<EventHubOutput>(s.Output)
                            , s => Assert.IsType<ApplicationInsightsOutput>(s.Output)
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
            await Task.Delay(delayMilliseconds);
            string[] targets = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{startWith}*{extension}");
            foreach (string file in targets)
            {
                File.Delete(file);
            }
        }
    }
}
