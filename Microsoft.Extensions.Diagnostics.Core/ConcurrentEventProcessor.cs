// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{

    public class ConcurrentEventProcessor<EventDataType> : IObserver<EventDataType>, IDisposable
    {
        private static readonly TimeSpan EventLossReportInterval = TimeSpan.FromSeconds(5);
        private readonly int capacityWarningThreshold;
        private BlockingCollection<EventDataType> events;
        private CancellationTokenSource cts;
        private uint maxConcurrency;
        private int batchSize;
        private TimeSpan noEventsDelay;
        private EventSink<EventDataType> sink;
        private TimeSpanThrottle eventLossThrottle;
        private IHealthReporter healthReporter;

        public ConcurrentEventProcessor(
            int eventBufferSize,
            uint maxConcurrency,
            int batchSize,
            TimeSpan noEventsDelay,
            EventSink<EventDataType> sink,
            IHealthReporter healthReporter)
        {
            Requires.Range(eventBufferSize > 0, nameof(eventBufferSize));
            Requires.Range(maxConcurrency > 0, nameof(maxConcurrency));
            Requires.Range(batchSize > 0, nameof(batchSize));
            Requires.NotNull(sink, nameof(sink));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.events = new BlockingCollection<EventDataType>(eventBufferSize);

            this.maxConcurrency = maxConcurrency;
            this.batchSize = batchSize;
            this.noEventsDelay = noEventsDelay;
            this.sink = sink;
            this.capacityWarningThreshold = (int)Math.Ceiling(0.9m * eventBufferSize);
            this.healthReporter = healthReporter;

            // Probably does not make sense to report event loss more often than once per second.
            this.eventLossThrottle = new TimeSpanThrottle(TimeSpan.FromSeconds(1));

            this.cts = new CancellationTokenSource();
            Task.Run(() => this.EventConsumerAsync(this.cts.Token));
        }

        public bool ApproachingBufferCapacity
        {
            get { return this.events.Count >= this.capacityWarningThreshold; }
        }

        public void Dispose()
        {
            if (this.cts.IsCancellationRequested)
            {
                // Already disposed
                return;
            }

            this.cts.Cancel();

            this.sink.Dispose();
        }

        public void OnNext(EventDataType eData)
        {
            if (!this.events.TryAdd(eData))
            {
                // Just drop the event. 
                this.eventLossThrottle.Execute(
                    () => { this.healthReporter.ReportProblem("Diagnostic events buffer overflow occurred. Some diagnostics data was lost."); });
            }
        }

        public void OnError(Exception error)
        {
            string description = "Event source reported a problem";
            if (error != null)
            {
                description += "\n" + error.ToString();
            }
            this.healthReporter.ReportProblem(description);
            this.Dispose();
        }

        public void OnCompleted()
        {
            this.Dispose();
        }

        private async Task EventConsumerAsync(CancellationToken cancellationToken)
        {
            List<Task> transmitterTasks = new List<Task>((int)this.maxConcurrency);
            long transmissionSequenceNumber = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (transmitterTasks.Count == this.maxConcurrency)
                {
                    Task.WaitAny(transmitterTasks.ToArray());
                }

                IEnumerable<EventDataType> eventsToFilter;
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

#if NET45
                Task transmitterTask = Task.Run(
                    () => this.sink.Sender.SendEventsAsync(eventsToSend.ToList(), transmissionSequenceNumber++, cancellationToken),
                    cancellationToken);
#elif NETSTANDARD1_6
                Task transmitterTask = Task.Run(
                    () => this.sink.Sender.SendEventsAsync(eventsToSend, transmissionSequenceNumber++, cancellationToken),
                    cancellationToken);
#endif
                transmitterTasks.Add(transmitterTask);

                this.ForgetCompletedTransmitterTasks(transmitterTasks);

                if (eventsFetched < this.batchSize)
                {
                    try
                    {
                        await Task.Delay(this.noEventsDelay, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private ConcurrentBag<EventDataType> FilterAndDecorate(CancellationToken cancellationToken, IEnumerable<EventDataType> eventsToFilter)
        {
            ConcurrentBag<EventDataType> eventsToSend;
            IEnumerable<IEventFilter<EventDataType>> filters = this.sink.Filters;
            if (filters != null)
            {
                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.CancellationToken = cancellationToken;
                eventsToSend = new ConcurrentBag<EventDataType>();
                Parallel.ForEach(eventsToFilter, parallelOptions, eventData =>
                {
                    if (filters.All(f => f.Filter(eventData)))
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

        private void ForgetCompletedTransmitterTasks(List<Task> transmitterTasks)
        {
            List<Task> completedTasks = transmitterTasks.Where(t => t.IsCompleted).ToList();
            foreach (Task t in completedTasks)
            {
                transmitterTasks.Remove(t);
            }
        }

        private bool GetBatch(CancellationToken cancellationToken, out IEnumerable<EventDataType> eventsToSend, out int eventsFetched)
        {
            // Consider: reuse event buffers
            List<EventDataType> events = new List<EventDataType>(this.batchSize);
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

            while (eventsFetched < this.batchSize)
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