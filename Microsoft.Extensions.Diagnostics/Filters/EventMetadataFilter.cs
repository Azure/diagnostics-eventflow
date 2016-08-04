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
        private MetadataCollection<TMetadata> metadataCollection;

        public EventMetadataFilter(MetadataCollection<TMetadata> metadataCollection, IHealthReporter healthReporter)
        {
            Requires.NotNull(metadataCollection, nameof(metadataCollection));

            this.metadataCollection = metadataCollection;
        }

        public bool Filter(EventData eventData)
        {
            TMetadata metadata = this.metadataCollection.GetMetadata(eventData.ProviderName, eventData.EventName);
            if (metadata != null)
            {
                eventData.SetMetadata(typeof(TMetadata), metadata);
            }

            return true;
        }
    }
}
