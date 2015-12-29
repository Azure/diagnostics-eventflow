// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventListeners
{
    using System;
    using Microsoft.Diagnostics.Tracing;

    public abstract class BufferingEventListener : EventListener
    {
        private IHealthReporter healthReporter;
        private TimeSpanThrottle errorReportingThrottle;

        public BufferingEventListener(IConfigurationProvider configurationProvider, IHealthReporter healthReporter)
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
        }

        public bool ApproachingBufferCapacity
        {
            get { return this.Sender.ApproachingBufferCapacity; }
        }

        public bool Disabled { get; }

        protected ConcurrentEventSender<EventData> Sender { get; set; }

        public override void Dispose()
        {
            base.Dispose();
            this.Sender.Dispose();
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventArgs)
        {
            this.Sender.SubmitEvent(eventArgs.ToEventData());
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (!this.Disabled)
            {
                this.EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords) ~0);
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