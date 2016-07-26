// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Microsoft.Diagnostics.Pipeline
{
    //
    // CONSIDER: 
    //   1. Reuse buffers during event processing
    //   2. Have a fixed-size buffer of EventData instances and reuse them as necessary
    //
    internal class BufferingEventProcessor<EventDataType> : IObserver<EventDataType>, IDisposable
    {
        private const int BatchSize = 100;
        private const int MaxConcurrency = 4;

        private BlockingCollection<EventDataType> events;
        private TimeSpan noEventsDelay;
        private TimeSpanThrottle eventLossThrottle;
        private IHealthReporter healthReporter;
        private CancellationTokenSource cts;
        private IReadOnlyCollection<IEventSender<EventDataType>> senders;
        private IEnumerable<Func<EventDataType, bool>> filters;
        private readonly int MaxOutstandingSenderTasks;

        public BufferingEventProcessor(
            int eventBufferSize,
            uint maxConcurrency,
            TimeSpan noEventsDelay,
            IHealthReporter healthReporter,
            IReadOnlyCollection<IEventSender<EventDataType>> senders,
            IEnumerable<Func<EventDataType, bool>> filters)
        {
            Requires.Range(eventBufferSize > 0, nameof(eventBufferSize));
            Requires.Range(maxConcurrency > 0, nameof(maxConcurrency));
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(senders, nameof(senders));
            Requires.That(senders.Count > 0, nameof(senders), "There should be at least one event sender");

            this.events = new BlockingCollection<EventDataType>(eventBufferSize);
            this.noEventsDelay = noEventsDelay;
            this.healthReporter = healthReporter;
            this.senders = senders;
            this.filters = filters;

            // Probably does not make sense to report event loss more often than once per second.
            this.eventLossThrottle = new TimeSpanThrottle(TimeSpan.FromSeconds(1));

            this.cts = new CancellationTokenSource();
            Task.Run(() => this.EventConsumerAsync(this.cts.Token));
        }

        public void Dispose()
        {
            if (this.cts.IsCancellationRequested)
            {
                // Already disposed
                return;
            }

            this.cts.Cancel();
        }

        public void OnCompleted()
        {
            this.Dispose();
        }

        public void OnError(Exception error)
        {
            string description = "Event source reported a problem";
            if (error != null)
            {
                description += "\n" + error.ToString();
            }
            this.healthReporter.ReportProblem(description);
        }

        public void OnNext(EventDataType eventData)
        {
            if (!this.events.TryAdd(eventData))
            {
                // Just drop the event. 
                this.eventLossThrottle.Execute(
                    () => { this.healthReporter.ReportProblem("Diagnostic events buffer overflow occurred. Some diagnostics data was lost."); });
            }
        }

        private async Task EventConsumerAsync(CancellationToken cancellationToken)
        {
            List<Task> transmitterTasks = new List<Task>(MaxConcurrency);
            long transmissionSequenceNumber = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                IList<EventDataType> eventsToFilter;
                int eventsFetched;
                if (!this.GetBatch(cancellationToken, out eventsToFilter, out eventsFetched))
                {
                    break;
                }

                // Filter and decorate events
                ConcurrentBag<EventDataType> eventsToSend = FilterAndDecorate(cancellationToken, eventsToFilter);
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (eventsToSend.Count == 0)
                {
                    continue;
                }

                IEnumerable<Task> senderTasks = this.senders.Select(sender => sender.SendEvents(eventsToSend, transmissionSequenceNumber, cancellationToken));
                transmissionSequenceNumber++;
                transmitterTasks.AddRange(senderTasks);
            }
        }

        private ConcurrentBag<EventDataType> FilterAndDecorate(CancellationToken cancellationToken, IList<EventDataType> eventsToFilter)
        {
            ConcurrentBag<EventDataType> eventsToSend;
            if (this.filters != null)
            {
                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.CancellationToken = cancellationToken;
                eventsToSend = new ConcurrentBag<EventDataType>();
                Parallel.ForEach(eventsToFilter, parallelOptions, eventData =>
                {
                    if (this.filters.All(filter => filter(eventData)))
                    {
                        eventsToSend.Add(eventData);
                    }
                });
            }
            else
            {
                eventsToSend = new ConcurrentBag<EventDataType>(eventsToFilter);
            }

            return eventsToSend;
        }

        private bool GetBatch(CancellationToken cancellationToken, out IList<EventDataType> eventsToSend, out int eventsFetched)
        {
            List<EventDataType> events = new List<EventDataType>(BatchSize);
            EventDataType eData;
            eventsFetched = 0;
            eventsToSend = null;

            try
            {
                eData = this.events.Take(cancellationToken);
                events.Add(eData);
                eventsFetched = 1;
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            while (eventsFetched < BatchSize)
            {
                if (!this.events.TryTake(out eData))
                {
                    break;
                }
                events.Add(eData);
                eventsFetched++;
            }

            eventsToSend = events;
            return true;
        }
    }
}
