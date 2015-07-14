using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.SharedLib
{
    internal class EventData
    {
        public DateTimeOffset Timestamp { get; set; }

        public string ProviderName { get; set; }

        public int Id { get; set; }

        public string Message { get; set; }

        public string Level { get; set; }

        public string Keywords { get; set; }

        public string EventName { get; set; }

        public IDictionary<string, object> Payload { get; set; }
    }

    internal static class EventDataExtensions
    {
        private static string HexadecimalNumberPrefix = "0x";

        // Micro-optimization: Enum.ToString() uses type information and does a binary search for the value,
        // which is kind of slow. We are going to to the conversion manually instead.
        private static readonly string[] EventLevelNames = new string[] {
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
                Timestamp = DateTimeOffset.UtcNow,
                Id = eventSourceEvent.EventId,
                Level = EventLevelNames[(int)eventSourceEvent.Level],
                Keywords = HexadecimalNumberPrefix + ((ulong)eventSourceEvent.Keywords).ToString("X16", CultureInfo.InvariantCulture),
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
            catch { }

            eventData.Payload = eventSourceEvent.GetPayloadData();

            return eventData;
        }

        private static IDictionary<string, object> GetPayloadData(this EventWrittenEventArgs eventSourceEvent)
        {
            var payloadData = new Dictionary<string, object>();

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
