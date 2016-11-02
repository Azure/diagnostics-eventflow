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

namespace Microsoft.Diagnostics.EventFlow
{
    public class DiagnosticPipeline : IDisposable
    {
        private List<IDisposable> pipelineDisposables;
        private List<IDisposable> inputSubscriptions;
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
            bool disposeDependencies = false)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(inputs, nameof(inputs));
            Requires.Argument(inputs.Count > 0, nameof(inputs), "There must be at least one input");
            Requires.NotNull(sinks, nameof(sinks));
            Requires.Argument(sinks.Count > 0, nameof(sinks), "There must be at least one sink");

            this.pipelineConfiguration = pipelineConfiguration ?? new DiagnosticPipelineConfiguration();

            this.Inputs = inputs;
            this.Sinks = sinks;
            // Just play nice and make sure there is always something to enumerate on
            this.GlobalFilters = globalFilters ?? new IFilter[0];
            this.HealthReporter = healthReporter;
            this.cancellationTokenSource = new CancellationTokenSource();
            var propagateCompletion = new DataflowLinkOptions() { PropagateCompletion = true };
            this.pipelineDisposables = new List<IDisposable>();

            var inputBuffer = new BufferBlock<EventData>(
                new DataflowBlockOptions()
                {
                    BoundedCapacity = this.pipelineConfiguration.PipelineBufferSize,
                    CancellationToken = this.cancellationTokenSource.Token
                });
            this.pipelineHead = inputBuffer;

            var batcher = new BatchBlock<EventData>(
                    this.pipelineConfiguration.MaxEventBatchSize,
                    new GroupingDataflowBlockOptions()
                    {
                        BoundedCapacity = this.pipelineConfiguration.MaxEventBatchSize,
                        CancellationToken = this.cancellationTokenSource.Token
                    }
                );
            this.pipelineDisposables.Add(inputBuffer.LinkTo(batcher, propagateCompletion));

            this.pipelineDisposables.Add(new Timer(
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
                    this.pipelineConfiguration.MaxEventBatchSize,
                    this.pipelineConfiguration.MaxConcurrency,
                    healthReporter);
                var globalFiltersBlock = filterTransform.GetFilterBlock();
                this.pipelineDisposables.Add(batcher.LinkTo(globalFiltersBlock, propagateCompletion));
                sinkSource = globalFiltersBlock;
            }
            else
            {
                sinkSource = batcher;
            }

            if (sinks.Count > 1)
            {
                var broadcaster = new BroadcastBlock<EventData[]>(
                    (events) => events?.Select((e) => e.DeepClone()).ToArray(),
                    new DataflowBlockOptions()
                    {
                        BoundedCapacity = this.pipelineConfiguration.MaxEventBatchSize,
                        CancellationToken = this.cancellationTokenSource.Token
                    });
                this.pipelineDisposables.Add(sinkSource.LinkTo(broadcaster, propagateCompletion));
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
                        this.pipelineConfiguration.MaxEventBatchSize,
                        this.pipelineConfiguration.MaxConcurrency,
                        healthReporter);
                    var filterBlock = filterTransform.GetFilterBlock();
                    this.pipelineDisposables.Add(sinkSource.LinkTo(filterBlock, propagateCompletion));
                    outputSource = filterBlock;
                }

                OutputAction outputAction = new OutputAction(
                    sink.Output,
                    this.cancellationTokenSource.Token,
                    this.pipelineConfiguration.MaxEventBatchSize,
                    this.pipelineConfiguration.MaxConcurrency,
                    healthReporter);
                ActionBlock<EventData[]> outputBlock = outputAction.GetOutputBlock();
                this.pipelineDisposables.Add(outputSource.LinkTo(outputBlock, propagateCompletion));
            }

            IObserver<EventData> inputBufferObserver = new TargetBlockObserver<EventData>(inputBuffer, this.HealthReporter);
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

            this.pipelineHead.Complete();
            this.pipelineHead.Completion.Wait(TimeSpan.FromMilliseconds(this.pipelineConfiguration.PipelineCompletionTimeoutMsec));

            this.cancellationTokenSource.Cancel();
            DisposeOf(this.pipelineDisposables);

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

            public FilterAction(
                IReadOnlyCollection<IFilter> filters,
                CancellationToken cancellationToken,
                int boundedCapacity,
                int maxDegreeOfParallelism,
                IHealthReporter healthReporter)
            {
                Requires.NotNull(filters, nameof(filters));
                Requires.Range(boundedCapacity > 0, nameof(boundedCapacity));
                Requires.Range(maxDegreeOfParallelism > 0, nameof(maxDegreeOfParallelism));
                Requires.NotNull(healthReporter, nameof(healthReporter));

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
            }

            public EventData[] FilterAndDecorate(EventData[] eventsToFilter)
            {
                if (eventsToFilter == null)
                {
                    return null;
                }

                // CONSIDER: using a Queue<T> with external lock and be able to reuse the queue
                // (ConcurrentQueue has no Clear() method while ordinary Queue does).
                ConcurrentQueue<EventData> eventsToKeep = new ConcurrentQueue<EventData>();
                Parallel.ForEach(eventsToFilter, this.parallelOptions, eventData =>
                {
                    try
                    {
                        if (this.filters.All(f => f.Evaluate(eventData) == FilterResult.KeepEvent))
                        {
                            eventsToKeep.Enqueue(eventData);
                        }
                    }
                    catch (Exception e)
                    {
                        this.healthReporter.ReportWarning(
                            nameof(DiagnosticPipeline) + ": a filter has thrown an exception" + Environment.NewLine + e.ToString(),
                            EventFlowContextIdentifiers.Filtering);
                    }
                });

                return eventsToKeep.ToArray();
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
                IHealthReporter healthReporter)
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
                }
            }

            public ActionBlock<EventData[]> GetOutputBlock()
            {
                return new ActionBlock<EventData[]>(this.SendDataAsync, this.executionDataflowBlockOptions);
            }
        }
    }
}
