// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Configuration;
using Microsoft.Extensions.Diagnostics.Metadata;
using Validation;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Diagnostics
{
    public class PerformanceCounterListener: ThrottledHealthReporter, IObservable<EventData>, IDisposable
    {
        private const int SampleIntervalSeconds = 10;
        internal static readonly string MetricValueProperty = "Value";

        private SimpleSubject<EventData> subject;
        private object syncObject;
        private Timer collectionTimer;
        private List<TrackedPerformanceCounter> trackedPerformanceCounters;
        
        public PerformanceCounterListener(IConfiguration configuration, IHealthReporter healthReporter): base(healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.syncObject = new object();
            this.subject = new SimpleSubject<EventData>();

            // The CLR Process ID counter used for process ID to counter instance name mapping for CLR counters will not read correctly
            // until at least one garbage collection is performed, so we will force one now. 
            // The listener is usually created during service startup so the GC should not take very long.
            GC.Collect();

            try
            {
                var counterConfigurations = new List<PerformanceCounterConfiguration>();
                configuration.Bind(counterConfigurations);

                this.trackedPerformanceCounters = new List<TrackedPerformanceCounter>();
                                
                foreach(var counterConfiguration in counterConfigurations)
                {
                    if (!counterConfiguration.Validate())
                    {
                        healthReporter.ReportProblem($"{nameof(PerformanceCounterListener)}: configuration for counter {counterConfiguration.CounterName} is invalid");
                    }
                    else
                    {
                        this.trackedPerformanceCounters.Add(new TrackedPerformanceCounter(counterConfiguration));
                    }
                }
            }
            catch(Exception e)
            {
                healthReporter.ReportProblem($"{nameof(PerformanceCounterListener)}: an error occurred when reading configuration{Environment.NewLine}{e.ToString()}");
                return;
            }

            this.collectionTimer = new Timer(this.DoCollection, null, TimeSpan.FromSeconds(SampleIntervalSeconds), TimeSpan.FromDays(1));            
        }

        private void DoCollection(object state)
        {
            float counterValue;

            lock(this.syncObject)
            {
                foreach (TrackedPerformanceCounter counter in this.trackedPerformanceCounters)
                {
                    try
                    {
                        if (counter.SampleNextValue(out counterValue))
                        {
                            EventData d = new EventData();
                            d.Payload = new Dictionary<string, object>();
                            d.Payload[MetricValueProperty] = counterValue;
                            d.Timestamp = DateTimeOffset.UtcNow;
                            d.SetMetadata(MetadataKind.Metric, counter.Metadata);
                            this.subject.OnNext(d);
                        }
                    }
                    catch (Exception e)
                    {
                        this.ReportProblem(
                            $"{nameof(PerformanceCounterListener)}: an error occurred when sampling performance counter {counter.Configuration.CounterName} "
                            + $"in category {counter.Configuration.CounterCategory}{Environment.NewLine}{e.ToString()}");
                    }
                }                

                this.collectionTimer.Change(TimeSpan.FromSeconds(SampleIntervalSeconds), TimeSpan.FromDays(1));
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

        private class TrackedPerformanceCounter: IDisposable
        {
            private PerformanceCounter counter;
            private bool disposed;
            private DateTimeOffset lastAccessedOn;
            private string lastInstanceName;

            public PerformanceCounterConfiguration Configuration { get; private set; }
            public PerformanceCounterMetricMetadata Metadata { get; private set; }
            

            public TrackedPerformanceCounter(PerformanceCounterConfiguration configuration)
            {
                Requires.NotNull(configuration, nameof(configuration));

                this.Configuration = configuration;
                this.lastAccessedOn = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
                this.disposed = false;

                this.Metadata = new PerformanceCounterMetricMetadata()
                {
                    MetricName = configuration.MetricName,
                    MetricValueProperty = PerformanceCounterListener.MetricValueProperty
                };
            }

            public bool SampleNextValue(out float newValue)
            {
                newValue = 0;
                if (disposed)
                {
                    return false;
                }

                var now = DateTimeOffset.UtcNow;
                if (now - this.lastAccessedOn < TimeSpan.FromSeconds(this.Configuration.CollectionIntervalInSeconds))
                {
                    return false;                    
                }

                // TODO: for counters that share process ID counter name and category we should be able to cache the instance name 
                // for a single counter collection cycle. In other words, we should be able call GetInstanceNameForCurrentProcess() 
                // only once per cycle for given process ID counter name and category.

                string instanceName = GetInstanceNameForCurrentProcess();
                this.lastAccessedOn = now;
                if (this.counter == null || (!string.IsNullOrEmpty(instanceName) && instanceName != this.lastInstanceName))
                {
                    if (this.counter != null)
                    {
                        this.counter.Dispose();
                    }
                    this.counter = new PerformanceCounter(
                        this.Configuration.CounterCategory,
                        this.Configuration.CounterName,
                        instanceName,
                        readOnly: true);
                }
                this.lastInstanceName = instanceName;

                newValue = this.counter.NextValue();
                return true;
            }

            public void Dispose()
            {
                if (this.counter != null)
                {
                    this.counter.Dispose();
                    this.counter = null;
                }
            }

            private string GetInstanceNameForCurrentProcess()
            {
                Process currentProcess = Process.GetCurrentProcess();

                if (this.Configuration.UseDotNetInstanceNameConvention)
                {
                    string instanceName = VersioningHelper.MakeVersionSafeName(currentProcess.ProcessName, ResourceScope.Machine, ResourceScope.AppDomain);
                    // We actually don't need the AppDomain portion, but there is no way to get the runtime ID without AppDomain id attached.
                    // So we just filter it out 
                    int adIdIndex = instanceName.LastIndexOf("_ad");
                    if (adIdIndex > 0)
                    {
                        instanceName = instanceName.Substring(0, adIdIndex);
                    }
                    return instanceName;
                }
                else
                {
                    string processIdCounterName = this.Configuration.ProcessIdCounterName;
                    Debug.Assert(!string.IsNullOrWhiteSpace(processIdCounterName));
                    string processIdCounterCategory = this.Configuration.ProcessIdCounterCategory;
                    if (string.IsNullOrWhiteSpace(processIdCounterCategory))
                    {
                        processIdCounterCategory = this.Configuration.CounterCategory;
                    }

                    PerformanceCounterCategory cat = new PerformanceCounterCategory(processIdCounterCategory);

                    string[] instanceNames = cat.GetInstanceNames()
                        .Where(inst => inst.StartsWith(currentProcess.ProcessName))
                        .ToArray();

                    foreach (string instanceName in instanceNames)
                    {
                        using (PerformanceCounter cnt = new PerformanceCounter(processIdCounterCategory, processIdCounterName, instanceName, readOnly: true))
                        {
                            int val = (int)cnt.RawValue;
                            if (val == currentProcess.Id)
                            {
                                return instanceName;
                            }
                        }
                    }
                }

                return null;
            }
        }
    }
}
