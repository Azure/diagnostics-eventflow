// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Inputs;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class DiagnosticPipeline_Should
    {
        [Fact(DisplayName = "Diagnostics pipeline should pass on input to output")]
        public async void PassOnInputToOutput()
        {
            // Fixture setup
            IHealthReporter dummyHealthReporter = new DummyHealthReporter();
            MockOutput mockOutput = new MockOutput(dummyHealthReporter);
            try
            {
                DiagnosticsPipeline<EventData> pipeline = new DiagnosticsPipeline<EventData>(
                    dummyHealthReporter,
                    new List<IObservable<EventData>>() { new TraceInput(dummyHealthReporter) },
                    new EventSink<EventData>[] { new EventSink<EventData>(mockOutput, null) }
                    );
                // Exercise System
                Trace.TraceInformation("Test information");

                // Verify Outcome
                await Task.Delay(100);

                Assert.NotNull(mockOutput.Output);
                Assert.True(mockOutput.Output.Count() == 1);
            }
            finally
            {
                // Fixture teardown
                mockOutput = null;
                dummyHealthReporter = null;
            }
        }
    }
}
