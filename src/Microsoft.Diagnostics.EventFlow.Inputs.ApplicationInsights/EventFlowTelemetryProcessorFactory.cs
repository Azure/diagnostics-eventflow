// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Diagnostics.EventFlow.ApplicationInsights
{
    public class EventFlowTelemetryProcessorFactory: ITelemetryProcessorFactory
    {
        private DiagnosticPipeline pipeline_;

        public EventFlowTelemetryProcessorFactory(DiagnosticPipeline pipeline) { pipeline_ = pipeline; }

        public ITelemetryProcessor Create(ITelemetryProcessor next)
        {
            var processor = new EventFlowTelemetryProcessor(next);
            processor.Pipeline = pipeline_;
            return processor;
        }
    }
}
