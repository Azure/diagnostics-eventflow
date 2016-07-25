// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Microsoft.Diagnostics.Pipeline
{
    internal class BufferingEventDecorator: IObserver<EventData>
    {
        public BufferingEventDecorator(
            int eventBufferSize,
            uint maxConcurrency,
            TimeSpan noEventsDelay,
            IHealthReporter healthReporter, 
            IReadOnlyCollection<IEventSender> senders, 
            IEnumerable<Action<EventData>> decorators)
        {
            Requires.Range(eventBufferSize > 0, nameof(eventBufferSize));
            Requires.Range(maxConcurrency > 0, nameof(maxConcurrency));
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(senders, nameof(senders));
            Requires.That(senders.Count > 0, nameof(senders), "There should be at least one event sender");
            

        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(EventData value)
        {
            throw new NotImplementedException();
        }
    }
}
