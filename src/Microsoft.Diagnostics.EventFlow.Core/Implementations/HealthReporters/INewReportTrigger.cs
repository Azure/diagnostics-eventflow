using System;

namespace Microsoft.Diagnostics.EventFlow.Core.Implementations.HealthReporters
{
    internal interface INewReportTrigger
    {
        // Event triggers when new report should be requested.
        event EventHandler<EventArgs> Triggered;
    }
}
