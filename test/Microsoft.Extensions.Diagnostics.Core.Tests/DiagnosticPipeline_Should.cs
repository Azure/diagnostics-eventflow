// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Inputs;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class DiagnosticPipeline_Should
    {
        [Fact(DisplayName = "DiagnosticPipeline should pass 1 input to 1 output")]
        public async void PassOneInputToOneOutput()
        {
            // Setup
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            Mock<IEventSender<EventData>> mockOutput = new Mock<IEventSender<EventData>>();
            DiagnosticsPipeline<EventData> pipeline = new DiagnosticsPipeline<EventData>(
                healthReporterMock.Object,
                new IObservable<EventData>[] { new TraceInput(healthReporterMock.Object) },
                new EventSink<EventData>[] { new EventSink<EventData>(mockOutput.Object, null) }
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
