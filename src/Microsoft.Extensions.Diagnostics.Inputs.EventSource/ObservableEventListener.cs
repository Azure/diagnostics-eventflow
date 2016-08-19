// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Configuration;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class ObservableEventListener : EventListener, IObservable<EventData>, IDisposable
    {
        private bool constructed;   // Initial value will be false (.NET default)
        private IHealthReporter healthReporter;
        private List<EventSource> eventSourcesPresentAtConstruction;        
        private SimpleSubject<EventData> subject;

        public ObservableEventListener(List<EventSourceConfiguration> eventSources, IHealthReporter healthReporter)
        {
            Requires.NotNull(eventSources, nameof(eventSources));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.subject = new SimpleSubject<EventData>();

            this.EventSources = eventSources;
            if (this.EventSources.Count == 0)
            {
                healthReporter.ReportWarning($"{nameof(ObservableEventListener)}: no event sources configured");
                return;
            }

            lock (this)  // See OnEventSourceCreated() for explanation why we are locking on 'this' here.
            {
                EnableInitialSources();
                this.constructed = true;
            }
        }

        public IReadOnlyCollection<EventSourceConfiguration> EventSources { get; private set; }

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
            this.subject.OnNext(eventArgs.ToEventData());
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
            EventSourceConfiguration provider = this.EventSources.FirstOrDefault(p => p.ProviderName == eventSource.Name);
            if (provider != null)
            {
                this.EnableEvents(eventSource, provider.Level, provider.Keywords);
            }
        }
    }
}
