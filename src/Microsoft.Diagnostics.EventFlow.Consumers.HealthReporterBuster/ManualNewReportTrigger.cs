using System;
using Microsoft.Diagnostics.EventFlow.Core.Implementations.HealthReporters;
namespace Microsoft.Diagnostics.EventFlow.Consumers.HealthReporterBuster
{
    internal class ManualNewReportTrigger : INewReportTrigger
    {
        public event EventHandler<EventArgs> Triggered;

        public void TriggerChange()
        {
            Triggered?.Invoke(this, EventArgs.Empty);
        }
    }
}
