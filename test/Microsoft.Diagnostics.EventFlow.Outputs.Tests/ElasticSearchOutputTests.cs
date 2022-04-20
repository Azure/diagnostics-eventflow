using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Xunit;

using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;


namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class ElasticSearchOutputTests
    {
        [Fact]
        public void VerifyAllConfigOptionsMappedToConfigObject()
        {
            var pipelineConfigObj = new Dictionary<string, object>
            {
                ["outputs"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "ElasticSearch",
                        ["indexNamePrefix"] = "myprefix",
                        ["serviceUri"] = "http://localhost:1000;http://localhost:1001;http://localhost:1002",
                        ["basicAuthenticationUserName"] = "myesuser",
                        ["basicAuthenticationUserPassword"] = "myespass",
                        ["numberOfShards"] = 10,
                        ["numberOfReplicas"] = 20,
                        ["refreshInterval"] = "60s",
                        ["useSniffingConnectionPooling"] = "true",
                        ["mappings"] = new Dictionary<string, object>
                        {
                            ["properties"]= new Dictionary<string, object>
                            {
                                ["timestamp"] = new Dictionary<string, object>
                                {
                                    ["type"] = "date_nanos"
                                }
                            }
                        },
                        ["proxy"] = new Dictionary<string, object>
                        {
                            ["uri"] = "https://proxy.local/net/esproxy",
                            ["userName"] = "esuser",
                            ["userPassword"] = "verysecret"
                        },
                    }
                }
            };

            using (var configFile = new TemporaryFile())
            {
                var pipelineConfig = JsonConvert.SerializeObject(pipelineConfigObj, EventFlowJsonUtilities.GetDefaultSerializerSettings());

                configFile.Write(pipelineConfig);
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddJsonFile(configFile.FilePath);
                var configuration = configBuilder.Build();
                var outputConfigSection = configuration.GetSection("outputs");
                var configFragments = outputConfigSection.GetChildren().ToList();

                var esOutputConfiguration = new ElasticSearchOutputConfiguration();
                configFragments[0].Bind(esOutputConfiguration);

                Assert.Equal("myprefix", esOutputConfiguration.IndexNamePrefix);
                Assert.Equal("http://localhost:1000;http://localhost:1001;http://localhost:1002", esOutputConfiguration.ServiceUri);
                Assert.Equal("myesuser", esOutputConfiguration.BasicAuthenticationUserName);
                Assert.Equal("myespass", esOutputConfiguration.BasicAuthenticationUserPassword);
                Assert.Equal(10, esOutputConfiguration.NumberOfShards);
                Assert.Equal(20, esOutputConfiguration.NumberOfReplicas);
                Assert.Equal("60s", esOutputConfiguration.RefreshInterval);

                Assert.NotNull(esOutputConfiguration.Mappings);
                Assert.NotNull(esOutputConfiguration.Mappings.Properties);
                Assert.NotNull(esOutputConfiguration.Mappings.Properties["timestamp"]);

                Assert.NotNull(esOutputConfiguration.Proxy);
                Assert.Equal("https://proxy.local/net/esproxy", esOutputConfiguration.Proxy.Uri);
                Assert.Equal("esuser", esOutputConfiguration.Proxy.UserName);
                Assert.Equal("verysecret", esOutputConfiguration.Proxy.UserPassword);

                Assert.Equal("date_nanos", esOutputConfiguration.Mappings.Properties["timestamp"].Type);
            }
        }

        [Theory]
        [InlineData("", typeof(StaticConnectionPool))]
        [InlineData("Static", typeof(StaticConnectionPool))]
        [InlineData("Sniffing", typeof(SniffingConnectionPool))]
        [InlineData("Sticky", typeof(StickyConnectionPool))]
        public void VerifyValidConfigCreatesConnectionPool(string connectionType, Type expectedConnectionPool)
        {
            var testUriString = "http://localhost:8080";
            var elasticConfig = new ElasticSearchOutputConfiguration
            {
                ServiceUri = testUriString,
                ConnectionPoolType = connectionType
            };
            var healthReporterMock = new Mock<IHealthReporter>();
            var result = elasticConfig.GetConnectionPool(healthReporterMock.Object);

            Assert.IsType(expectedConnectionPool, result);
        }

        [Fact]
        public void VerifyUriListParsedFromString()
        {
            //Test single node
            var singleUri = "http://localhost:8080";
            var elasticConfigSingleNode = new ElasticSearchOutputConfiguration
            {
                ServiceUri = singleUri,
                ConnectionPoolType = ""
            };
            var singleExpected = new List<Node> { new Node(new Uri(singleUri)) };

            //Test many nodes
            var manyUri = "http://localhost:8080;http://localhost:8081";
            var elasticConfigManyNode = new ElasticSearchOutputConfiguration
            {
                ServiceUri = manyUri,
                ConnectionPoolType = "Static"
            };
            var manyExpected = new List<Node>
            {
                new Node(new Uri("http://localhost:8080")),
                new Node(new Uri("http://localhost:8081"))
            };

            var healthReporterMock = new Mock<IHealthReporter>();

            var singleResult = elasticConfigSingleNode.GetConnectionPool(healthReporterMock.Object);
            var manyResult = elasticConfigManyNode.GetConnectionPool(healthReporterMock.Object);

            Assert.Equal(singleExpected, singleResult.Nodes);

            // The nodes might be returned in different order, so we cannot just use "Equals" to compare the expected and actual collections.
            Assert.Equal(manyExpected.Count, manyResult.Nodes.Count);
            Assert.All(manyResult.Nodes, n => manyExpected.Contains(n));

            healthReporterMock.Verify(m => m.ReportHealthy("ElasticSearchOutput: Using default Static connection type.", EventFlowContextIdentifiers.Configuration));
        }

        [Fact]
        public void InvalidServiceUriConfigThrows()
        {
            var invalidUri = "httpNotValidUri";
            var elasticConfigSingleNode = new ElasticSearchOutputConfiguration
            {
                ServiceUri = invalidUri,
                ConnectionPoolType = ""
            };
            var healthReporterMock = new Mock<IHealthReporter>();
            var expectedExceptionMessage = "ElasticSearchOutput:  required 'serviceUri' configuration parameter is invalid";

            var exception = Assert.Throws<Exception>(() => elasticConfigSingleNode.GetConnectionPool(healthReporterMock.Object));

            Assert.Equal(expectedExceptionMessage, exception.Message);
            healthReporterMock.Verify(m => m.ReportProblem(expectedExceptionMessage, EventFlowContextIdentifiers.Configuration), Times.Once);
        }
    }
}
