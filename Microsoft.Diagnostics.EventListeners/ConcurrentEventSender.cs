// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventListeners
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class ConcurrentEventSender<EventDataType> : IDisposable
    {
        private static readonly TimeSpan EventLossReportInterval = TimeSpan.FromSeconds(5);
        private readonly int capacityWarningThreshold;
        private BlockingCollection<EventDataType> events;
        private CancellationTokenSource cts;
        private uint maxConcurrency;
        private int batchSize;
        private TimeSpan noEventsDelay;
        private Func<IEnumerable<EventDataType>, long, CancellationToken, Task> TransmitterProc;
        private TimeSpanThrottle eventLossThrottle;
        private IHealthReporter healthReporter;

        public ConcurrentEventSender(
            int eventBufferSize, uint maxConcurrency, int batchSize, TimeSpan noEventsDelay,
            Func<IEnumerable<EventDataType>, long, CancellationToken, Task> transmitterProc,
            IHealthReporter healthReporter)
        {
            this.events = new BlockingCollection<EventDataType>(eventBufferSize);

            this.ValidateConstructorParameters(eventBufferSize, maxConcurrency, batchSize, noEventsDelay, transmitterProc, healthReporter);
            this.maxConcurrency = maxConcurrency;
            this.batchSize = batchSize;
            this.noEventsDelay = noEventsDelay;
            this.TransmitterProc = transmitterProc;
            this.capacityWarningThreshold = (int) Math.Ceiling(0.9m*eventBufferSize);
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
        }

        public void SubmitEvent(EventDataType eData)
        {
            if (!this.events.TryAdd(eData))
            {
                // Just drop the event. 
                this.eventLossThrottle.Execute(
                    () => { this.healthReporter.ReportProblem("Diagnostic events buffer overflow occurred. Some diagnostics data was lost."); });
            }
        }

        private async Task EventConsumerAsync(CancellationToken cancellationToken)
        {
            List<Task> transmitterTasks = new List<Task>((int) this.maxConcurrency);
            long transmissionSequenceNumber = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (transmitterTasks.Count == this.maxConcurrency)
                {
                    Task.WaitAny(transmitterTasks.ToArray());
                }

                IEnumerable<EventDataType> transmitterEvents;
                int eventsFetched;
                if (!this.GetBatch(cancellationToken, out transmitterEvents, out eventsFetched))
                {
                    break;
                }

                Task transmitterTask = Task.Run(
                    () => this.TransmitterProc(transmitterEvents, transmissionSequenceNumber++, cancellationToken),
                    cancellationToken);
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

        private void ValidateConstructorParameters(
            int eventBufferSize, uint maxConcurrency, int batchSize, TimeSpan noEventsDelay,
            Func<IEnumerable<EventDataType>, long, CancellationToken, Task> transmitterProc,
            IHealthReporter healthReporter)
        {
            if (eventBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("eventBufferSize", "Event buffer size should be greater than zero");
            }

            if (maxConcurrency == 0)
            {
                throw new ArgumentOutOfRangeException("maxConcurrency", "Max concurrency should be at least one");
            }

            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException("batchSize", "Batch size should be at least one");
            }

            if (transmitterProc == null)
            {
                throw new ArgumentNullException("transmitterProc");
            }

            if (healthReporter == null)
            {
                throw new ArgumentNullException("healthReporter");
            }
        }
    }
}