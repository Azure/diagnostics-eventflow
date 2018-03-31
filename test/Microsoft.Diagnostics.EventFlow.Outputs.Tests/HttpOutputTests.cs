using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;
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
                        ["headers"] = new Dictionary<string, string>
                        {
                            ["X-Foo"] = "example"
                        }
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

                Assert.Equal("http://localhost:1000", httpOutputConfiguration.ServiceUri);
                Assert.Equal(HttpOutputFormat.JsonLines, httpOutputConfiguration.Format);
                Assert.Equal("mywebuser", httpOutputConfiguration.BasicAuthenticationUserName);
                Assert.Equal("mywebpass", httpOutputConfiguration.BasicAuthenticationUserPassword);
                Assert.Equal("application/x-custom", httpOutputConfiguration.HttpContentType);
                Assert.Equal("example", httpOutputConfiguration.Headers["X-Foo"]);
            }
        }

        [Fact]
        public async Task ProducesJsonByDefault()
        {
            var config = new HttpOutputConfiguration();
            config.ServiceUri = "http://logcollector:1234";

            var healthReporterMock = new Mock<IHealthReporter>();
            var httpClientMock = new Mock<Implementation.IHttpClient>();
            httpClientMock.Setup(client => client.PostAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));

            var output = new HttpOutput(config, healthReporterMock.Object, httpClientMock.Object);

            var events = GetTestBatch(healthReporterMock.Object);

            string expectedContent = @"
                [
                    {
                        ""Timestamp"":""2018-01-02T14:12:00+00:00"",
                        ""ProviderName"":""HttpOutputTests"",
                        ""Level"":4,
                        ""Keywords"":0,
                        ""Payload"":{""Message"":""Hey!""}
                    },
                    {
                        ""Timestamp"":""2018-01-02T14:14:20+00:00"",
                        ""ProviderName"":""HttpOutputTests"",
                        ""Level"":3,
                        ""Keywords"":0,
                        ""Payload"":{""Message"":""Hey!""}
                    }]";
            expectedContent = RemoveWhitespace(expectedContent);
            
            await output.SendEventsAsync(events, 78, CancellationToken.None);
            httpClientMock.Verify(client => client.PostAsync(
                new Uri("http://logcollector:1234"), 
                It.Is<HttpContent>(content => content.ReadAsStringAsync().GetAwaiter().GetResult() == expectedContent)
            ), Times.Once());

        }

        [Fact]
        public async Task ProducesJsonLinesIfRequested()
        {
            var config = new HttpOutputConfiguration();
            config.ServiceUri = "http://logcollector:1234";
            config.Format = HttpOutputFormat.JsonLines;

            var healthReporterMock = new Mock<IHealthReporter>();
            var httpClientMock = new Mock<Implementation.IHttpClient>();
            httpClientMock.Setup(client => client.PostAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));

            var output = new HttpOutput(config, healthReporterMock.Object, httpClientMock.Object);

            var events = new List<EventData>(2);

            var e = new EventData();
            e.Timestamp = new DateTimeOffset(2018, 1, 2, 14, 12, 0, TimeSpan.Zero);
            e.ProviderName = nameof(HttpOutputTests);
            e.Level = LogLevel.Informational;
            e.AddPayloadProperty("Message", "Hey!", healthReporterMock.Object, "tests");
            events.Add(e);

            e = new EventData();
            e.Timestamp = new DateTimeOffset(2018, 1, 2, 14, 14, 20, TimeSpan.Zero);
            e.ProviderName = nameof(HttpOutputTests);
            e.Level = LogLevel.Warning;
            e.AddPayloadProperty("Message", "Hey!", healthReporterMock.Object, "tests");
            events.Add(e);

            string expectedContent = @"
                {
                    ""Timestamp"":""2018-01-02T14:12:00+00:00"",
                    ""ProviderName"":""HttpOutputTests"",
                    ""Level"":4,
                    ""Keywords"":0,
                    ""Payload"":{""Message"":""Hey!""}
                }
                {
                    ""Timestamp"":""2018-01-02T14:14:20+00:00"",
                    ""ProviderName"":""HttpOutputTests"",
                    ""Level"":3,
                    ""Keywords"":0,
                    ""Payload"":{""Message"":""Hey!""}
                }";
            expectedContent = RemoveWhitespace(expectedContent);

            await output.SendEventsAsync(events, 78, CancellationToken.None);
            httpClientMock.Verify(client => client.PostAsync(
                new Uri("http://logcollector:1234"),
                It.Is<HttpContent>(content => RemoveWhitespace(content.ReadAsStringAsync().GetAwaiter().GetResult()) == expectedContent)
            ), Times.Once());
        }

        private List<EventData> GetTestBatch(IHealthReporter healthReporter)
        {
            var events = new List<EventData>(2);

            var e = new EventData();
            e.Timestamp = new DateTimeOffset(2018, 1, 2, 14, 12, 0, TimeSpan.Zero);
            e.ProviderName = nameof(HttpOutputTests);
            e.Level = LogLevel.Informational;
            e.AddPayloadProperty("Message", "Hey!", healthReporter, "tests");
            events.Add(e);

            e = new EventData();
            e.Timestamp = new DateTimeOffset(2018, 1, 2, 14, 14, 20, TimeSpan.Zero);
            e.ProviderName = nameof(HttpOutputTests);
            e.Level = LogLevel.Warning;
            e.AddPayloadProperty("Message", "Hey!", healthReporter, "tests");
            events.Add(e);

            return events;
        }

        private string RemoveWhitespace(string input)
        {
            return new string(input.Where(c => !Char.IsWhiteSpace(c)).ToArray());
        }
    }
}