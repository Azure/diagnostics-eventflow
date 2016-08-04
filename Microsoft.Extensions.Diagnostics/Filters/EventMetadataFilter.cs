// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.Metadata;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class EventMetadataFilter<TMetadata>: IEventFilter<EventData> where TMetadata : EventMetadata
    {
        private EventMetadataCollection<TMetadata> metadataCollection;
        private string metadataKind;

        public EventMetadataFilter(EventMetadataCollection<TMetadata> metadataCollection, string metadataKind)
        {
            Requires.NotNull(metadataCollection, nameof(metadataCollection));
            Requires.NotNullOrWhiteSpace(metadataKind, nameof(metadataKind));

            this.metadataCollection = metadataCollection;
            this.metadataKind = metadataKind;
        }

        public bool Filter(EventData eventData)
        {
            TMetadata metadata = this.metadataCollection.GetMetadata(eventData.ProviderName, eventData.EventName);
            if (metadata != null)
            {
                eventData.SetMetadata(this.metadataKind, metadata);
            }

            return true;
        }
    }
}
