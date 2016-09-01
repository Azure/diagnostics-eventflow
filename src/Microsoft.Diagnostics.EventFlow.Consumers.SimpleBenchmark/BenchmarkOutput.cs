// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Diagnostics.EventFlow;

namespace Microsoft.Diagnostics.EventFlow.Consumers.SimpleBenchmark
{
    internal class BenchmarkOutput : IOutput
    {
        private string name;
        private long eventCount;

        public long EventCount { get { return this.eventCount; }  }

        public string Summary
        {
            get { return $"Output {this.name} processed {this.eventCount} events"; }
        }

        public BenchmarkOutput(string name)
        {
            this.name = name;
            this.eventCount = 0;
        }

        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            Interlocked.Add(ref this.eventCount, events.Count);
            return Task.CompletedTask;
        }

        public void ResetCounter()
        {
            this.eventCount = 0;
        }
    }
}
