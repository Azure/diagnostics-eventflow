// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Newtonsoft.Json;
using MessagingEventData = Microsoft.ServiceBus.Messaging.EventData;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    internal static class EventDataExtensions
    {
        internal class ShoeBoxRecord
        {
            public DateTimeOffset time;
        }

        internal class ShoeBoxTraceRecord : ShoeBoxRecord
        {
            public string category;
            public string level;
            public Dictionary<string, object> properties = new Dictionary<string, object>();
        }

        internal class ShoeBoxMetricRecord : ShoeBoxRecord
        {
            public double last;
            public string metricName;
            public string timeGrain;
            public Dictionary<string, object> dimensions;
        }

        internal class ShoeBoxEventData
        {
            public List<ShoeBoxRecord> records = new List<ShoeBoxRecord>();
        }

        private static readonly JsonSerializerSettings serializerSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        public static MessagingEventData ToMessagingEventData(this EventData eventData, out int messageSize)
        {
            IReadOnlyCollection<EventMetadata> metadataCollection;
            var sbEventData = eventData.TryGetMetadata("metric", out metadataCollection)
                ? ConvertToShoeboxMetric(eventData, metadataCollection)
                : ConvertToShoeboxTrace(eventData);

            // If this turns out to consume significant CPU time, we could serialize the object "manually".
            // See https://github.com/aspnet/Logging/blob/dev/src/Microsoft.Extensions.Logging.EventSource/EventSourceLogger.cs for an example.
            // This avoids the reflection cost that is associate with single-call SerializeObject approach
            string eventDataSerialized = JsonConvert.SerializeObject(sbEventData, serializerSetting);

            byte[] messageBytes = Encoding.UTF8.GetBytes(eventDataSerialized);
            messageSize = messageBytes.Length;
            return new MessagingEventData(messageBytes);
        }

        private static ShoeBoxEventData ConvertToShoeboxTrace(EventData eventData)
        {
            ShoeBoxEventData sbEventData = new ShoeBoxEventData();

            var traceRecord = new ShoeBoxTraceRecord()
            {
                time = eventData.Timestamp,
                level = eventData.Level.GetName(),
                category = null
            };

            traceRecord.properties.Add(nameof(eventData.Keywords), eventData.Keywords);
            traceRecord.properties.Add(nameof(eventData.ProviderName), eventData.ProviderName);

            foreach (var payload in eventData.Payload)
            {
                traceRecord.properties.Add(payload.Key, payload.Value.ToString());
            }

            sbEventData.records.Add(traceRecord);

            return sbEventData;
        }

        private static ShoeBoxEventData ConvertToShoeboxMetric(EventData eventData, IReadOnlyCollection<EventMetadata> metadataCollection)
        {
            ShoeBoxEventData sbEventData = new ShoeBoxEventData();

            foreach (var metadata in metadataCollection)
            {
                double metricValue = default(double);
                string metricValueProperty = metadata["metricValueProperty"];
                if (string.IsNullOrEmpty(metricValueProperty))
                {
                    double.TryParse(metadata["metricValue"], out metricValue);
                }
                else
                {
                    eventData.GetValueFromPayload<double>(metricValueProperty, (v) => metricValue = v);
                }

                var metricRecord = new ShoeBoxMetricRecord()
                {
                    last = metricValue,
                    metricName = metadata["metricName"],
                    time = eventData.Timestamp,
                    timeGrain = null,
                    dimensions = null,
                };

                sbEventData.records.Add(metricRecord);
            }

            return sbEventData;
        }
    }
}
