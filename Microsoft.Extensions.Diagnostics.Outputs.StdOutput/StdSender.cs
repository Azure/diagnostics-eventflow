using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Diagnostics.Outputs.StdOutput
{
    public class StdSender : EventDataSender
    {
        public static readonly string TraceTag = nameof(StdSender);

        public StdSender(IHealthReporter healthReporter) : base(healthReporter)
        {
            this.healthReporter.ReportMessage($"Initializing.", TraceTag);
            this.healthReporter.ReportMessage($"Initialized.", TraceTag);
        }

        public override Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            try
            {
                this.healthReporter.ReportMessage($"Entering SendEvents in batch.", TraceTag);
                Parallel.ForEach(events, e =>
                {
                    this.healthReporter.ReportMessage($"Sending an event.", TraceTag);
                    string eventString = JsonConvert.SerializeObject(events);
                    string output = $"[{transmissionSequenceNumber}] {eventString}";

                    Console.WriteLine(output);
                    this.healthReporter.ReportMessage($"Event Sent.", TraceTag);
                });

                return Task.CompletedTask;
            }
            finally
            {
                this.healthReporter.ReportMessage($"Exit SendEvents in batch.", TraceTag);
            }
        }
    }
}
