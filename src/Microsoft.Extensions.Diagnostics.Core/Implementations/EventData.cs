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

        public object GetPropertyValue(string propertyName)
        {
            throw new NotImplementedException("Just make the parser work for now. This method needs to be implemented to make evaluators work.");
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