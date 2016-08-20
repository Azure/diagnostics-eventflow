// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.Metadata;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class EventMetadataFilter: IEventFilter<EventData>
    {
        private EventMetadata metadata;

        public EventMetadataFilter(EventMetadata metadata)
        {
            Requires.NotNull(metadata, nameof(metadata));
            this.metadata = metadata;
        }

        public bool Filter(EventData eventData)
        {
            this.metadata.TryApplying(eventData);
            return true;
        }

        public override bool Equals(object obj)
        {
            return (obj as EventMetadataFilter)?.metadata.Equals(this.metadata) ?? false;
        }

        public override int GetHashCode()
        {
            return this.metadata.GetHashCode();
        }
    }
}
