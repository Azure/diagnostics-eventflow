// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class UnitTestOutput : IOutput
    {
        public TimeSpan SendEventsDelay = TimeSpan.Zero;
        public int CallCount = 0;
        public int EventCount = 0;
        public bool DisregardCancellationToken = false;
        public Func<long, bool> FailureCondition = null;

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
        }
    }

    public class UnitTestOutputFactory : IPipelineItemFactory<UnitTestOutput>
    {
        public UnitTestOutput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            return new UnitTestOutput();
        }
    }
}
