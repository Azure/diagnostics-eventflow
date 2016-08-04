// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class PerformanceCounterListener: IObservable<EventData>, IDisposable
    {
        private const int SampleIntervalSeconds = 10;

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
                        healthReporter.ReportProblem($"{nameof(PerformanceCounterListener)}: configuration for counter {counterConfiguration.CoutnerName} is invalid");
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
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            throw new NotImplementedException();
        }

        private class TrackedPerformanceCounter
        {
            private PerformanceCounter counter;

            public PerformanceCounterConfiguration Configuration { get; private set; }
            public DateTimeOffset LastAccessedOn { get; set; }

            public TrackedPerformanceCounter(PerformanceCounterConfiguration configuration)
            {
                Requires.NotNull(configuration, nameof(configuration));
                this.Configuration = configuration;
                this.LastAccessedOn = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
                this.counter = new PerformanceCounter(configuration.CounterCategory, configuration.CoutnerName, instanceName: string.Empty, readOnly: true);
            }

            public bool SampleNextValue(out float newValue)
            {
                var now = DateTimeOffset.UtcNow;
                if (now - this.LastAccessedOn > TimeSpan.FromSeconds(this.Configuration.CollectionIntervalInSeconds))
                {
                    this.LastAccessedOn = now;
                    newValue = this.counter.NextValue();
                    return true;
                }
                else
                {
                    newValue = 0;
                    return false;
                }
            }
        }
    }
}
