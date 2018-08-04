// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class Log4NetInputTests
    {

        [Fact]
        public void VerifyAllConfigOptionsMappedToConfigObject()
        {
            var pipelineConfigObj = new Dictionary<string, object>
            {
                ["inputs"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["logLevel"] = "Verbose"
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
                var inputsConfigSection = configuration.GetSection("inputs");
                var configFragments = inputsConfigSection.GetChildren().ToList();

                var l4nInputConfiguration = new Log4netConfiguration();
                configFragments[0].Bind(l4nInputConfiguration);

                Assert.Equal("Verbose", l4nInputConfiguration.LogLevel);
            }
        }

        [Fact]
        public void ValidConfigurationCanWrtieToInputPipleine()
        {
            var healthReportMock = new Mock<IHealthReporter>();
            var configurationMock = new Log4netConfiguration { LogLevel = "INFO" };
            var mockOutput = new Mock<IOutput>();

            using (var log4NetInput = new Log4netInput(configurationMock, healthReportMock.Object))
            using (var pipeline = new DiagnosticPipeline(
                healthReportMock.Object,
                new[] { log4NetInput },
                new IFilter[0],
                new[] { new EventSink(mockOutput.Object, new IFilter[0]) }))
            {
                var logger = LogManager.GetLogger("EventFlow", typeof(Log4NetInputTests));
                logger.Info("some message");
            }
            mockOutput.Verify(
                output => output.SendEventsAsync(It.Is<IReadOnlyCollection<EventData>>(c => c.Count == 1),
                    It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public void LowerLevelDefinedDoesNotWriteToPipeline()
        {
            var healthReportMock = new Mock<IHealthReporter>();
            var configurationMock = new Log4netConfiguration { Log4netLevel = Level.Info };
            var mockOutput = new Mock<IOutput>();

            using (var log4NetInput = new Log4netInput(configurationMock, healthReportMock.Object))
            using (var pipeline = new DiagnosticPipeline(
                healthReportMock.Object,
                new[] { log4NetInput },
                new IFilter[0],
                new[] { new EventSink(mockOutput.Object, new IFilter[0]) }))
            {
                var logger = LogManager.GetLogger("EventFlow", typeof(Log4NetInputTests));
                logger.Debug("some message");
            }
            mockOutput.Verify(
                output => output.SendEventsAsync(It.Is<IReadOnlyCollection<EventData>>(c => c.Count == 1),
                    It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
