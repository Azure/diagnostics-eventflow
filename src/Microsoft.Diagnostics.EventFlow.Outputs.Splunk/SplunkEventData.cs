// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Splunk
{
    /// <summary>
    /// Class representing an "Event" consumed by the Splunk HTTP Event Collector (HEC).
    /// </summary>
    internal class SplunkEventData
    {
        public SplunkEventData(
            EventData eventData,
            string host = null,
            string index = null, 
            string source = null,
            string sourceType = null)
        {
            var dateTime = eventData.Timestamp.DateTime.ToLocalTime();
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixDateTime = (dateTime.ToUniversalTime() - epoch).TotalSeconds;
            Timestamp = unixDateTime.ToString("#.000");
            Event = eventData;
            Host = host;
            Index = index;
            Source = source;
            SourceType = sourceType;            
        }

        /// <summary>
        /// Event timestamp in epoch format.
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        public string Timestamp { get; private set; }

        /// <summary>
        /// Event metadata host.
        /// </summary>
        [JsonProperty(PropertyName = "host", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Host { get; private set; }

        /// <summary>
        /// Event metadata index.
        /// </summary>
        [JsonProperty(PropertyName = "index", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Index { get; private set; }

        /// <summary>
        /// Event metadata source.
        /// </summary>
        [JsonProperty(PropertyName = "source", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Source { get; private set; }

        /// <summary>
        /// Event metadata sourcetype.
        /// </summary>
        [JsonProperty(PropertyName = "sourcetype", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SourceType { get; private set; }        

        /// <summary>
        /// Event data.
        /// </summary>
        [JsonProperty(PropertyName = "event", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public EventData Event { get; private set; }
    }
}