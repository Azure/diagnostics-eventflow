using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class TcpOutputTests
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
                        ["type"] = "Tcp",
                        ["format"] = "json-lines",
                        ["ServiceHost"] = "example.com",
                        ["ServicePort"] = 8000,
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

                var tcpOutputConfiguration = new TcpOutputConfiguration();
                configFragments[0].Bind(tcpOutputConfiguration);

                Assert.Equal(tcpOutputConfiguration.ServiceHost, "example.com");
                Assert.Equal(tcpOutputConfiguration.ServicePort, 8000);
                Assert.Equal(tcpOutputConfiguration.Format, "json-lines");
            }
        }
    }
}