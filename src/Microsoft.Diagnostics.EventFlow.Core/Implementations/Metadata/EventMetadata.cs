// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using Validation;
using System.Diagnostics;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public class EventMetadata
    {
        public string MetadataType { get; private set; }
        
        public IDictionary<string, string> Properties { get; private set; }

        public EventMetadata(string metadataKind)
        {
            Requires.NotNullOrWhiteSpace(metadataKind, nameof(metadataKind));
            this.MetadataType = metadataKind;
            this.Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // Rather than throwing KeyNotFoundException we simply return null if the property does not exist.
        public string this[string propertyName]
        {
            get
            {
                string value;
                this.Properties.TryGetValue(propertyName, out value);
                return value;
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as EventMetadata;
            if (other == null)
            {
                return false;
            }

            if (MetadataType != other.MetadataType)
            {
                return false;
            }

            var sortedKeys = this.Properties.Keys.OrderBy(k => k, StringComparer.Ordinal);
            var otherSortedKeys = other.Properties.Keys.OrderBy(k => k, StringComparer.Ordinal);
            return sortedKeys.SequenceEqual(otherSortedKeys)
                && sortedKeys.Select(k => this.Properties[k]).SequenceEqual(otherSortedKeys.Select(k2 => other.Properties[k2]));
        }

        public override int GetHashCode()
        {
            return this.MetadataType.GetHashCode();
        }

        // Retrieves an event value from a property that is named in the metadata. For example, if the metadataPropertyName parameter is
        // "ResponseCodeProperty", we read the "ResponseCodeProperty" from the metadata. Suppose its value is "returnCode". 
        // Then we look into the event and try to find "returnCode" property in the payload. If successful, we return its value to the caller
        // (e.g. it could be "200 OK", indicating successful HTTP call).
        public DataRetrievalResult GetEventPropertyValue<T>(
            EventData eventData,
            string metadataPropertyName,
            out T eventPropertyValue)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNullOrEmpty(metadataPropertyName, nameof(metadataPropertyName));
            eventPropertyValue = default(T);

            string eventPropertyName = this[metadataPropertyName];
            if (string.IsNullOrWhiteSpace(eventPropertyName))
            {
                return DataRetrievalResult.MissingMetadataProperty(metadataPropertyName);
            }

            T value = default(T);
            if (!eventData.GetValueFromPayload<T>(eventPropertyName, (v) => value = v))
            {
                return DataRetrievalResult.DataMissingOrInvalid(eventPropertyName);
            }
            else
            {
                eventPropertyValue = value;
            }

            return DataRetrievalResult.Success;
        }


    }
}
