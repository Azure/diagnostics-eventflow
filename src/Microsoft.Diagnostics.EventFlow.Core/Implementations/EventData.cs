﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Validation;

namespace Microsoft.Diagnostics.EventFlow
{
    //
    // Note: this class is not thread-safe. Since it will not be used concurrently, we do not want 
    // to pay the cost of synchronized access to its members.
    //
    public class EventData: IDeepCloneable<EventData>
    {
        private Dictionary<string, object> payload;
        private Dictionary<string, List<EventMetadata>> metadata;

        public DateTimeOffset Timestamp { get; set; }

        public string ProviderName { get; set; }

        public LogLevel Level { get; set; }

        public long Keywords { get; set; }

        public IDictionary<string, object> Payload
        {
            get
            {
                if (this.payload == null)
                {
                    this.payload = new Dictionary<string, object>();
                }

                return this.payload;
            }
        }

        public bool TryGetMetadata(string metadataKind, out IReadOnlyCollection<EventMetadata> metadataOfKind)
        {
            Requires.NotNull(metadataKind, nameof(metadataKind));
            metadataOfKind = null;

            if (this.metadata == null)
            {
                return false;
            }

            List<EventMetadata> existingMetadata;
            if (!this.metadata.TryGetValue(metadataKind, out existingMetadata))
            {
                return false;
            }
            else
            {
                metadataOfKind = existingMetadata;
                return true;
            }
        }

        public void SetMetadata(EventMetadata newMetadata)
        {
            Requires.NotNull(newMetadata, nameof(newMetadata));

            string metadataKind = newMetadata.MetadataType;
            // CONSIDER: should metadataKind string be interned?

            if (this.metadata == null)
            {
                this.metadata = new Dictionary<string, List<EventMetadata>>();
            }

            List<EventMetadata> existingEntry;
            if (!this.metadata.TryGetValue(metadataKind, out existingEntry))
            {
                existingEntry = new List<EventMetadata>(1);
                this.metadata[metadataKind] = existingEntry;
            }

            existingEntry.Add(newMetadata);
        }

        /// <summary>
        /// Given the property name, retrieve the value from EventData.
        /// If the property name is not any common property of EventData, we will look up the payload.
        ///
        /// There can be a problem if some property in payload has the same name with common property (Timestamp for example).
        /// In this case, we can add some more functionality like append the propertyName with @, which means look property in payload.
        /// </summary>
        /// <param name="propertyName">The propertyName</param>
        /// <param name="value">The value of the property. Null if the property doesn't exist</param>
        /// <returns>True if find the property. False if the property doesn't exist</returns>
        public bool TryGetPropertyValue(string propertyName, out object value)
        {
            // TODO: Fine tuning this piece of logic. .Net core doesn't support creating delegate. If we use reflection to get the property at run time, performance can be an critical issue in this case.
            // However, the current implementation also has too many comparison, which may not be better than caching the PropertyInfo() and call GetValue()
            value = null;
            try
            {
                if (propertyName.Equals(nameof(Timestamp), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.Timestamp;
                }
                else if (propertyName.Equals(nameof(ProviderName), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.ProviderName;
                }
                else if (propertyName.Equals(nameof(Level), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.Level;
                }
                else if (propertyName.Equals(nameof(Keywords), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.Keywords;
                }
                else if (Payload.TryGetValue(propertyName, out value))
                {
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public delegate void ProcessPayload<T>(T value);

        public bool GetValueFromPayload<T>(string payloadName, ProcessPayload<T> handler)
        {
            if (string.IsNullOrEmpty(payloadName))
            {
                return false;
            }

            object p;
            if (!Payload.TryGetValue(payloadName, out p) || p == null)
            {
                return false;
            }

            bool converted = false;
            T value = default(T);

            try
            {
                value = (T)p;
                converted = true;
            }
            catch { }

            if (!converted)
            {
                try
                {
                    value = (T)Convert.ChangeType(p, typeof(T));
                    converted = true;
                }
                catch { }
            }

            if (converted)
            {
                handler(value);
            }

            return converted;
        }

        public void AddPayloadProperty(string key, object value, IHealthReporter healthReporter, string context)
        {
            PayloadDictionaryUtilities.AddPayloadProperty(this.Payload, key, value, healthReporter, context);
        }

        public EventData DeepClone()
        {
            var other = new EventData();
            other.Keywords = this.Keywords;
            other.Level = this.Level;
            other.ProviderName = this.ProviderName;
            other.Timestamp = this.Timestamp;
            if (this.payload != null)
            {
                other.payload = new Dictionary<string, object>(this.payload);
            }
            if (this.metadata != null)
            {
                other.metadata = new Dictionary<string, List<EventMetadata>>(this.metadata);
            }
            return other;
        }
    }
}