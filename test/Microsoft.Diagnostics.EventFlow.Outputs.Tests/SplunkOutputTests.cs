using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Outputs.Splunk;
using Microsoft.Diagnostics.EventFlow.Outputs.Splunk.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class SplunkOutputTests
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
                        ["type"] = "Splunk",
                        ["serviceBaseAddress"] = "https://hec.mysplunkserver.com:8088",
                        ["authenticationToken"] = "B5A79AAD-D822-46CC-80D1-819F80D7BFB0",
                        ["host"] = "localhost",
                        ["index"] = "main",
                        ["source"] = "my source",
                        ["sourceType"] = "_json"
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

                var splunkOutputConfiguration = new SplunkOutputConfiguration();
                configFragments[0].Bind(splunkOutputConfiguration);

                Assert.Equal("https://hec.mysplunkserver.com:8088", splunkOutputConfiguration.ServiceBaseAddress);
                Assert.Equal("B5A79AAD-D822-46CC-80D1-819F80D7BFB0", splunkOutputConfiguration.AuthenticationToken);
                Assert.Equal("localhost", splunkOutputConfiguration.Host);
                Assert.Equal("main", splunkOutputConfiguration.Index);
                Assert.Equal("my source", splunkOutputConfiguration.Source);
                Assert.Equal("_json", splunkOutputConfiguration.SourceType);
            }
        }

        [Fact]
        public void MissingRequiredServiceBaseAddressConfigThrows()
        {
            var splunkOutputConfiguration = new SplunkOutputConfiguration()
            {
                AuthenticationToken = "B5A79AAD-D822-46CC-80D1-819F80D7BFB0"
            };

            var healthReporterMock = new Mock<IHealthReporter>();
            var expectedExceptionMessage = "SplunkOutput: 'serviceBaseAddress' configuration parameter is not set";

            var exception = Assert.Throws<Exception>(() => new SplunkOutput(splunkOutputConfiguration, healthReporterMock.Object));

            Assert.Equal(expectedExceptionMessage, exception.Message);
            healthReporterMock.Verify(m => m.ReportProblem(expectedExceptionMessage, EventFlowContextIdentifiers.Configuration), Times.Once);
        }

        [Fact]
        public void MissingRequiredAuthenticationTokenConfigThrows()
        {
            var splunkOutputConfiguration = new SplunkOutputConfiguration()
            {
                ServiceBaseAddress = "https://hec.mysplunkserver.com:8088"
            };

            var healthReporterMock = new Mock<IHealthReporter>();
            var expectedExceptionMessage = "SplunkOutput: 'authenticationToken' configuration parameter is not set";

            var exception = Assert.Throws<Exception>(() => new SplunkOutput(splunkOutputConfiguration, healthReporterMock.Object));

            Assert.Equal(expectedExceptionMessage, exception.Message);
            healthReporterMock.Verify(m => m.ReportProblem(expectedExceptionMessage, EventFlowContextIdentifiers.Configuration), Times.Once);
        }

        [Fact]
        public void RequiredConfigOnlyDoesNotThrow()
        {
            var splunkOutputConfiguration = new SplunkOutputConfiguration()
            {
                ServiceBaseAddress = "https://hec.mysplunkserver.com:8088",
                AuthenticationToken = "B5A79AAD-D822-46CC-80D1-819F80D7BFB0"
            };

            var healthReporterMock = new Mock<IHealthReporter>();
            var output = new SplunkOutput(splunkOutputConfiguration, healthReporterMock.Object);            

            Assert.NotNull(output);
            healthReporterMock.Verify(m => m.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
