// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using MessagingEventData = Microsoft.ServiceBus.Messaging.EventData;

namespace Microsoft.Diagnostics.EventListeners
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.Diagnostics.Tracing;
    using Newtonsoft.Json;

    internal static class EventDataExtensions
    {
        private static string HexadecimalNumberPrefix = "0x";
        // Micro-optimization: Enum.ToString() uses type information and does a binary search for the value,
        // which is kind of slow. We are going to to the conversion manually instead.
        private static readonly string[] EventLevelNames = new string[]
        {
            "Always",
            "Critical",
            "Error",
            "Warning",
            "Informational",
            "Verbose"
        };

        public static EventData ToEventData(this EventWrittenEventArgs eventSourceEvent)
        {
            EventData eventData = new EventData
            {
                ProviderName = eventSourceEvent.EventSource.GetType().FullName,
                Timestamp = DateTime.UtcNow,
                EventId = eventSourceEvent.EventId,
                Level = EventLevelNames[(int) eventSourceEvent.Level],
                Keywords = HexadecimalNumberPrefix + ((ulong) eventSourceEvent.Keywords).ToString("X16", CultureInfo.InvariantCulture),
                EventName = eventSourceEvent.EventName,
            };

            try
            {
                if (eventSourceEvent.Message != null)
                {
                    // If the event has a badly formatted manifest, the FormattedMessage property getter might throw
                    eventData.Message = string.Format(CultureInfo.InvariantCulture, eventSourceEvent.Message, eventSourceEvent.Payload.ToArray());
                }
            }
            catch
            {
            }

            eventData.Payload = eventSourceEvent.GetPayloadData();

            return eventData;
        }

        public static MessagingEventData ToMessagingEventData(this EventData eventData)
        {
            string eventDataSerialized = JsonConvert.SerializeObject(eventData);
            MessagingEventData messagingEventData = new MessagingEventData(Encoding.UTF8.GetBytes(eventDataSerialized));
            return messagingEventData;
        }

        private static IDictionary<string, object> GetPayloadData(this EventWrittenEventArgs eventSourceEvent)
        {
            Dictionary<string, object> payloadData = new Dictionary<string, object>();

            if (eventSourceEvent.Payload == null || eventSourceEvent.PayloadNames == null)
            {
                return payloadData;
            }

            IEnumerator<object> payloadEnumerator = eventSourceEvent.Payload.GetEnumerator();
            IEnumerator<string> payloadNamesEnunmerator = eventSourceEvent.PayloadNames.GetEnumerator();
            while (payloadEnumerator.MoveNext())
            {
                payloadNamesEnunmerator.MoveNext();
                payloadData.Add(payloadNamesEnunmerator.Current, payloadEnumerator.Current);
            }

            return payloadData;
        }
    }
}