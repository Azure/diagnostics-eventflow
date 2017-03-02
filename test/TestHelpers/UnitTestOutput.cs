// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public class UnitTestOutput : IOutput
    {
        private UnitTestOutputConfiguration configuration;

        public TimeSpan SendEventsDelay = TimeSpan.Zero;
        public int CallCount = 0;
        public int EventCount = 0;
        public bool DisregardCancellationToken = false;
        public Func<long, bool> FailureCondition = null;
        public ConcurrentQueue<EventData> CapturedEvents = new ConcurrentQueue<EventData>();

        public UnitTestOutput(UnitTestOutputConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (this.SendEventsDelay != TimeSpan.Zero)
            {
                if (DisregardCancellationToken)
                {
                    await Task.Delay(SendEventsDelay);
                }
                else
                {
                    await Task.Delay(SendEventsDelay, cancellationToken);
                }
            }

            Interlocked.Increment(ref CallCount);

            if (this.FailureCondition != null && this.FailureCondition(transmissionSequenceNumber))
            {
                throw new Exception($"Failed to send batch {transmissionSequenceNumber}");
            }

            Interlocked.Add(ref EventCount, events.Count);
            if (this.configuration.PreserveEvents)
            {
                foreach(EventData e in events)
                {
                    this.CapturedEvents.Enqueue(e);
                }
            }
        }
    }

    public class UnitTestOutputFactory : IPipelineItemFactory<UnitTestOutput>
    {
        public UnitTestOutput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            var unitTestOutputConfiguration = new UnitTestOutputConfiguration();
            configuration.Bind(unitTestOutputConfiguration);
            return new UnitTestOutput(unitTestOutputConfiguration);
        }
    }

    public class UnitTestOutputConfiguration
    {
        public bool PreserveEvents { get; set; }
    }
}
