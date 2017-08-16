// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Diagnostics.EventFlow.Configuration;


namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    #if !NET451

    public class EventSourceInputTests
    {
        [Fact]
        public void HandlesDuplicatePropertyNames()
        {
            var healthReporterMock = new Mock<IHealthReporter>();

            var inputConfiguration = new List<EventSourceConfiguration>();
            inputConfiguration.Add(new EventSourceConfiguration()
            {
                ProviderName = "EventSourceInput-TestEventSource"
            });
            var eventSourceInput = new EventSourceInput(inputConfiguration, healthReporterMock.Object);
            eventSourceInput.Activate();

            var observer = new Mock<IObserver<EventData>>();
            using (eventSourceInput.Subscribe(observer.Object))
            {
                EventSourceInputTestSource.Log.Tricky(7, "TrickyEvent", "Actual message");

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("Manifest message")
                    && data.Payload["EventId"].Equals(1)
                    && data.Payload["EventName"].Equals("Tricky")
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("Message") && key != "Message")].Equals("Actual message")
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("EventId") && key != "EventId")].Equals(7)
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("EventName") && key != "EventName")].Equals("TrickyEvent")
                )), Times.Exactly(1));

                healthReporterMock.Verify(o => o.ReportWarning(
                        It.Is<string>(s => s.Contains("already exist in the event payload")),
                        It.Is<string>(s => s == nameof(EventSourceInput))),
                    Times.Exactly(3));
            }
        }

        [Fact]
        public void CapturesEventsFromEventSourceExistingBeforeInputCreated()
        {
            var healthReporterMock = new Mock<IHealthReporter>();

            var inputConfiguration = new List<EventSourceConfiguration>();
            inputConfiguration.Add(new EventSourceConfiguration()
            {
                ProviderName = "EventSourceInput-TestEventSource"
            });

            // EventSourceInputTestSource has a static instance that exists before the input is created.
            // But it won't be actually hooked up to EventSource/EventListener infrastructure until an event is raised.
            EventSourceInputTestSource.Log.Message("ignored");

            var eventSourceInput = new EventSourceInput(inputConfiguration, healthReporterMock.Object);
            eventSourceInput.Activate();

            var observer = new Mock<IObserver<EventData>>();
            using (eventSourceInput.Subscribe(observer.Object))
            {
                
                EventSourceInputTestSource.Log.Message("Hello!");

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("Hello!")
                    && data.Payload["EventId"].Equals(2)
                    && data.Payload["EventName"].Equals("Message")                    
                )), Times.Exactly(1));

                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        public void CapturesEventsFromEventSourceCreatedAfterInputCreated()
        {
            var healthReporterMock = new Mock<IHealthReporter>();

            var inputConfiguration = new List<EventSourceConfiguration>();
            inputConfiguration.Add(new EventSourceConfiguration()
            {
                ProviderName = "EventSourceInput-OtherTestEventSource"
            });

            var eventSourceInput = new EventSourceInput(inputConfiguration, healthReporterMock.Object);
            eventSourceInput.Activate();

            var observer = new Mock<IObserver<EventData>>();
            using (eventSourceInput.Subscribe(observer.Object))
            using (var eventSource = new EventSourceInputTestOtherSource())
            {
                eventSource.Message("Wow!");

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("Wow!")
                    && data.Payload["EventId"].Equals(3)
                    && data.Payload["EventName"].Equals("Message")
                )), Times.Exactly(1));

                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            }
        }

        [EventSource(Name = "EventSourceInput-TestEventSource")]
        private class EventSourceInputTestSource : EventSource
        {
            public static EventSourceInputTestSource Log = new EventSourceInputTestSource();

            [Event(1, Level = EventLevel.Informational, Message ="Manifest message")]
            public void Tricky(int EventId, string EventName, string Message)
            {
                WriteEvent(1, EventId, EventName, Message);
            }

            [Event(2, Level = EventLevel.Informational, Message ="{0}")]
            public void Message(string message)
            {
                WriteEvent(2, message);
            }
        }

        [EventSource(Name = "EventSourceInput-OtherTestEventSource")]
        private class EventSourceInputTestOtherSource: EventSource
        {
            [Event(3, Level = EventLevel.Informational, Message = "{0}")]
            public void Message(string message)
            {
                WriteEvent(3, message);
            }
        }
    }

    #endif
}
