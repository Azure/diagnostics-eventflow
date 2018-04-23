// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Diagnostics.EventFlow.Metadata;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs.EventHub
{
    /// <summary>
    /// Object representing data associated with "PartitionKey" in Event Hub.
    /// </summary>
    internal class PartitionKeyData
    {
        internal static readonly string EventMetadataKind = "partitionKey";
        internal static readonly string PartitionKeyPropertyMoniker = "partitionKeyProperty";

        public string PartitionKey { get; private set; }

        private PartitionKeyData()
        {
        }

        public static DataRetrievalResult TryGetData(
            EventData eventData,
            EventMetadata eventTelemetryMetadata,
            out PartitionKeyData partitionKeyData)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNull(eventTelemetryMetadata, nameof(eventTelemetryMetadata));
            partitionKeyData = null;

            if (!EventMetadataKind.Equals(eventTelemetryMetadata.MetadataType, System.StringComparison.OrdinalIgnoreCase))
            {
                return DataRetrievalResult.InvalidMetadataType(eventTelemetryMetadata.MetadataType, EventMetadataKind);
            }

            string partitionKey = eventTelemetryMetadata[PartitionKeyPropertyMoniker];
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                return DataRetrievalResult.MissingMetadataProperty(PartitionKeyPropertyMoniker);
            }

            DataRetrievalResult result = eventTelemetryMetadata.GetEventPropertyValue(eventData, PartitionKeyPropertyMoniker, out partitionKey);
            if (result.Status != DataRetrievalStatus.Success)
            {
                return result;
            }

            partitionKeyData = new PartitionKeyData();
            partitionKeyData.PartitionKey = partitionKey;
            return DataRetrievalResult.Success;
        }
    }
}