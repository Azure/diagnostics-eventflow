using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class UdpOutputTests
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
                        ["type"] = "Udp",
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

                var udpOutputConfiguration = new UdpOutputConfiguration();
                configFragments[0].Bind(udpOutputConfiguration);

                Assert.Equal(udpOutputConfiguration.ServiceHost, "example.com");
                Assert.Equal(udpOutputConfiguration.ServicePort, 8000);
                Assert.Equal(udpOutputConfiguration.Format, "json-lines");
            }
        }
    }
}