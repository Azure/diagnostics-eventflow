using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Xunit;

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
                        ["eventDocumentTypeName"] = "mytype",
                        ["serviceUri"] = "http://localhost:1000;http://localhost:1001;http://localhost:1002",
                        ["basicAuthenticationUserName"] = "myesuser",
                        ["basicAuthenticationUserPassword"] = "myespass",
                        ["numberOfShards"] = 10,
                        ["numberOfReplicas"] = 20,
                        ["refreshInterval"] = "60s",
                        ["useSniffingConnectionPooling"] = "true",
                    }
                }
            };

            using (var configFile = new TemporaryFile())
            {
                var pipelineConfig = JsonConvert.SerializeObject(pipelineConfigObj);

                configFile.Write(pipelineConfig);
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddJsonFile(configFile.FilePath);
                var configuration = configBuilder.Build();
                var outputConfigSection = configuration.GetSection("outputs");
                var configFragments = outputConfigSection.GetChildren().ToList();

                var esOutputConfiguration = new ElasticSearchOutputConfiguration();
                configFragments[0].Bind(esOutputConfiguration);

                Assert.Equal("myprefix", esOutputConfiguration.IndexNamePrefix);
                Assert.Equal("mytype", esOutputConfiguration.EventDocumentTypeName);
                Assert.Equal("http://localhost:1000;http://localhost:1001;http://localhost:1002", esOutputConfiguration.ServiceUri);
                Assert.Equal("myesuser", esOutputConfiguration.BasicAuthenticationUserName);
                Assert.Equal("myespass", esOutputConfiguration.BasicAuthenticationUserPassword);
                Assert.Equal(10, esOutputConfiguration.NumberOfShards);
                Assert.Equal(20, esOutputConfiguration.NumberOfReplicas);
                Assert.Equal("60s", esOutputConfiguration.RefreshInterval);
            }
        }

        [Theory]
        [InlineData("SingleNode", typeof(SingleNodeConnectionPool))]
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
                ConnectionPoolType = "SingleNode"
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
            Assert.Equal(manyExpected, manyResult.Nodes);
        }
    }
}