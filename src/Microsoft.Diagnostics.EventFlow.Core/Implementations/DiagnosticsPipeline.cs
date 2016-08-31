// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Validation;

namespace Microsoft.Diagnostics.EventFlow
{
    public class DiagnosticsPipeline: IDisposable
    {
        private IEnumerable<ConcurrentEventProcessor> processors;
        private List<IDisposable> subscriptions;
        private bool disposed;

        public DiagnosticsPipeline(
            IHealthReporter healthReporter,
            IReadOnlyCollection<IObservable<EventData>> inputs, 
            IReadOnlyCollection<EventSink> sinks)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(inputs, nameof(inputs));
            Requires.Argument(inputs.Count > 0, nameof(inputs), "There must be at least one input");
            Requires.NotNull(sinks, nameof(sinks));
            Requires.Argument(sinks.Count > 0, nameof(sinks), "There must be at least one sink");

            this.Inputs = inputs;
            this.Sinks = sinks;
            this.HealthReporter = healthReporter;

            // TODO: cloning should be used only if there are more than one sink and any of them have output-specific filters.
            bool useCloning = sinks.Count() > 1; 

            this.processors = sinks.Select(sink => new ConcurrentEventProcessor(
                    eventBufferSize: 1000,
                    maxConcurrency: 4,
                    batchSize: 100,
                    noEventsDelay: TimeSpan.FromMilliseconds(500),
                    cloneReceivedEvents: useCloning,
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

            HealthReporter.Dispose();
        }

        public IReadOnlyCollection<IObservable<EventData>> Inputs { get; private set; }
        public IReadOnlyCollection<EventSink> Sinks { get; private set; }
        public IHealthReporter HealthReporter { get; private set; }
    }
}
