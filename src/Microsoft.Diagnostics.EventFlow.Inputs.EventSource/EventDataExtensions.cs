// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.Threading;
using Microsoft.Diagnostics.EventFlow.Metadata;

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
                Timestamp = DateTimePrecise.UtcNow,
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

            bool isEventCountersEvent = eventSourceEvent.EventName == "EventCounters" && eventSourceEvent.Payload.Count == 1 && eventSourceEvent.PayloadNames[0] == "Payload";
            if (isEventCountersEvent)
            {
                ExtractEventCounterPayloadData(eventSourceEvent, eventData, healthReporter, context);
            }
            else
            {
                ExtractEventPayloadData(eventSourceEvent, eventData, healthReporter, context);
            }
        }

        private static void ExtractEventPayloadData(this EventWrittenEventArgs eventSourceEvent, EventData eventData, IHealthReporter healthReporter, string context)
        {
            IEnumerator<object> payloadEnumerator = eventSourceEvent.Payload.GetEnumerator();
            IEnumerator<string> payloadNamesEnunmerator = eventSourceEvent.PayloadNames.GetEnumerator();
            while (payloadEnumerator.MoveNext())
            {
                payloadNamesEnunmerator.MoveNext();
                eventData.AddPayloadProperty(payloadNamesEnunmerator.Current, payloadEnumerator.Current, healthReporter, context);
            }
        }

        private static void ExtractEventCounterPayloadData(this EventWrittenEventArgs eventSourceEvent, EventData eventData, IHealthReporter healthReporter, string context)
        {
            foreach(var payload in (IDictionary<string, object>)eventSourceEvent.Payload[0])
            {
                eventData.AddPayloadProperty(payload.Key, payload.Value, healthReporter, context);
                if (IsNumericValue(payload.Value))
                {
                    eventData.SetMetadata(CreateMetricMetadata(payload.Key));
                }
            }
        }

        private static EventMetadata CreateMetricMetadata(string property)
        {
            EventMetadata eventMetadata = new EventMetadata(MetricData.MetricMetadataKind);
            eventMetadata.Properties.Add(MetricData.MetricNamePropertyMoniker, property);
            eventMetadata.Properties.Add(MetricData.MetricValuePropertyMoniker, property);

            return eventMetadata;
        }

        private static bool IsNumericValue(object payloadValue)
        {
            if (payloadValue == null)
            {
                return false;
            }

            Type payloadValueType = payloadValue.GetType();
            if (payloadValueType == typeof(string))
            {
                return double.TryParse((string)payloadValue, out _);
            }
            else if (payloadValueType == typeof(double)
                  || payloadValueType == typeof(float)
                  || payloadValueType == typeof(long)
                  || payloadValueType == typeof(ulong)
                  || payloadValueType == typeof(int)
                  || payloadValueType == typeof(uint)
                  || payloadValueType == typeof(short)
                  || payloadValueType == typeof(ushort)
                  || payloadValueType == typeof(byte)
                  || payloadValueType == typeof(sbyte))
            {
                return true;
            }
            else return false;
        }
    }
}