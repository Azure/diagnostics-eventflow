// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Configuration;
using Microsoft.Extensions.Diagnostics.Metadata;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class PerformanceCounterListener: IObservable<EventData>, IDisposable
    {
        private const int SampleIntervalSeconds = 10;
        internal static readonly string MetricValueProperty = "Value";

        private IHealthReporter healthReporter;
        private SimpleSubject<EventData> subject;
        private object syncObject;
        private Timer collectionTimer;
        private List<TrackedPerformanceCounter> trackedPerformanceCounters;
        
        public PerformanceCounterListener(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.syncObject = new object();
            this.healthReporter = healthReporter;
            this.subject = new SimpleSubject<EventData>();

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
                foreach(TrackedPerformanceCounter counter in this.trackedPerformanceCounters)
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

                this.counter = new PerformanceCounter(configuration.CounterCategory, configuration.CounterName, instanceName: string.Empty, readOnly: true);
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

                this.lastAccessedOn = now;
                newValue = this.counter.NextValue();
                return true;
            }

            public void Dispose()
            {
                this.counter.Dispose();
            }

            private string GetInstanceNameForCurrentProcess()
            {
                Process currentProcess = Process.GetCurrentProcess();
                string processName = Path.GetFileNameWithoutExtension(currentProcess.ProcessName);

                PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");
                string[] instances = cat.GetInstanceNames()
                    .Where(inst => inst.StartsWith(processName))
                    .ToArray();

                foreach (string instance in instances)
                {
                    using (PerformanceCounter cnt = new PerformanceCounter("Process", "ID Process", instance, true))
                    {
                        int val = (int)cnt.RawValue;
                        if (val == currentProcess.Id)
                        {
                            return instance;
                        }
                    }
                }
                return null;
            }
        }
    }
}
