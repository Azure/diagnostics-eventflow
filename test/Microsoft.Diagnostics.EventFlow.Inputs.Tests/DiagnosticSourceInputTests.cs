// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Inputs.DiagnosticSource;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class DiagnosticSourceInputTests
    {
        private static readonly System.Diagnostics.DiagnosticSource TestLog = new DiagnosticListener("test");

        [Fact]
        public void ActivitiesAreRecorded()
        {
            var healthReporter = Mock.Of<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var input = new DiagnosticSourceInput(new[] { new DiagnosticSourceConfiguration { ProviderName = "test" } }, healthReporter))
            {
                input.Subscribe(observer.Object);

                var activityArgs = new { one = "two" };
                var activity = new Activity("activity-name");
                activity.AddBaggage("baggage-name", "baggage-value");
                activity.AddTag("tag-name", "tag-value");
                TestLog.StartActivity(activity, activityArgs);
                TestLog.StopActivity(activity, activityArgs);

                observer.Verify(o => o.OnNext(It.Is<EventData>(data =>
                    data.Payload["EventName"].Equals("activity-name.Start")
                 && data.Payload["Value"].Equals(activityArgs)
                 && data.Payload["baggage-name"].Equals("baggage-value")
                 && data.Payload["tag-name"].Equals("tag-value")
                )), Times.Once);
                observer.Verify(o => o.OnNext(It.Is<EventData>(data =>
                    data.Payload["EventName"].Equals("activity-name.Stop")
                 && data.Payload["Value"].Equals(activityArgs)
                 && data.Payload["baggage-name"].Equals("baggage-value")
                 && data.Payload["tag-name"].Equals("tag-value")
                 && (TimeSpan)data.Payload["Duration"] != TimeSpan.Zero
                )), Times.Once);
            }
        }

        [Fact]
        public void ActivityIdsAreRecordedOnChildEvents()
        {
            var healthReporter = Mock.Of<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var input = new DiagnosticSourceInput(new[] { new DiagnosticSourceConfiguration { ProviderName = "test" } }, healthReporter))
            {
                input.Subscribe(observer.Object);

                var parentActivity = new Activity("parent-activity");
                var childActivity = new Activity("child-activity");
                const string eventName = "event-name";

                TestLog.StartActivity(parentActivity, null);
                TestLog.StartActivity(childActivity, null);
                TestLog.Write(eventName, null);
                TestLog.StopActivity(childActivity, null);
                TestLog.StopActivity(parentActivity, null);

                observer.Verify(o => o.OnNext(It.Is<EventData>(data =>
                    data.Payload["EventName"].Equals(eventName)
                 && data.Payload["ActivityId"].Equals(childActivity.Id)
                 && data.Payload["ActivityParentId"].Equals(parentActivity.Id)
                )), Times.Once);
            }
        }

        [Fact]
        public void BagsTakePrecedenceOverTags()
        {
            var healthReporter = Mock.Of<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var input = new DiagnosticSourceInput(new[] { new DiagnosticSourceConfiguration { ProviderName = "test" } }, healthReporter))
            {
                input.Subscribe(observer.Object);

                var activity = new Activity("activity-name");
                activity.AddBaggage("key", "bag-value");
                activity.AddTag("key", "tag-value");
                TestLog.StartActivity(activity, null);

                observer.Verify(o => o.OnNext(It.Is<EventData>(data =>
                    data.Payload["key"].Equals("bag-value")
                 && data.Payload["key_1"].Equals("tag-value")
                )), Times.Once);
            }
        }

        [Fact]
        public void DiagnosticSourceCanBeCreatedAfterInput()
        {
            var providerName = Guid.NewGuid().ToString();
            var healthReporter = Mock.Of<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var input = new DiagnosticSourceInput(new[] { new DiagnosticSourceConfiguration { ProviderName = providerName } }, healthReporter))
            {
                input.Subscribe(observer.Object);
                new DiagnosticListener(providerName).Write("event-name", null);
                observer.Verify(o => o.OnNext(It.Is<EventData>(data => data.ProviderName.Equals(providerName))), Times.Once);
            }
        }

        [Fact]
        public void EventsAreRecorded()
        {
            var healthReporter = Mock.Of<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var input = new DiagnosticSourceInput(new[] { new DiagnosticSourceConfiguration { ProviderName = "test" } }, healthReporter))
            {
                input.Subscribe(observer.Object);

                const string eventName = "event-name";
                var value = new { text = "Test" };
                TestLog.Write(eventName, value);

                observer.Verify(o => o.OnNext(It.Is<EventData>(data =>
                    data.ProviderName.Equals("test")
                 && data.Timestamp != default(DateTimeOffset)
                 && data.Payload["EventName"].Equals(eventName)
                 && data.Payload["Value"].Equals(value)
                )), Times.Once);
            }
        }

        [Fact]
        public void ProvidersAreFiltered()
        {
            var healthReporter = Mock.Of<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var input = new DiagnosticSourceInput(new[] { new DiagnosticSourceConfiguration { ProviderName = "not-test" } }, healthReporter))
            {
                input.Subscribe(observer.Object);
                TestLog.Write("event-name", null);
            }

            observer.Verify(o => o.OnNext(It.IsAny<EventData>()), Times.Never);
        }

        [Fact]
        public void SubscriptionsAreDisposed()
        {
            var healthReporter = Mock.Of<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var input = new DiagnosticSourceInput(new[] { new DiagnosticSourceConfiguration { ProviderName = "test" } }, healthReporter))
            {
                input.Subscribe(observer.Object);
            }

            TestLog.Write("event-name", null);

            observer.Verify(o => o.OnNext(It.IsAny<EventData>()), Times.Never);
        }
    }
}