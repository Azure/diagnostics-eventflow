using System;

namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    internal interface INewReportFileTrigger
    {
        // Event triggers when new report file should be requested.
        event EventHandler<EventArgs> NewReportFileRequested;
    }
}
