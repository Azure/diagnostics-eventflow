// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class DiagnosticsPipeline<EventData>: IDisposable
    {
        private IEnumerable<ConcurrentEventProcessor<EventData>> processors;
        private List<IDisposable> subscriptions;
        private bool disposed;

        public DiagnosticsPipeline(
            IHealthReporter healthReporter,
            IReadOnlyCollection<IObservable<EventData>> sources, 
            IReadOnlyCollection<EventSink<EventData>> sinks)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(sources, nameof(sources));
            Requires.Argument(sources.Count > 0, nameof(sources), "There must be at least one source");
            Requires.NotNull(sinks, nameof(sinks));
            Requires.Argument(sinks.Count > 0, nameof(sinks), "There must be at least one sink");

            this.processors = sinks.Select(sink => new ConcurrentEventProcessor<EventData>(
                    eventBufferSize: 1000,
                    maxConcurrency: 4,
                    batchSize: 100,
                    noEventsDelay: TimeSpan.FromMilliseconds(500),
                    sink: sink,
                    healthReporter: healthReporter));

            this.subscriptions = new List<IDisposable>(sources.Count * sinks.Count);

            foreach(var source in sources)
            {
                foreach (var processor in this.processors)
                {
                    this.subscriptions.Add(source.Subscribe(processor));
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
    }
}
