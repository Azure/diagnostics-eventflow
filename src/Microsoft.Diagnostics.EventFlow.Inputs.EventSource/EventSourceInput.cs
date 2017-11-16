// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Utilities.Etw;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class EventSourceInput : EventListener, IObservable<EventData>, IDisposable, IRequireActivation
    {
        private bool constructed;   // Initial value will be false (.NET default)
        private IHealthReporter healthReporter;
        // This does not really need to be a ConcurrentQueue, but the ConcurrentQueue has a very convenient-to-use TryDequeue method.
        private ConcurrentQueue<EventSource> eventSourcesPresentAtConstruction;
        private EventFlowSubject<EventData> subject;
        private Task initialization;
        private ConcurrentDictionary<string, bool> disabledSources;
        private Action<EventWrittenEventArgs> OnEventWrittenImpl;

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

            var myEventSources = new List<EventSourceConfiguration>(eventSources);
            Initialize(myEventSources, healthReporter);
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

        public void Activate()
        {
            Assumes.NotNull(this.initialization);
            this.initialization.Wait();
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventArgs)
        {
            // Suppress events from TplEventSource--they are mostly interesting for debugging task processing and interaction,
            // and not that useful for production tracing. However, TPL EventSource must be enabled to get hierarchical activity IDs.
            if (!TplActivities.TplEventSourceGuid.Equals(eventArgs.EventSource.Guid))
            {
                this.OnEventWrittenImpl(eventArgs);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // There is a bug in the EventListener library that causes this override to be called before the object is fully constructed.
            // So if we are not constructed yet, we will just remember the event source reference. Once the construction is accomplished,
            // we can decide if we want to handle a given event source or not.

            bool enableImmediately = false;

            // Locking on 'this' is generally a bad practice because someone from outside could put a lock on us, and this is outside of our control.
            // But in the case of this class it is an unlikely scenario, and because of the bug described above, 
            // we cannot rely on construction to prepare a private lock object for us.
            lock (this)
            {
                if (!this.constructed)
                {
                    if (this.eventSourcesPresentAtConstruction == null)
                    {
                        this.eventSourcesPresentAtConstruction = new ConcurrentQueue<EventSource>();
                    }

                    this.eventSourcesPresentAtConstruction.Enqueue(eventSource);
                }
                else
                {
                    enableImmediately = true;
                }
            }

            // Do not try to enable an EventSource source while holding 'this' lock. Enabling an EventSource tries to take a lock on EventListener list.
            // (part of EventSource implementation). If another EventSource is created on a different thread, 
            // the same lock will be taken before the call to OnEventSourceCreated() comes in and deadlock will result.
            if (enableImmediately)
            {
                EnableAsNecessary(eventSource);
            }
        }

        private void EnableInitialSources()
        {
            ConcurrentQueue<EventSource> sourcesToExamine = null;
            Assumes.True(this.constructed);

            // Taking a lock to ensure that the queue of sources existing at construction is fully created and populated (if necessary).
            // See OnEventSourceCreated() for explanation why we are locking on 'this'.
            lock (this)
            {
                sourcesToExamine = this.eventSourcesPresentAtConstruction;
            }

            if (sourcesToExamine != null)
            {
                EventSource eventSource;
                while (sourcesToExamine.TryDequeue(out eventSource))
                {
                    EnableAsNecessary(eventSource);
                }
            }
        }

        private void EnableAsNecessary(EventSource eventSource)
        {
            // Special case: enable TPL activity flow for better tracing of nested activities.
            if (eventSource.Guid == TplActivities.TplEventSourceGuid)
            {
                this.EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)TplActivities.TaskFlowActivityIdsKeyword);
            }
            else
            {
                foreach (EventSourceConfiguration sourceConfiguration in this.EventSources)
                {
                    if (sourceConfiguration.Enables(eventSource))
                    {
                        // LIMITATION: There is a known issue where if we listen to the FrameworkEventSource, the dataflow pipeline may hang when it
                        // tries to process the Threadpool event. The reason is the dataflow pipeline itself is using Task library for scheduling async
                        // tasks, which then itself also fires Threadpool events on FrameworkEventSource at unexpected locations, and trigger deadlocks.
                        // Hence, we like special case this and mask out Threadpool events.
                        EventKeywords keywords = sourceConfiguration.Keywords;
                        if (sourceConfiguration.ProviderName == "System.Diagnostics.Eventing.FrameworkEventSource")
                        {
                            // Turn off the Threadpool | ThreadTransfer keyword. Definition is at http://referencesource.microsoft.com/#mscorlib/system/diagnostics/eventing/frameworkeventsource.cs
                            // However, if keywords was not set, then we need to set it to All first, which is 0xFFFFF....
                            if (keywords == 0)
                            {
                                keywords = EventKeywords.All;
                            }
                            keywords &= (EventKeywords)~0x12;
                        }

                        this.EnableEvents(eventSource, sourceConfiguration.Level, keywords);
                    }
                }
            }
        }

        private void Initialize(List<EventSourceConfiguration> eventSources, IHealthReporter healthReporter)
        {
            this.healthReporter = healthReporter;
            this.subject = new EventFlowSubject<EventData>();

            if (eventSources.Count() == 0)
            {
                healthReporter.ReportWarning($"{nameof(EventSourceInput)}: no event sources configured", EventFlowContextIdentifiers.Configuration);
            }

            var invalidConfigurationItems = new List<EventSourceConfiguration>();
            foreach (var eventSourceConfiguration in eventSources)
            {
                if (!eventSourceConfiguration.Validate())
                {
                    healthReporter.ReportProblem($"{nameof(EventSourceInput)}: configuration for one of the sources is invalid", EventFlowContextIdentifiers.Configuration);
                    invalidConfigurationItems.Add(eventSourceConfiguration);
                }
            }
            // eventSources is a collection created by us, so we can modify it as necessary
            eventSources.RemoveAll(config => invalidConfigurationItems.Contains(config));
            this.EventSources = eventSources;

            bool haveDisabledSources = this.EventSources.Any(config => !string.IsNullOrWhiteSpace(config.DisabledProviderNamePrefix));
            if (haveDisabledSources)
            {
                this.disabledSources = new ConcurrentDictionary<string, bool>();
                this.OnEventWrittenImpl = BroadcastEventIfSourceNotDisabled;
            }
            else
            {
                this.OnEventWrittenImpl = BroadcastEvent;
            }

            // Make sure the constructor has run to completion before enabling any sources.
            this.initialization = Task.Run(() =>
            {
                this.constructed = true;
                EnableInitialSources();
            });
        }

        private void BroadcastEventIfSourceNotDisabled(EventWrittenEventArgs eventArgs)
        {
            string eventSourceName = eventArgs.EventSource.Name;
            if (eventSourceName != null)
            {
                bool isDisabled;
                if (!this.disabledSources.TryGetValue(eventSourceName, out isDisabled))
                {
                    isDisabled = this.EventSources.Any(eventSourceConfiguration => eventSourceConfiguration.Disables(eventArgs.EventSource));
                    this.disabledSources[eventSourceName] = isDisabled;
                }

                if (isDisabled) { return; }
            }

            BroadcastEvent(eventArgs);
        }

        private void BroadcastEvent(EventWrittenEventArgs eventArgs)
        {
            this.subject.OnNext(eventArgs.ToEventData(this.healthReporter, nameof(EventSourceInput)));
        }
    }
}
