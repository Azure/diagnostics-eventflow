// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class DiagnosticsPipeline<EventDataType>: IDisposable
    {
        private IEnumerable<ConcurrentEventProcessor<EventDataType>> processors;
        private List<IDisposable> subscriptions;
        private bool disposed;

        public DiagnosticsPipeline(
            IHealthReporter healthReporter,
            IReadOnlyCollection<IObservable<EventDataType>> inputs, 
            IReadOnlyCollection<EventSink<EventDataType>> sinks)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(inputs, nameof(inputs));
            Requires.Argument(inputs.Count > 0, nameof(inputs), "There must be at least one input");
            Requires.NotNull(sinks, nameof(sinks));
            Requires.Argument(sinks.Count > 0, nameof(sinks), "There must be at least one sink");

            this.Inputs = inputs;
            this.Sinks = sinks;

            this.processors = sinks.Select(sink => new ConcurrentEventProcessor<EventDataType>(
                    eventBufferSize: 1000,
                    maxConcurrency: 4,
                    batchSize: 100,
                    noEventsDelay: TimeSpan.FromMilliseconds(500),
                    sink: sink,
                    healthReporter: healthReporter));

            this.subscriptions = new List<IDisposable>(inputs.Count * sinks.Count);

            foreach(var input in inputs)
            {
                foreach (var processor in this.processors)
                {
                    this.subscriptions.Add(input.Subscribe(processor));
                }
            }

            this.disposed = false;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            foreach(var subscription in this.subscriptions)
            {
                subscription.Dispose();
            }

            foreach(var processor in this.processors)
            {
                processor.Dispose();
            }
        }

        public IReadOnlyCollection<IObservable<EventDataType>> Inputs { get; private set; }
        public IReadOnlyCollection<EventSink<EventDataType>> Sinks { get; private set; }
    }
}
