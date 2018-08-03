// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.Tracing;

using Microsoft.Diagnostics.EventFlow.Utilities.Etw;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal static class TraceEventExtensions
    {
        public static EventData ToEventData(this TraceEvent traceEvent, IHealthReporter healthReporter)
        {
            Debug.Assert(healthReporter != null);

            EventData eventData = new EventData
            {
                ProviderName = traceEvent.ProviderName,
                Timestamp = traceEvent.TimeStamp.ToUniversalTime(),
                Level = (LogLevel)(int)traceEvent.Level,
                Keywords = (long)traceEvent.Keywords
            };

            IDictionary<string, object> payloadData = eventData.Payload;
            payloadData.Add(nameof(traceEvent.ID), (int) traceEvent.ID);  // TraceEvent.ID is ushort, not CLS-compliant, so we cast to int
            payloadData.Add(nameof(traceEvent.EventName), traceEvent.EventName);
            if (traceEvent.ActivityID != default(Guid))
            {
                payloadData.Add(nameof(traceEvent.ActivityID), ActivityPathDecoder.GetActivityPathString(traceEvent.ActivityID));
            }
            if (traceEvent.RelatedActivityID != default(Guid))
            {
                payloadData.Add(nameof(traceEvent.RelatedActivityID), traceEvent.RelatedActivityID.ToString());
            }
            // ProcessID and ProcessName are somewhat common property names, so to minimize likelihood of conflicts we use a prefix
            payloadData.Add("TraceEventProcessID", traceEvent.ProcessID);
            payloadData.Add("TraceEventProcessName", traceEvent.ProcessName);

            try
            {
                // If the event has a badly formatted manifest, the FormattedMessage property getter might throw
                string message = traceEvent.FormattedMessage;
                if (message != null)
                {                    
                    payloadData.Add("Message", traceEvent.FormattedMessage);
                }
            }
            catch { }

            traceEvent.ExtractPayloadData(eventData, healthReporter);

            return eventData;
        }

        private static void ExtractPayloadData(this TraceEvent traceEvent, EventData eventData, IHealthReporter healthReporter)
        {
            bool hasPayload = traceEvent.PayloadNames != null && traceEvent.PayloadNames.Length > 0;
            if (!hasPayload)
            {
                return;
            }

            IDictionary<string, object> payloadData = eventData.Payload;
            for (int i = 0; i < traceEvent.PayloadNames.Length; i++)
            {
                try
                {
                    var payloadName = traceEvent.PayloadNames[i];
                    var payloadValue = traceEvent.PayloadValue(i);

                    eventData.AddPayloadProperty(payloadName, payloadValue, healthReporter, nameof(EtwInput));
                }
                catch { }
            }
        }
    }
}
