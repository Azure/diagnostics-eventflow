// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.Metadata;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Filters
{
    public class EventMetadataFilter: IncludeConditionFilter
    {
        private EventMetadata metadata;

        public EventMetadataFilter(EventMetadata metadata, string includeCondition = null): base(includeCondition)
        {
            Requires.NotNull(metadata, nameof(metadata));
            this.metadata = metadata;
        }

        public override FilterResult Evaluate(EventData eventData)
        {
            if (this.Evaluator.Evaluate(eventData))
            {
                eventData.SetMetadata(this.metadata);
            }
            return FilterResult.KeepEvent;
        }

        public override bool Equals(object obj)
        {
            EventMetadataFilter other = obj as EventMetadataFilter;
            if (other == null)
            {
                return false;
            }

            return other.IncludeCondition == this.IncludeCondition && other.metadata.Equals(this.metadata);
        }

        public override int GetHashCode()
        {
            return this.IncludeCondition.GetHashCode() ^ this.metadata.GetHashCode();
        }
    }
}
