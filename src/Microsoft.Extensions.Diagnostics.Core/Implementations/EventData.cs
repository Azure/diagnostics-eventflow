// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.Metadata;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    //
    // Note: this class is not thread-safe. Since it will not be used concurrently, we do not want 
    // to pay the cost of synchronized access to its members.
    //
    public class EventData: IDeepCloneable<EventData>
    {
        private Dictionary<string, object> payload;

        private Dictionary<string, object> metadata;

        public DateTimeOffset Timestamp { get; set; }

        public string ProviderName { get; set; }

        public int EventId { get; set; }

        public string Message { get; set; }

        public string Level { get; set; }

        public string Keywords { get; set; }

        public string EventName { get; set; }

        public string ActivityID { get; set; }

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

        // Since in vast majority of cases only single piece of metadata of given kind will be carried by the EventData instance,
        // we optimize for that case and avoid the cost of allocating a list of metadata if only one instance of a given 
        // metadata kind is associated with this EventData.
        //
        // If only one instance of the metadata exists, it will be returned via singleMetadata parameter
        // If multiple instances exist, it will be returned via multiMetatada parameter.
        public bool TryGetMetadata(string metadataKind, out EventMetadata singleMetadata, out IEnumerable<EventMetadata> multiMetadata)
        {
            Requires.NotNull(metadataKind, nameof(metadataKind));
            multiMetadata = null;
            singleMetadata = null;

            if (this.metadata == null)
            {
                return false;
            }

            object metadata;
            if (!this.metadata.TryGetValue(metadataKind, out metadata))
            {
                return false;
            }

            singleMetadata = metadata as EventMetadata;
            if (singleMetadata != null)
            {
                return true;
            }

            multiMetadata = metadata as IEnumerable<EventMetadata>;
            Debug.Assert(multiMetadata != null);
            return true;
        }

        public void SetMetadata(EventMetadata newMetadata)
        {
            Requires.NotNull(newMetadata, nameof(newMetadata));

            string metadataKind = newMetadata.MetadataType;
            // CONSIDER: should metadataKind string be interned?

            if (this.metadata == null)
            {
                this.metadata = new Dictionary<string, object>();
            }

            object existingEntry;
            if (!this.metadata.TryGetValue(metadataKind, out existingEntry))
            {
                this.metadata[metadataKind] = newMetadata;
                return;
            }

            var metadataList = existingEntry as List<EventMetadata>;
            if (metadataList != null)
            {
                metadataList.Add(newMetadata);
            }
            else
            {
                EventMetadata oldMetadata = existingEntry as EventMetadata;
                Debug.Assert(oldMetadata != null);
                metadataList = new List<EventMetadata>(2);
                metadataList.Add(oldMetadata);
                metadataList.Add(newMetadata);
            }
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
                else if (propertyName.Equals(nameof(EventId), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.EventId;
                }
                else if (propertyName.Equals(nameof(Message), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.Message;
                }
                else if (propertyName.Equals(nameof(Level), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.Level;
                }
                else if (propertyName.Equals(nameof(Keywords), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.Keywords;
                }
                else if (propertyName.Equals(nameof(EventName), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.EventName;
                }
                else if (propertyName.Equals(nameof(ActivityID), StringComparison.OrdinalIgnoreCase))
                {
                    value = this.ActivityID;
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

        public EventData DeepClone()
        {
            var other = new EventData();
            other.ActivityID = this.ActivityID;
            other.EventId = this.EventId;
            other.EventName = this.EventName;
            other.Keywords = this.Keywords;
            other.Level = this.Level;
            other.Message = this.Message;
            other.ProviderName = this.ProviderName;
            other.Timestamp = this.Timestamp;
            other.payload = new Dictionary<string, object>(this.payload);
            other.metadata = new Dictionary<string, object>(this.metadata);
            return other;
        }
    }
}