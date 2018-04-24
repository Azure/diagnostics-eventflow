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
            configuration.PartitionKeyProperty = "PartitionByProperty";

            EventData e = new EventData();
            e.ProviderName = "TestProvider";
            e.Timestamp = DateTimeOffset.UtcNow;
            e.Level = LogLevel.Warning;
            e.Payload.Add("IntProperty", 42);
            e.Payload.Add("StringProperty", "perfection");

            EventData ep = new EventData();
            ep.ProviderName = "TestProvider";
            ep.Timestamp = DateTimeOffset.UtcNow;
            ep.Level = LogLevel.Warning;
            ep.Payload.Add("IntProperty", 23);
            ep.Payload.Add("StringProperty", "partition-perfection");
            ep.Payload.Add("PartitionByProperty", "partition1");

            EventHubOutput eho = new EventHubOutput(configuration, healthReporter.Object, connectionString => client.Object);
            await eho.SendEventsAsync(new EventData[] {e, ep}, 17, CancellationToken.None);

            Func<IEnumerable<MessagingEventData>, bool> verifyBatch = batch =>
            {
                if (batch.Count() != 2) return false;

                var data = batch.First();
                var bodyString = Encoding.UTF8.GetString(data.Body.Array, data.Body.Offset, data.Body.Count);
                var recordSet = JObject.Parse(bodyString);
                var message = recordSet["records"][0];

                var dataP = batch.Last();
                var bodyStringP = Encoding.UTF8.GetString(dataP.Body.Array, dataP.Body.Offset, dataP.Body.Count);
                var recordSetP = JObject.Parse(bodyStringP);
                var messageP = recordSetP["records"][0];


                return (string) message["level"] == "Warning"
                       && (string) message["properties"]["ProviderName"] == "TestProvider"
                       && (int) message["properties"]["IntProperty"] == 42
                       && (string) message["properties"]["StringProperty"] == "perfection"

                       && (string) messageP["level"] == "Warning"
                       && (string) messageP["properties"]["ProviderName"] == "TestProvider"
                       && (int) messageP["properties"]["IntProperty"] == 23
                       && (string) messageP["properties"]["StringProperty"] == "partition-perfection"
                       && (string) messageP["properties"]["PartitionByProperty"] == "partition1";
            };

            client.Verify(c => c.SendAsync(It.Is<IEnumerable<MessagingEventData>>(b => verifyBatch(b))), Times.Once);
            client.Verify(c => c.SendAsync(It.Is<IEnumerable<MessagingEventData>>(b => verifyBatch(b)), "partition1"), Times.Once);
            healthReporter.Verify(hr => hr.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            healthReporter.Verify(hr => hr.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
