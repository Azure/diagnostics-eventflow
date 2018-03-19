using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
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
                        ["serviceUri"] = "http://localhost:1000",
                        ["basicAuthenticationUserName"] = "myesuser",
                        ["basicAuthenticationUserPassword"] = "myespass",
                        ["numberOfShards"] = 10,
                        ["numberOfReplicas"] = 20,
                        ["refreshInterval"] = "60s",
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
                Assert.Equal("http://localhost:1000", esOutputConfiguration.ServiceUri);
                Assert.Equal("myesuser", esOutputConfiguration.BasicAuthenticationUserName);
                Assert.Equal("myespass", esOutputConfiguration.BasicAuthenticationUserPassword);
                Assert.Equal(10, esOutputConfiguration.NumberOfShards);
                Assert.Equal(20, esOutputConfiguration.NumberOfReplicas);
                Assert.Equal("60s", esOutputConfiguration.RefreshInterval);
            }
        }
    }
}