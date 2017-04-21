// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Validation;
using Microsoft.Diagnostics.EventFlow.Metadata;

namespace Microsoft.Diagnostics.EventFlow.Outputs.ApplicationInsights
{
    /// <summary>
    /// Object representing data associated with "Event" telemetry type in Application Insights.
    /// </summary>
    internal class EventTelemetryData
    {
        internal static readonly string EventMetadataKind = "ai_event";
        internal static readonly string EventNamePropertyMoniker = "eventNameProperty";
        internal static readonly string EventNameMoniker = "eventName";

        public string Name { get; private set; }

        private EventTelemetryData() { }

        public static DataRetrievalResult TryGetData(
            EventData eventData,
            EventMetadata eventTelemetryMetadata,
            out EventTelemetryData eventTelemetryData)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNull(eventTelemetryMetadata, nameof(eventTelemetryMetadata));
            eventTelemetryData = null;

            if (!EventMetadataKind.Equals(eventTelemetryMetadata.MetadataType, System.StringComparison.OrdinalIgnoreCase))
            {
                return DataRetrievalResult.InvalidMetadataType(eventTelemetryMetadata.MetadataType, EventMetadataKind);
            }

            string eventName = eventTelemetryMetadata[EventNameMoniker];
            if (string.IsNullOrWhiteSpace(eventName))
            {
                DataRetrievalResult result = eventTelemetryMetadata.GetEventPropertyValue(eventData, EventNamePropertyMoniker, out eventName);
                if (result.Status != DataRetrievalStatus.Success)
                {
                    return result;
                }
            }

            eventTelemetryData = new EventTelemetryData();
            eventTelemetryData.Name = eventName;
            return DataRetrievalResult.Success;
        }
    }
}
