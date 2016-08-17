// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class MockOutput : EventDataSender
    {
        public IEnumerable<EventData> Output
        {
            get
            {
                return _outputs.IsValueCreated ? _outputs.Value : null;
            }
        }

        private readonly Lazy<IList<EventData>> _outputs = new Lazy<IList<EventData>>(() =>
        {
            return new List<EventData>();
        }, false);

        public MockOutput(IHealthReporter healthReporter)
            : base(healthReporter)
        {
        }

        public override Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            foreach (EventData @event in events)
            {
                _outputs.Value.Add(@event);
            }
            return Task.CompletedTask;
        }
    }
}
