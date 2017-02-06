// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
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
        public void ActivityPathDecoderDecodesHierarchicalActivityId()
        {
            Guid activityId = new Guid("000000110000000000000000be999d59");
            string activityPath = ActivityPathDecoder.GetActivityPathString(activityId);
            Assert.Equal("//1/1/", activityPath);
        }

        [Fact]
        public void ActivityPathDecoderHandlesNonhierarchicalActivityIds()
        {
            string guidString = "bf0209f9-bf5e-415e-86ed-0e20b615b406";
            Guid activityId = new Guid(guidString);
            string activityPath = ActivityPathDecoder.GetActivityPathString(activityId);
            Assert.Equal(guidString, activityPath);
        }

        [Fact]
        public void ActivityPathDecoderHandlesEmptyActivityId()
        {
            string activityPath = ActivityPathDecoder.GetActivityPathString(Guid.Empty);
            Assert.Equal(Guid.Empty.ToString(), activityPath);
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
        }
    }

    #endif
}
