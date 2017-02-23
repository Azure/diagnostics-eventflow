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
using System.Threading.Tasks.Dataflow;
using Validation;

using Microsoft.Diagnostics.EventFlow.Configuration;
using System.Diagnostics;

namespace Microsoft.Diagnostics.EventFlow
{
    public class DiagnosticPipeline : IDisposable
    {
        private List<IDisposable> pipelineLinkDisposables;
        private List<IDisposable> inputSubscriptions;
        private List<Task> pipelineCompletionTasks;
        private bool disposed;
        private bool disposeDependencies;
        private CancellationTokenSource cancellationTokenSource;
        private IDataflowBlock pipelineHead;
        private DiagnosticPipelineConfiguration pipelineConfiguration;
        private volatile int eventsInProgress;

        public DiagnosticPipeline(
            IHealthReporter healthReporter,
            IReadOnlyCollection<IObservable<EventData>> inputs,
            IReadOnlyCollection<IFilter> globalFilters,
            IReadOnlyCollection<EventSink> sinks,
            DiagnosticPipelineConfiguration pipelineConfiguration = null,
            bool disposeDependencies = false)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(inputs, nameof(inputs));
            Requires.Argument(inputs.Count > 0, nameof(inputs), "There must be at least one input");
            Requires.NotNull(sinks, nameof(sinks));
            Requires.Argument(sinks.Count > 0, nameof(sinks), "There must be at least one sink");

            this.eventsInProgress = 0;

            this.pipelineConfiguration = pipelineConfiguration ?? new DiagnosticPipelineConfiguration();

            // An estimatie how many batches of events to allow inside the pipeline.
            // We want to be able to process full buffer of events, but also have enough batches in play in case of high concurrency.
            int MaxNumberOfBatchesInProgress = Math.Max(
                5 * this.pipelineConfiguration.MaxConcurrency,
                this.pipelineConfiguration.PipelineBufferSize / this.pipelineConfiguration.MaxEventBatchSize);

            this.Inputs = inputs;
            this.Sinks = sinks;
            
            // Just play nice and make sure there is always something to enumerate on
            this.GlobalFilters = globalFilters ?? new IFilter[0];

            this.HealthReporter = healthReporter;
            this.cancellationTokenSource = new CancellationTokenSource();
            var propagateCompletion = new DataflowLinkOptions() { PropagateCompletion = true };
            this.pipelineLinkDisposables = new List<IDisposable>();
            this.pipelineCompletionTasks = new List<Task>();

