// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Moq;
using Xunit;
using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class DiagnosticPipelineTests
    {
        [Fact]
        public void ConstructorShouldRequireHealthReport()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                DiagnosticPipeline pipeline = new DiagnosticPipeline(
                    null,
                    new List<TraceInput>(),
                    null,
                    new List<EventSink>());
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: healthReporter", ex.Message);
        }

        [Fact]
        public async void ShouldPassOneInputToOneOutput()
        {
            // Setup
            var configurationSectionMock = new Mock<IConfigurationSection>();
            configurationSectionMock.Setup(cs => cs["type"]).Returns("Trace");
            configurationSectionMock.Setup(cs => cs["traceLevel"]).Returns("All");
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            Mock<IOutput> mockOutput = new Mock<IOutput>();
            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = 1000
            };
            DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { new TraceInput(configurationSectionMock.Object, healthReporterMock.Object) },
                null,
                new EventSink[] { new EventSink(mockOutput.Object, null) },
                settings);

            // Exercise
            Trace.TraceInformation("Test information");
            // Batch delay is set to 10 so waiting 50 ms should be plenty of time to get the data to its output
            await Task.Delay(50);

            pipeline.Dispose();            

            // Verify
            mockOutput.Verify(o => o.SendEventsAsync(It.Is<IReadOnlyCollection<EventData>>(c => c.Count == 1),
                It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}
