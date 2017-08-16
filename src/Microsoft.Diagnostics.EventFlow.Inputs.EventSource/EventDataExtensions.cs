// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Diagnostics.Tracing;
using System.Diagnostics;

using Microsoft.Diagnostics.EventFlow.Utilities.Etw;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal static class EventDataExtensions
    {
        public static EventData ToEventData(this EventWrittenEventArgs eventSourceEvent, IHealthReporter healthReporter, string context)
        {
            Debug.Assert(healthReporter != null);

            EventData eventData = new EventData
            {
                ProviderName = eventSourceEvent.EventSource.Name,
                Timestamp = DateTime.UtcNow,
                Level = (LogLevel)(int)eventSourceEvent.Level,
                Keywords = (long)eventSourceEvent.Keywords
            };

            IDictionary<string, object> payloadData = eventData.Payload;
            payloadData.Add(nameof(eventSourceEvent.EventId), eventSourceEvent.EventId);
            payloadData.Add(nameof(eventSourceEvent.EventName), eventSourceEvent.EventName);
            if (eventSourceEvent.ActivityId != default(Guid))
            {
                payloadData.Add(nameof(EventWrittenEventArgs.ActivityId), ActivityPathDecoder.GetActivityPathString(eventSourceEvent.ActivityId));
            }
            if (eventSourceEvent.RelatedActivityId != default(Guid))
            {
                payloadData.Add(nameof(EventWrittenEventArgs.RelatedActivityId), ActivityPathDecoder.GetActivityPathString(eventSourceEvent.RelatedActivityId));
            }

            try
            {
                if (eventSourceEvent.Message != null)
                {
                    // If the event has a badly formatted manifest, the FormattedMessage property getter might throw
                    payloadData.Add(nameof(eventSourceEvent.Message), string.Format(CultureInfo.InvariantCulture, eventSourceEvent.Message, eventSourceEvent.Payload.ToArray()));
                }
            }
            catch { }

            eventSourceEvent.ExtractPayloadData(eventData, healthReporter, context);

            return eventData;
        }

        private static void ExtractPayloadData(this EventWrittenEventArgs eventSourceEvent, EventData eventData, IHealthReporter healthReporter, string context)
        {
            if (eventSourceEvent.Payload == null || eventSourceEvent.PayloadNames == null)
            {
                return;
            }

            IDictionary<string, object> payloadData = eventData.Payload;

            IEnumerator<object> payloadEnumerator = eventSourceEvent.Payload.GetEnumerator();
            IEnumerator<string> payloadNamesEnunmerator = eventSourceEvent.PayloadNames.GetEnumerator();
            while (payloadEnumerator.MoveNext())
            {
                payloadNamesEnunmerator.MoveNext();
                eventData.AddPayloadProperty(payloadNamesEnunmerator.Current, payloadEnumerator.Current, healthReporter, context);
            }
        }
    }
}