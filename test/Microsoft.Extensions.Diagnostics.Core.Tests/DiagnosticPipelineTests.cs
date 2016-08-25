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
using Microsoft.Extensions.Diagnostics.Inputs;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Core.Tests
{
    public class DiagnosticPipelineTests
    {
        [Fact]
        public void ConstructorShouldRequireHealthReport()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                DiagnosticsPipeline pipeline = new DiagnosticsPipeline(
                    null,
                    new List<TraceInput>(),
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
            DiagnosticsPipeline pipeline = new DiagnosticsPipeline(
                healthReporterMock.Object,
                new IObservable<EventData>[] { new TraceInput(configurationSectionMock.Object, healthReporterMock.Object) },
                new EventSink[] { new EventSink(mockOutput.Object, null) }
                );

            // Execrise
            Trace.TraceInformation("Test information");
            // Give it a small delay to let the pipeline process through.
            await Task.Delay(100);

            // Verify
            mockOutput.Verify(o => o.SendEventsAsync(It.Is<IReadOnlyCollection<EventData>>(c => c.Count == 1),
                It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}
