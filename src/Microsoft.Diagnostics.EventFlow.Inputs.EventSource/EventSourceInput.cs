// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class EventSourceInput : EventListener, IObservable<EventData>, IDisposable
    {
        private static readonly Guid TplEventSourceGuid = new Guid("2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5");
        private static readonly long TaskFlowActivityIds = 0x80;

        private bool constructed;   // Initial value will be false (.NET default)
        private IHealthReporter healthReporter;
        private List<EventSource> eventSourcesPresentAtConstruction;        
        private EventFlowSubject<EventData> subject;

        public EventSourceInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            IConfiguration sourcesConfiguration = configuration.GetSection("sources");
            if (sourcesConfiguration == null)
            {
                healthReporter.ReportProblem($"{nameof(EventSourceInput)}: required configuration section 'sources' is missing");
                return;
            }
            var eventSources = new List<EventSourceConfiguration>();
            try
            {
                sourcesConfiguration.Bind(eventSources);
            }
            catch
            {
                healthReporter.ReportProblem($"{nameof(EventSourceInput)}: configuration is invalid", EventFlowContextIdentifiers.Configuration);
                return;
            }

            Initialize(eventSources, healthReporter);
        }

        public EventSourceInput(IReadOnlyCollection<EventSourceConfiguration> eventSources, IHealthReporter healthReporter)
        {
            Requires.NotNull(eventSources, nameof(eventSources));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            Initialize(eventSources, healthReporter);
        }

        public IEnumerable<EventSourceConfiguration> EventSources { get; private set; }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }

        public override void Dispose()
        {
            base.Dispose();
            this.subject.Dispose();
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventArgs)
        {
            this.subject.OnNext(eventArgs.ToEventData(this.healthReporter, nameof(EventSourceInput)));
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // There is a bug in the EventListener library that causes this override to be called before the object is fully constructed.
            // So if we are not constructed yet, we will just remember the event source reference. Once the construction is accomplished,
            // we can decide if we want to handle a given event source or not.

            // Locking on 'this' is generally a bad practice because someone from outside could put a lock on us, and this is outside of our control.
            // But in the case of this class it is an unlikely scenario, and because of the bug described above, 
            // we cannot rely on construction to prepare a private lock object for us.
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
                else
                {
                    EnableAsNecessary(eventSource);
                }
            }
        }

        private void EnableInitialSources()
        {
            Assumes.False(this.constructed);
            if (this.eventSourcesPresentAtConstruction != null)
            {
                foreach (EventSource eventSource in this.eventSourcesPresentAtConstruction)
                {
                    EnableAsNecessary(eventSource);
                }
                this.eventSourcesPresentAtConstruction.Clear(); // Do not hold onto EventSource references that are already initialized.
            }
        }

        private void EnableAsNecessary(EventSource eventSource)
        {
            // Special case: enable TPL activity flow for better tracing of nested activities.
            if (eventSource.Guid == TplEventSourceGuid)
            {
                this.EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)TaskFlowActivityIds);
            }
            else
            {
                EventSourceConfiguration provider = this.EventSources.FirstOrDefault(p => p.ProviderName == eventSource.Name);
                if (provider != null)
                {
                    // LIMITATION: There is a known issue where if we listen to the FrameworkEventSource, the dataflow pipeline may hang when it
                    // tries to process the Threadpool event. The reason is the dataflow pipeline itself is using Task library for scheduling async
                    // tasks, which then itself also fires Threadpool events on FrameworkEventSource at unexpected locations, and trigger deadlocks.
                    // Hence, we like special case this and mask out Threadpool events.
                    EventKeywords keywords = provider.Keywords;
                    if (provider.ProviderName == "System.Diagnostics.Eventing.FrameworkEventSource")
                    {
                        // Turn off the Threadpool | ThreadTransfer keyword. Definition is at http://referencesource.microsoft.com/#mscorlib/system/diagnostics/eventing/frameworkeventsource.cs
                        // However, if keywords was to begin with, then we need to set it to All first, which is 0xFFFFF....
                        if (keywords == 0)
                        {
                            keywords = EventKeywords.All;
                        }
                        keywords &= (EventKeywords)~0x12;
                    }
                    this.EnableEvents(eventSource, provider.Level, keywords);
                }
            }
        }

        private void Initialize(IReadOnlyCollection<EventSourceConfiguration> eventSources, IHealthReporter healthReporter)
        {
            this.healthReporter = healthReporter;
            this.subject = new EventFlowSubject<EventData>();

            this.EventSources = eventSources;
            if (this.EventSources.Count() == 0)
            {
                healthReporter.ReportWarning($"{nameof(EventSourceInput)}: no event sources configured", nameof(EventSourceInput));
                return;
            }

            lock (this)  // See OnEventSourceCreated() for explanation why we are locking on 'this' here.
            {
                EnableInitialSources();
                this.constructed = true;
            }
        }
    }
}
