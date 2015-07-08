using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AirTrafficControl.SharedLib
{
    public class ConcurrentEventSender<EventDataType> : IDisposable
    {
        private BlockingCollection<EventDataType> events;
        private CancellationTokenSource cts;
        private int maxConcurrency;
        private int batchSize;
        private TimeSpan noEventsDelay;
        private Func<IEnumerable<EventDataType>, CancellationToken, Task> TransmitterProc;

        public ConcurrentEventSender(int eventBufferSize, int maxConcurrency, int batchSize, TimeSpan noEventsDelay,
            Func<IEnumerable<EventDataType>, CancellationToken, Task> transmitterProc)
        {
            this.events = new BlockingCollection<EventDataType>(eventBufferSize);

            ValidateConstructorParameters(eventBufferSize, maxConcurrency, batchSize, noEventsDelay, transmitterProc);
            this.maxConcurrency = maxConcurrency;
            this.batchSize = batchSize;
            this.noEventsDelay = noEventsDelay;
            this.TransmitterProc = transmitterProc;

            this.cts = new CancellationTokenSource();
            Task.Run(() => EventConsumerAsync(this.cts.Token));
        }

        public void SubmitEvent(EventDataType eData)
        {
            if (!this.events.TryAdd(eData))
            {
                // Just drop the event. 
                // TODO: there should be a way to indicate that event loss is occurring.
            }
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

        private async Task EventConsumerAsync(CancellationToken cancellationToken)
        {
            List<Task> transmitterTasks = new List<Task>(this.maxConcurrency);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (transmitterTasks.Count == this.maxConcurrency)
                {
                    Task.WaitAny(transmitterTasks.ToArray());
                }

                IEnumerable<EventDataType> transmitterEvents;
                int eventsFetched;
                if (!GetBatch(cancellationToken, out transmitterEvents, out eventsFetched))
                {
                    break;
                }

                Task transmitterTask = Task.Run(() => this.TransmitterProc(transmitterEvents, cancellationToken));
                transmitterTasks.Add(transmitterTask);

                ForgetCompletedTransmitterTasks(transmitterTasks);

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
            var completedTasks = transmitterTasks.Where(t => t.IsCompleted).ToList();
            foreach (Task t in completedTasks)
            {
                transmitterTasks.Remove(t);
            }
        }

        private bool GetBatch(CancellationToken cancellationToken, out IEnumerable<EventDataType> eventsToSend, out int eventsFetched)
        {
            // Consider: reuse event buffers
            var events = new List<EventDataType>(this.batchSize);
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

            while (eventsFetched < batchSize)
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

        private void ValidateConstructorParameters(int eventBufferSize, int maxConcurrency, int batchSize, TimeSpan noEventsDelay,
            Func<IEnumerable<EventDataType>, CancellationToken, Task> transmitterProc)
        {
            if (eventBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("eventBufferSize", "Event buffer size should be greater than zero");
            }

            if (maxConcurrency <= 0)
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
        }
    }
}
