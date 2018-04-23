// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Azure.EventHubs;
using Moq;
using Xunit;
using MessagingEventData = Microsoft.Azure.EventHubs.EventData;

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
            throw new NotImplementedException();
        }
    }

    
}
