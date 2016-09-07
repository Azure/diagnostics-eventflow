// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

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
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            Mock<IOutput> mockOutput = new Mock<IOutput>();
            DiagnosticPipelineConfiguration settings = new DiagnosticPipelineConfiguration()
            {
                MaxBatchDelayMsec = 10,
                PipelineCompletionTimeoutMsec = 1000
            };

            using (UnitTestInput unitTestInput = new UnitTestInput())
            using (DiagnosticPipeline pipeline = new DiagnosticPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { unitTestInput },
                null,
                new EventSink[] { new EventSink(mockOutput.Object, null) },
                settings))
            {

                // Execrise
                unitTestInput.SendMessage("Test information");

                // Give it a small delay to let the pipeline process through.
                await Task.Delay(100);

                // Verify
                mockOutput.Verify(o => o.SendEventsAsync(It.Is<IReadOnlyCollection<EventData>>(c => c.Count == 1),
                    It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            }
        }
    }
}
