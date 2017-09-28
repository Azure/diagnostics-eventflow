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
#if !NETSTANDARD1_6
using System.Runtime.InteropServices;
#endif

using Microsoft.Diagnostics.EventFlow.Utilities.Etw;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal static class EventDataExtensions
    {
#if !NETSTANDARD1_6
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);
#endif

        public static EventData ToEventData(this EventWrittenEventArgs eventSourceEvent, IHealthReporter healthReporter, string context)
        {
            Debug.Assert(healthReporter != null);

            // High-precision event timestamping is availabe on .NET 4.6+ and .NET Core 2.0+ 
            // For the latter the implementation of DateTime.UtcNow has changed and we do not need to do anything.
            // .NET Core 1.1 will use imprecise timestamp--there is no easy fix for this target.

#if NETSTANDARD1_6
            DateTime now = DateTime.UtcNow;
#else
            GetSystemTimePreciseAsFileTime(out long filetime);
            DateTime now = DateTime.FromFileTimeUtc(filetime);
#endif
            EventData eventData = new EventData
            {
                ProviderName = eventSourceEvent.EventSource.Name,
                Timestamp = now,
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