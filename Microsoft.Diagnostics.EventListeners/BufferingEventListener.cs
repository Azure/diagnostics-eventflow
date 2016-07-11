// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Validation;

namespace Microsoft.Diagnostics.EventListeners
{
    public abstract class BufferingEventListener : EventListener
    {
        private bool constructed;
        private IHealthReporter healthReporter;
        private TimeSpanThrottle errorReportingThrottle;
        private List<EtwProviderConfiguration> providers;
        private List<EventSource> eventSourcesPresentAtConstruction;

        public BufferingEventListener(ICompositeConfigurationProvider configurationProvider, IHealthReporter healthReporter)
        {
            if (configurationProvider == null)
            {
                throw new ArgumentNullException("configurationProvider");
            }

            if (healthReporter == null)
            {
                throw new ArgumentNullException("healthReporter");
            }
            this.healthReporter = healthReporter;
            this.errorReportingThrottle = new TimeSpanThrottle(TimeSpan.FromSeconds(1));

            lock(this)  // See OnEventSourceCreated() for explanation why we are locking on 'this' here.
            {
                this.Disabled = !configurationProvider.HasConfiguration;
                if (!this.Disabled)
                {
                    this.providers = configurationProvider.GetValue<List<EtwProviderConfiguration>>("EtwProviders");
                }
                EnableInitialSources();
                this.constructed = true;
            }            
        }

        public bool? ApproachingBufferCapacity
        {
            get { return this.Sender?.ApproachingBufferCapacity; }
        }

        public bool Disabled { get; }

        protected ConcurrentEventSender<EventData> Sender { get; set; }

        public override void Dispose()
        {
            base.Dispose();
            this.Sender?.Dispose();
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventArgs)
        {
            this.Sender?.SubmitEvent(eventArgs.ToEventData());
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // There is a bug in the EventListener library that causes this override to be called before the object is fully constructed.
            // So if we are not constructed yet, we will just remember the event source reference. Once the construction is accomplished,
            // we can decide right here if we want to handle a given event source or not.

            // Yes, locking on 'this' is a bad practice because someone from outside could put a lock on us, and this is outside of our control.
            // But that scenario is very unlikely and because of the bug above, we cannot rely on construction to prepare a private lock object for us.
            lock (this)
            {
                if (!this.constructed)
                {
                    if (this.eventSourcesPresentAtConstruction == null)
                    {
                        this.eventSourcesPresentAtConstruction = new List<EventSource>();
                    }

                    this.eventSourcesPresentAtConstruction.Add(eventSource);
                }
                else if (!this.Disabled)
                {
                    EnableAsNecessary(eventSource);
                }
            }
        }        

        protected void ReportListenerHealthy()
        {
            this.errorReportingThrottle.Execute(() => this.healthReporter.ReportHealthy());
        }

        protected void ReportListenerProblem(string problemDescription)
        {
            this.errorReportingThrottle.Execute(() => this.healthReporter.ReportProblem(problemDescription));
        }

        private void EnableInitialSources()
        {
            Assumes.False(this.constructed);
            if (this.eventSourcesPresentAtConstruction != null)
            {
                foreach(EventSource eventSource in this.eventSourcesPresentAtConstruction)
                {
                    EnableAsNecessary(eventSource);
                }
                this.eventSourcesPresentAtConstruction.Clear(); // Do not hold onto EventSource references that are no longer needed.
            }
        }

        private void EnableAsNecessary(EventSource eventSource)
        {
            if (this.providers == null)
            {
                this.EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)~0);
            }
            else
            {
                EtwProviderConfiguration provider = this.providers.Where(p => p.ProviderName == eventSource.Name).FirstOrDefault();
                if (provider != null)
                {
                    this.EnableEvents(eventSource, provider.Level, provider.Keywords);
                }
            }
        }
    }
}