            var inputBuffer = new BufferBlock<EventData>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = this.pipelineConfiguration.PipelineBufferSize,
                    CancellationToken = this.cancellationTokenSource.Token
                });
            this.pipelineHead = inputBuffer;
            this.pipelineCompletionTasks.Add(inputBuffer.Completion);

            var batcher = new BatchBlock<EventData>(
                    this.pipelineConfiguration.MaxEventBatchSize,
                    new GroupingDataflowBlockOptions()
                    {
                        BoundedCapacity = this.pipelineConfiguration.MaxEventBatchSize,
                        CancellationToken = this.cancellationTokenSource.Token
                    }
                );
            this.pipelineLinkDisposables.Add(inputBuffer.LinkTo(batcher, propagateCompletion));
            this.pipelineCompletionTasks.Add(batcher.Completion);

            this.pipelineLinkDisposables.Add(new Timer(
                (unused) => batcher.TriggerBatch(),
                state: null,
                dueTime: TimeSpan.FromMilliseconds(this.pipelineConfiguration.MaxBatchDelayMsec),
                period: TimeSpan.FromMilliseconds(this.pipelineConfiguration.MaxBatchDelayMsec)));

            ISourceBlock<EventData[]> sinkSource;
            FilterAction filterTransform;

            if (this.GlobalFilters.Count > 0)
            {
                filterTransform = new FilterAction(
                    this.GlobalFilters,
                    this.cancellationTokenSource.Token,
                    MaxNumberOfBatchesInProgress,
                    this.pipelineConfiguration.MaxConcurrency,
                    healthReporter,
                    this.OnEventsFilteredOut);
                var globalFiltersBlock = filterTransform.GetFilterBlock();
                this.pipelineLinkDisposables.Add(batcher.LinkTo(globalFiltersBlock, propagateCompletion));
                this.pipelineCompletionTasks.Add(globalFiltersBlock.Completion);
                sinkSource = globalFiltersBlock;
            }
            else
            {
                sinkSource = batcher;
            }

            if (sinks.Count > 1)
            {
                // After broadcasting we will effectively have (sinks.Count - 1) * batch.Length more events in the pipeline, 
                // because the broadcaster is cloning the events for the sake of each sink (filters-output combination).
                var eventCounter = new TransformBlock<EventData[], EventData[]>(
                    (batch) => { Interlocked.Add(ref this.eventsInProgress, (sinks.Count - 1) * batch.Length);  return batch; },
                    new ExecutionDataflowBlockOptions()
                    {
                        BoundedCapacity = MaxNumberOfBatchesInProgress,
                        CancellationToken = this.cancellationTokenSource.Token
                    });
                this.pipelineLinkDisposables.Add(sinkSource.LinkTo(eventCounter, propagateCompletion));
                this.pipelineCompletionTasks.Add(eventCounter.Completion);

                var broadcaster = new BroadcastBlock<EventData[]>(
                    (events) => events?.Select((e) => e.DeepClone()).ToArray(),
                    new DataflowBlockOptions()
                    {
                        BoundedCapacity = MaxNumberOfBatchesInProgress,
                        CancellationToken = this.cancellationTokenSource.Token
                    });
                this.pipelineLinkDisposables.Add(eventCounter.LinkTo(broadcaster, propagateCompletion));
                this.pipelineCompletionTasks.Add(broadcaster.Completion);
                sinkSource = broadcaster;
            }

            foreach (var sink in sinks)
            {
                ISourceBlock<EventData[]> outputSource = sinkSource;
                if (sink.Filters != null && sink.Filters.Count > 0)
                {
                    filterTransform = new FilterAction(
                        sink.Filters,
                        this.cancellationTokenSource.Token,
                        MaxNumberOfBatchesInProgress,
                        this.pipelineConfiguration.MaxConcurrency,
                        healthReporter,
                        this.OnEventsFilteredOut);
                    var filterBlock = filterTransform.GetFilterBlock();
                    this.pipelineLinkDisposables.Add(sinkSource.LinkTo(filterBlock, propagateCompletion));
                    this.pipelineCompletionTasks.Add(filterBlock.Completion);
                    outputSource = filterBlock;
                }

                OutputAction outputAction = new OutputAction(
                    sink.Output,
                    this.cancellationTokenSource.Token,
                    MaxNumberOfBatchesInProgress,
                    this.pipelineConfiguration.MaxConcurrency,
                    healthReporter,
                    (eventsSentCount) => Interlocked.Add(ref this.eventsInProgress, -eventsSentCount));
                var outputBlock = outputAction.GetOutputBlock();
                this.pipelineLinkDisposables.Add(outputSource.LinkTo(outputBlock, propagateCompletion));
                this.pipelineCompletionTasks.Add(outputBlock.Completion);
            }

            IObserver<EventData> inputBufferObserver = new TargetBlockObserver<EventData>(
                inputBuffer, 
                this.HealthReporter,
                () => Interlocked.Increment(ref this.eventsInProgress));
            this.inputSubscriptions = new List<IDisposable>(inputs.Count);
            foreach (var input in inputs)
            {
                this.inputSubscriptions.Add(input.Subscribe(inputBufferObserver));
            }

            this.disposed = false;
            this.disposeDependencies = disposeDependencies;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            DisposeOf(this.inputSubscriptions);

            TimeSpan pipelineDrainWaitTime = PollWaitForPipelineDrain();

            pipelineHead.Complete();
            // We want to give the completion logic some non-zero wait time for the pipeline blocks to dispose of their internal resources.
            TimeSpan completionWaitTime = TimeSpan.FromMilliseconds(Math.Max(100, this.pipelineConfiguration.PipelineCompletionTimeoutMsec - pipelineDrainWaitTime.TotalMilliseconds));
            Task.WaitAll(this.pipelineCompletionTasks.ToArray(), completionWaitTime);

            this.cancellationTokenSource.Cancel();
            DisposeOf(this.pipelineLinkDisposables);

            if (this.disposeDependencies)
            {
                DisposeOf(this.Inputs);
                DisposeOf(this.Sinks);
                HealthReporter.Dispose();
            }
        }

        public IReadOnlyCollection<IObservable<EventData>> Inputs { get; private set; }
        public IReadOnlyCollection<EventSink> Sinks { get; private set; }
        public IReadOnlyCollection<IFilter> GlobalFilters { get; private set; }
        public IHealthReporter HealthReporter { get; private set; }

        private void DisposeOf(IEnumerable<object> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                (item as IDisposable)?.Dispose();
            }
        }

        private TimeSpan PollWaitForPipelineDrain()
        {
            TimeSpan waitTime = TimeSpan.Zero;
            TimeSpan HundredMsec = TimeSpan.FromMilliseconds(100);
            TimeSpan MaxWaitTime = TimeSpan.FromMilliseconds(this.pipelineConfiguration.PipelineCompletionTimeoutMsec);

            while (this.eventsInProgress > 0)
            {
                Thread.Sleep(HundredMsec);
                waitTime += HundredMsec;
                if (waitTime >= MaxWaitTime)
                {
                    break;
                }
            }

            return waitTime;
        }

        private void OnEventsFilteredOut(int eventsRemoved)
        {
            Interlocked.Add(ref this.eventsInProgress, -eventsRemoved);
        }

        private class FilterAction
        {
            private IReadOnlyCollection<IFilter> filters;
            private ParallelOptions parallelOptions;
            private ExecutionDataflowBlockOptions executionDataflowBlockOptions;
            private IHealthReporter healthReporter;
            private Action<int> eventsRemovedNotification;

            // The stacks in the pool are used as temporary containers for filtered events.
            // Pooling them avoids creation of a new stack every time the filter action is invoked.
            private ConcurrentBag<ConcurrentStack<EventData>> stackPool;

            public FilterAction(
                IReadOnlyCollection<IFilter> filters,
                CancellationToken cancellationToken,
                int boundedCapacity,
                int maxDegreeOfParallelism,
                IHealthReporter healthReporter,
                Action<int> eventsRemovedNotification)
            {
                Requires.NotNull(filters, nameof(filters));
                Requires.Range(boundedCapacity > 0, nameof(boundedCapacity));
                Requires.Range(maxDegreeOfParallelism > 0, nameof(maxDegreeOfParallelism));
                Requires.NotNull(healthReporter, nameof(healthReporter));
                Requires.NotNull(eventsRemovedNotification, nameof(eventsRemovedNotification));

                this.filters = filters;
                this.healthReporter = healthReporter;

                this.parallelOptions = new ParallelOptions();
                parallelOptions.CancellationToken = cancellationToken;

                this.executionDataflowBlockOptions = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    SingleProducerConstrained = false,
                    CancellationToken = cancellationToken
                };
                this.eventsRemovedNotification = eventsRemovedNotification;
                this.stackPool = new ConcurrentBag<ConcurrentStack<EventData>>();
            }

            public EventData[] FilterAndDecorate(EventData[] eventsToFilter)
            {
                if (eventsToFilter == null)
                {
                    return null;
                }

                ConcurrentStack<EventData> eventsToKeep;
                if (!this.stackPool.TryTake(out eventsToKeep))
                {
                    eventsToKeep = new ConcurrentStack<EventData>();
                }

                Parallel.ForEach(eventsToFilter, this.parallelOptions, eventData =>
                {
                    try
                    {
                        if (this.filters.All(f => f.Evaluate(eventData) == FilterResult.KeepEvent))
                        {
                            eventsToKeep.Push(eventData);
                        }
                    }
                    catch (Exception e)
                    {
                        this.healthReporter.ReportWarning(
                            nameof(DiagnosticPipeline) + ": a filter has thrown an exception" + Environment.NewLine + e.ToString(),
                            EventFlowContextIdentifiers.Filtering);
                    }
                });

                EventData[] eventsFiltered = eventsToKeep.ToArray();
                eventsToKeep.Clear();
                this.stackPool.Add(eventsToKeep);
                int eventsRemoved = eventsToFilter.Length - eventsFiltered.Length;
                if (eventsRemoved > 0)
                {
                    this.eventsRemovedNotification(eventsRemoved);
                }
                return eventsFiltered;
            }

            public TransformBlock<EventData[], EventData[]> GetFilterBlock()
            {
                return new TransformBlock<EventData[], EventData[]>(
                    (Func<EventData[], EventData[]>)this.FilterAndDecorate,
                    this.executionDataflowBlockOptions);
            }
        }

        private class OutputAction
        {
            private long transmissionSequenceNumber = 0;
            private IOutput output;
            private CancellationToken cancellationToken;
            private ExecutionDataflowBlockOptions executionDataflowBlockOptions;
            private IHealthReporter healthReporter;
            private Action<int> eventsSentNotification;

            public OutputAction(
                IOutput output,
                CancellationToken cancellationToken,
                int boundedCapacity,
                int maxDegreeOfParallelism,
                IHealthReporter healthReporter,
                Action<int> eventsSentNotification)
            {
                Requires.NotNull(output, nameof(output));
                Requires.NotNull(healthReporter, nameof(healthReporter));
                Requires.NotNull(eventsSentNotification, nameof(eventsSentNotification));

                this.output = output;
                this.cancellationToken = cancellationToken;
                this.healthReporter = healthReporter;
                this.eventsSentNotification = eventsSentNotification;

                this.executionDataflowBlockOptions = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    SingleProducerConstrained = false,
                    CancellationToken = cancellationToken
                };
            }

            public async Task SendDataAsync(EventData[] events)
            {
                if (events.Length > 0)
                {
                    try
                    {
                        await this.output.SendEventsAsync(events, Interlocked.Increment(ref transmissionSequenceNumber), this.cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        this.healthReporter.ReportWarning(
                            nameof(DiagnosticPipeline) + ": an output has thrown an exception while sending data" + Environment.NewLine + e.ToString(),
                            EventFlowContextIdentifiers.Output);
                    }
                    finally
                    {
                        this.eventsSentNotification(events.Length);
                    }
                }
            }

            public ActionBlock<EventData[]> GetOutputBlock()
            {
                return new ActionBlock<EventData[]>(this.SendDataAsync, this.executionDataflowBlockOptions);
            }
        }
    }
}
