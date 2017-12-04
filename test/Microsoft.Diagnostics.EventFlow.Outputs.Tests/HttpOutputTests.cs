using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class HttpOutputTests
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
                        ["type"] = "Http",
                        ["format"] = "JsonLines",
                        ["ServiceUri"] = "http://localhost:1000",
                        ["basicAuthenticationUserName"] = "mywebuser",
                        ["basicAuthenticationUserPassword"] = "mywebpass",
                        ["httpContentType"] = "application/x-custom",
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

                var httpOutputConfiguration = new HttpOutputConfiguration();
                configFragments[0].Bind(httpOutputConfiguration);

                Assert.Equal(httpOutputConfiguration.ServiceUri, "http://localhost:1000");
                Assert.Equal(httpOutputConfiguration.Format, HttpOutputFormat.JsonLines);
                Assert.Equal(httpOutputConfiguration.BasicAuthenticationUserName, "mywebuser");
                Assert.Equal(httpOutputConfiguration.BasicAuthenticationUserPassword, "mywebpass");
                Assert.Equal(httpOutputConfiguration.HttpContentType, "application/x-custom");
            }
        }
    }
}