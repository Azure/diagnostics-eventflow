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
        private List<IDisposable> inputSubscriptions;
        private IDisposable batcherTimer;
        private List<Task> outputCompletionTasks;
        private bool disposed;
        private bool disposeDependencies;
        private CancellationTokenSource cancellationTokenSource;
        private IDataflowBlock pipelineHead;
        private DiagnosticPipelineConfiguration pipelineConfiguration;

        public DiagnosticPipeline(
            IHealthReporter healthReporter,
            IReadOnlyCollection<IObservable<EventData>> inputs,
            IReadOnlyCollection<IFilter> globalFilters,
            IReadOnlyCollection<EventSink> sinks,
            DiagnosticPipelineConfiguration pipelineConfiguration = null,
            bool disposeDependencies = false,
            TaskScheduler taskScheduler = null)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(inputs, nameof(inputs));
            Requires.Argument(inputs.Count > 0, nameof(inputs), "There must be at least one input");
            Requires.NotNull(sinks, nameof(sinks));
            Requires.Argument(sinks.Count > 0, nameof(sinks), "There must be at least one sink");

            this.pipelineConfiguration = pipelineConfiguration ?? new DiagnosticPipelineConfiguration();
            taskScheduler = taskScheduler ?? TaskScheduler.Current;

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

            // One disposable for each input subscription.
            this.inputSubscriptions = new List<IDisposable>(inputs.Count);

            var inputBuffer = new BufferBlock<EventData>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = this.pipelineConfiguration.PipelineBufferSize,
                    CancellationToken = this.cancellationTokenSource.Token,
                    TaskScheduler = taskScheduler
                });
            this.pipelineHead = inputBuffer;

            var batcher = new BatchBlock<EventData>(
                    this.pipelineConfiguration.MaxEventBatchSize,
                    new GroupingDataflowBlockOptions()
                    {
                        BoundedCapacity = this.pipelineConfiguration.PipelineBufferSize,
                        CancellationToken = this.cancellationTokenSource.Token,
                        TaskScheduler = taskScheduler
                    }
                );
            inputBuffer.LinkTo(batcher, propagateCompletion);

            this.batcherTimer = new Timer(
                (unused) => { try { batcher.TriggerBatch(); } catch {} },
                state: null,
                dueTime: TimeSpan.FromMilliseconds(this.pipelineConfiguration.MaxBatchDelayMsec),
                period: TimeSpan.FromMilliseconds(this.pipelineConfiguration.MaxBatchDelayMsec));

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
                    taskScheduler);
                var globalFiltersBlock = filterTransform.GetFilterBlock();
                batcher.LinkTo(globalFiltersBlock, propagateCompletion);
                sinkSource = globalFiltersBlock;
            }
            else
            {
                sinkSource = batcher;
            }

            bool usingBroadcastBlock = sinks.Count > 1;
            if (usingBroadcastBlock)
            {
                var broadcaster = new BroadcastBlock<EventData[]>(
                    (events) => events?.Select((e) => e.DeepClone()).ToArray(),
                    new DataflowBlockOptions()
                    {
                        BoundedCapacity = MaxNumberOfBatchesInProgress,
                        CancellationToken = this.cancellationTokenSource.Token,
                        TaskScheduler = taskScheduler
                    });
                sinkSource.LinkTo(broadcaster, propagateCompletion);
                sinkSource = broadcaster;
            }

            this.outputCompletionTasks = new List<Task>(sinks.Count);
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
                        taskScheduler);
                    var filterBlock = filterTransform.GetFilterBlock();

                    if (usingBroadcastBlock)
                    {
                        var lossReportingPropagator = new LossReportingPropagatorBlock<EventData[]>(this.HealthReporter);
                        sinkSource.LinkTo(lossReportingPropagator, propagateCompletion);
                        lossReportingPropagator.LinkTo(filterBlock, propagateCompletion);
                    }
                    else
                    {
                        sinkSource.LinkTo(filterBlock, propagateCompletion);
                    }
                    outputSource = filterBlock;
                }
                else if (usingBroadcastBlock)
                {
                    var lossReportingPropagator = new LossReportingPropagatorBlock<EventData[]>(this.HealthReporter);
                    sinkSource.LinkTo(lossReportingPropagator, propagateCompletion);
                    outputSource = lossReportingPropagator;
                }

                OutputAction outputAction = new OutputAction(
                    sink.Output,
                    this.cancellationTokenSource.Token,
                    MaxNumberOfBatchesInProgress,
                    this.pipelineConfiguration.MaxConcurrency,
                    healthReporter,
                    taskScheduler);
                var outputBlock = outputAction.GetOutputBlock();
                outputSource.LinkTo(outputBlock, propagateCompletion);
                this.outputCompletionTasks.Add(outputBlock.Completion);
            }

            IObserver<EventData> inputBufferObserver = new TargetBlockObserver<EventData>(inputBuffer, this.HealthReporter);            
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

            pipelineHead.Complete();
            // The completion should propagate all the way to the outputs. When all outputs complete, the pipeline has been drained successfully.
            Task.WhenAny(Task.WhenAll(this.outputCompletionTasks.ToArray()), Task.Delay(this.pipelineConfiguration.PipelineCompletionTimeoutMsec)).GetAwaiter().GetResult();

            this.cancellationTokenSource.Cancel();

            this.batcherTimer.Dispose();

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

        private class FilterAction
        {
            private IReadOnlyCollection<IFilter> filters;
            private ParallelOptions parallelOptions;
            private ExecutionDataflowBlockOptions executionDataflowBlockOptions;
            private IHealthReporter healthReporter;

            // The stacks in the pool are used as temporary containers for filtered events.
            // Pooling them avoids creation of a new stack every time the filter action is invoked.
            private ConcurrentBag<ConcurrentStack<EventData>> stackPool;

            public FilterAction(
                IReadOnlyCollection<IFilter> filters,
                CancellationToken cancellationToken,
                int boundedCapacity,
                int maxDegreeOfParallelism,
                IHealthReporter healthReporter,
                TaskScheduler taskScheduler)
            {
                Requires.NotNull(filters, nameof(filters));
                Requires.Range(boundedCapacity > 0, nameof(boundedCapacity));
                Requires.Range(maxDegreeOfParallelism > 0, nameof(maxDegreeOfParallelism));
                Requires.NotNull(healthReporter, nameof(healthReporter));

                this.filters = filters;
                this.healthReporter = healthReporter;

                this.parallelOptions = new ParallelOptions();
                parallelOptions.CancellationToken = cancellationToken;
                parallelOptions.TaskScheduler = taskScheduler;

                this.executionDataflowBlockOptions = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    SingleProducerConstrained = false,
                    CancellationToken = cancellationToken,
                    TaskScheduler = taskScheduler
                };
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

            public OutputAction(
                IOutput output,
                CancellationToken cancellationToken,
                int boundedCapacity,
                int maxDegreeOfParallelism,
                IHealthReporter healthReporter,
                TaskScheduler taskScheduler)
            {
                Requires.NotNull(output, nameof(output));
                Requires.NotNull(healthReporter, nameof(healthReporter));

                this.output = output;
                this.cancellationToken = cancellationToken;
                this.healthReporter = healthReporter;

                this.executionDataflowBlockOptions = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    SingleProducerConstrained = false,
                    CancellationToken = cancellationToken,
                    TaskScheduler = taskScheduler
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
                }
            }

            public ActionBlock<EventData[]> GetOutputBlock()
            {
                return new ActionBlock<EventData[]>(this.SendDataAsync, this.executionDataflowBlockOptions);
            }
        }
    }
}
