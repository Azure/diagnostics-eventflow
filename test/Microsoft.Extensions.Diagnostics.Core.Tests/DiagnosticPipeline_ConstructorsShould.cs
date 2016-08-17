// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Inputs;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Core.Tests
{
    public class DiagnosticPipeline_ConstructorsShould
    {
        [Fact(DisplayName = "DiagnosticsPipeline constructor should require health reporter")]
        public void RequireHealthReport()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                DiagnosticsPipeline<EventData> pipeline = new DiagnosticsPipeline<EventData>(
                    null,
                    new List<TraceInput>(),
                    new List<EventSink<EventData>>());
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: healthReporter", ex.Message);
        }
    }
}
