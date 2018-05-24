// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using MessagingEventData = Microsoft.Azure.EventHubs.EventData;

using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class EventHubOutputTests
    {
        [Fact]
        public void UsesIsoDateFormat()
        {
            EventData e = new EventData();
            e.Payload.Add("DateTimeProperty", new DateTime(2017, 4, 19, 10, 15, 23, DateTimeKind.Utc));
            e.Payload.Add("DateTimeOffsetProperty", new DateTimeOffset(2017, 4, 19, 10, 16, 07, TimeSpan.Zero));

            var messagingData = EventDataExtensions.ToMessagingEventData(e, out int messageSize);
            string messageBody = Encoding.UTF8.GetString(messagingData.Body.Array, messagingData.Body.Offset, messagingData.Body.Count);

            var dateTimeRegex = new Regex(@"""DateTimeProperty"" \s* : \s* ""2017-04-19T10:15:23Z""", RegexOptions.IgnorePatternWhitespace, TimeSpan.FromMilliseconds(100));
            Assert.Matches(dateTimeRegex, messageBody);

            var dateTimeOffsetRegex = new Regex(@"""DateTimeOffsetProperty"" \s* : \s* ""2017-04-19T10:16:07\+00:00""", RegexOptions.IgnorePatternWhitespace, TimeSpan.FromMilliseconds(100));
            Assert.Matches(dateTimeOffsetRegex, messageBody);
        }

        [Fact]
        public async Task SendsDataToEventHub()
        {
            var client = new Mock<IEventHubClient>();
            var healthReporter = new Mock<IHealthReporter>();
            var configuration = new EventHubOutputConfiguration();
            configuration.ConnectionString = "Connection string";
            configuration.EventHubName = "foo";

            EventData e = new EventData();
            e.ProviderName = "TestProvider";
            e.Timestamp = DateTimeOffset.UtcNow;
            e.Level = LogLevel.Warning;
            e.Payload.Add("IntProperty", 42);
            e.Payload.Add("StringProperty", "perfection");

            EventHubOutput eho = new EventHubOutput(configuration, healthReporter.Object, connectionString => client.Object);
            await eho.SendEventsAsync(new EventData[] {e,}, 17, CancellationToken.None);

            Func<IEnumerable<MessagingEventData>, bool> verifyBatch = batch =>
            {
                if (batch.Count() != 1) return false;

                var data = batch.First();
                var bodyString = Encoding.UTF8.GetString(data.Body.Array, data.Body.Offset, data.Body.Count);
                var recordSet = JObject.Parse(bodyString);
                var message = recordSet["records"][0];

                return (string) message["level"] == "Warning"
                       && (string) message["properties"]["ProviderName"] == "TestProvider"
                       && (int) message["properties"]["IntProperty"] == 42
                       && (string) message["properties"]["StringProperty"] == "perfection";
            };

            client.Verify(c => c.SendAsync(It.Is<IEnumerable<MessagingEventData>>(b => verifyBatch(b))), Times.Once);
            healthReporter.Verify(hr => hr.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            healthReporter.Verify(hr => hr.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendsDataToPartitionedEventHub()
        {
            var client = new Mock<IEventHubClient>();
            var healthReporter = new Mock<IHealthReporter>();
            var configuration = new EventHubOutputConfiguration();
            configuration.ConnectionString = "Connection string";
            configuration.EventHubName = "foo";
            configuration.PartitionKeyProperty = "PartitionByProperty";

            EventData e1 = new EventData();
            e1.ProviderName = "TestProvider";
            e1.Timestamp = DateTimeOffset.UtcNow;
            e1.Level = LogLevel.Warning;
            e1.Payload.Add("IntProperty", 23);
            e1.Payload.Add("StringProperty", "partition-perfection");
            e1.Payload.Add("PartitionByProperty", "partition1");

            EventData e2 = new EventData();
            e2.ProviderName = "TestProvider";
            e2.Timestamp = DateTimeOffset.UtcNow;
            e2.Level = LogLevel.Warning;
            e2.Payload.Add("IntProperty", 23);
            e2.Payload.Add("StringProperty", "partition-perfection");

            EventData e3 = new EventData();
            e3.ProviderName = "TestProvider";
            e3.Timestamp = DateTimeOffset.UtcNow;
            e3.Level = LogLevel.Warning;
            e3.Payload.Add("IntProperty", 23);
            e3.Payload.Add("StringProperty", new string('a', 150000));
            e3.Payload.Add("PartitionByProperty", "partition2");

            EventData e4 = new EventData();
            e4.ProviderName = "TestProvider";
            e4.Timestamp = DateTimeOffset.UtcNow;
            e4.Level = LogLevel.Warning;
            e4.Payload.Add("IntProperty", 23);
            e4.Payload.Add("StringProperty", new string('b', 150000));
            e4.Payload.Add("PartitionByProperty", "partition2");

            EventData e5 = new EventData();
            e5.ProviderName = "TestProvider";
            e5.Timestamp = DateTimeOffset.UtcNow;
            e5.Level = LogLevel.Warning;
            e5.Payload.Add("IntProperty", 23);
            e5.Payload.Add("StringProperty", new string('c', 150000));
            e5.Payload.Add("PartitionByProperty", "partition2");

            EventHubOutput eho = new EventHubOutput(configuration, healthReporter.Object, connectionString => client.Object);
            await eho.SendEventsAsync(new[] {e1, e2, e3, e4, e5}, 17, CancellationToken.None);

            Func<IEnumerable<MessagingEventData>, bool> verifyBatch = batch =>
            {
                if (batch.Count() != 1) return false;
                return true;
            };

            client.Verify(c => c.SendAsync(It.Is<IEnumerable<MessagingEventData>>(b => verifyBatch(b))), Times.Once);
            client.Verify(c => c.SendAsync(It.Is<IEnumerable<MessagingEventData>>(b => verifyBatch(b)), "partition1"), Times.Once);
            client.Verify(c => c.SendAsync(It.Is<IEnumerable<MessagingEventData>>(b => verifyBatch(b)), "partition2"), Times.Exactly(3));
            healthReporter.Verify(hr => hr.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            healthReporter.Verify(hr => hr.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public Task Sends512ItemsToPartitionedEventHub()
        {
            return SendsManyItemsToPartitionedEventHub(512);
        }

        private async Task SendsManyItemsToPartitionedEventHub(int itemCount)
        {
            var client = new Mock<IEventHubClient>();
            var healthReporter = new Mock<IHealthReporter>();
            var configuration = new EventHubOutputConfiguration();
            configuration.ConnectionString = "Connection string";
            configuration.EventHubName = "foo";
            configuration.PartitionKeyProperty = "PartitionByProperty";

            EventHubOutput eho = new EventHubOutput(configuration, healthReporter.Object, connectionString => client.Object);

            //send `itemCount` EventData items with payload of 55Kb+ each and targeting PartitionKey "partition3"
            string payload = new string('a', 55000);
            await eho.SendEventsAsync(Enumerable.Range(0, itemCount)
                .Select(r =>
                {
                    var eventData = new EventData();
                    eventData.ProviderName = "TestProvider";
                    eventData.Timestamp = DateTimeOffset.UtcNow;
                    eventData.Level = LogLevel.Warning;
                    eventData.Payload.Add("IntProperty", r);
                    eventData.Payload.Add("StringProperty", payload);
                    eventData.Payload.Add("PartitionByProperty", "partition3");
                    return eventData;
                })
                .ToList(), 17, CancellationToken.None);

            Func<IEnumerable<MessagingEventData>, bool> verifyBatch = batch =>
            {
                if (batch.Count() != 4)
                {
                    return false;
                }
                return true;
            };

            //the individual messages are 55Kb in size each, so a maximum of 4 messages total byte size fits within EventHubMessageSizeLimit limits.
            //so we expect `itemCount` / 4 batches executed against EventHubClient.SendAsync()
            client.Verify(c => c.SendAsync(It.Is<IEnumerable<MessagingEventData>>(b => verifyBatch(b)), "partition3"), Times.Exactly(itemCount / 4));
            healthReporter.Verify(hr => hr.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            healthReporter.Verify(hr => hr.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
