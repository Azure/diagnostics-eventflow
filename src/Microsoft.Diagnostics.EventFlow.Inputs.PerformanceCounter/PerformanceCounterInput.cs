// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Validation;
using System.Diagnostics;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class PerformanceCounterInput : IObservable<EventData>, IDisposable
    {
        private static readonly string MetricValueProperty = "Value";
        private static readonly string CounterNameProperty = "CounterName";
        private static readonly string CounterCategoryProperty = "CounterCategory";
        private static readonly string ProcessNameProperty = "ProcessName";
        private static readonly string ProcessIdProperty = "ProcessId";

        private static TimeSpan MinimumCollectionInterval = TimeSpan.FromMilliseconds(100);
        private static readonly string PerformanceCounterInputTypeName = typeof(PerformanceCounterInput).FullName;

        private EventFlowSubject<EventData> subject;
        private object syncObject;
        private Timer collectionTimer;
        private TimeSpan sampleInterval;
        private List<TrackedPerformanceCounter> trackedPerformanceCounters;
        private IHealthReporter healthReporter;
        private ProcessInstanceNameCache processInstanceNameCache;
        private string currentProcessName;
        private int currentProcessId;        

        public PerformanceCounterInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            var inputConfiguration = new PerformanceCounterInputConfiguration();
            try
            {
                configuration.Bind(inputConfiguration);
            }
            catch (Exception e)
            {
                healthReporter.ReportProblem($"{nameof(PerformanceCounterInput)}: an error occurred when reading configuration{Environment.NewLine}{e.ToString()}");
                return;
            }

            Initialize(inputConfiguration, healthReporter);
        }

        public PerformanceCounterInput(PerformanceCounterInputConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            Initialize(configuration, healthReporter);
        }

        private void Initialize(PerformanceCounterInputConfiguration configuration, IHealthReporter healthReporter)
        {
            this.syncObject = new object();
            this.subject = new EventFlowSubject<EventData>();
            this.healthReporter = healthReporter;
            this.processInstanceNameCache = new ProcessInstanceNameCache();
            this.sampleInterval = TimeSpan.FromMilliseconds(configuration.SampleIntervalMsec);
            var currentProcess = Process.GetCurrentProcess();
            this.currentProcessName = currentProcess.ProcessName;
            this.currentProcessId = currentProcess.Id;

            // The CLR Process ID counter used for process ID to counter instance name mapping for CLR counters will not read correctly
            // until at least one garbage collection is performed, so we will force one now. 
            // The listener is usually created during service startup so the GC should not take very long.
            GC.Collect();

            this.trackedPerformanceCounters = new List<TrackedPerformanceCounter>();

            foreach (var counterConfiguration in configuration.Counters)
            {
                if (!counterConfiguration.Validate())
                {
                    healthReporter.ReportProblem($"{nameof(PerformanceCounterInput)}: configuration for counter {counterConfiguration.CounterName} is invalid");
                }
                else
                {
                    this.trackedPerformanceCounters.Add(new TrackedPerformanceCounter(counterConfiguration));
                }
            }

            this.collectionTimer = new Timer(this.DoCollection, null, this.sampleInterval, TimeSpan.FromDays(1));
        }

        private void DoCollection(object state)
        {
            float counterValue;

            lock (this.syncObject)
            {
                var collectionStartTime = DateTime.Now;

                this.processInstanceNameCache.Clear();

                foreach (TrackedPerformanceCounter counter in this.trackedPerformanceCounters)
                {
                    try
                    {
                        if (counter.SampleNextValue(this.processInstanceNameCache, out counterValue))
                        {
                            EventData d = new EventData();
                            d.Payload[CounterNameProperty] = counter.Configuration.CounterName;
                            d.Payload[CounterCategoryProperty] = counter.Configuration.CounterCategory;
                            d.Payload[MetricValueProperty] = counterValue;
                            d.Payload[ProcessIdProperty] = this.currentProcessId;
                            d.Payload[ProcessNameProperty] = this.currentProcessName;
                            d.Timestamp = DateTimeOffset.UtcNow;
                            d.Level = LogLevel.Informational;
                            d.ProviderName = PerformanceCounterInputTypeName;                            
                            this.subject.OnNext(d);
                        }
                    }
                    catch (Exception e)
                    {
                        healthReporter.ReportProblem(
                            $"{nameof(PerformanceCounterInput)}: an error occurred when sampling performance counter {counter.Configuration.CounterName} "
                            + $"in category {counter.Configuration.CounterCategory}{Environment.NewLine}{e.ToString()}");
                    }
                }

                TimeSpan dueTime = this.sampleInterval - (DateTime.Now - collectionStartTime);
                if (dueTime < MinimumCollectionInterval)
                {
                    dueTime = MinimumCollectionInterval;
                }
                this.collectionTimer.Change(dueTime, TimeSpan.FromDays(1));
            }
        }

        public void Dispose()
        {
            lock (this.syncObject)
            {
                this.collectionTimer.Dispose();
                this.subject.Dispose();
                foreach (TrackedPerformanceCounter counter in this.trackedPerformanceCounters)
                {
                    counter.Dispose();
                }
                this.trackedPerformanceCounters.Clear();
            }
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }
    }
}
