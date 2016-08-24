// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Diagnostics.Outputs
{
    public class StdOutput : OutputBase
    {
        public static readonly string TraceTag = nameof(StdOutput);

        public StdOutput(IHealthReporter healthReporter) : base(healthReporter)
        {
        }

        public override Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            try
            {
                foreach (EventData evt in events)
                {
                    string eventString = JsonConvert.SerializeObject(evt);
                    string output = $"[{transmissionSequenceNumber}] {eventString}";

                    Console.WriteLine(output);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                this.healthReporter.ReportProblem($"Fail to send events in batch. Error details: {ex.ToString()}");
                return Task.FromException(ex);
            }
        }
    }
}
