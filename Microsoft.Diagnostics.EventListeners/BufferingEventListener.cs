// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.EventListeners
{
    public abstract class BufferingEventListener : EventListener
    {
        private IHealthReporter healthReporter;
        private TimeSpanThrottle errorReportingThrottle;
        private List<EtwProviderConfiguration> providers;

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

            this.Disabled = !configurationProvider.HasConfiguration;
            if (!this.Disabled)
            {
                this.providers = configurationProvider.GetValue<List<EtwProviderConfiguration>>("EtwProviders");
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
            if (!this.Disabled)
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

        protected void ReportListenerHealthy()
        {
            this.errorReportingThrottle.Execute(() => this.healthReporter.ReportHealthy());
        }

        protected void ReportListenerProblem(string problemDescription)
        {
            this.errorReportingThrottle.Execute(() => this.healthReporter.ReportProblem(problemDescription));
        }
    }
}