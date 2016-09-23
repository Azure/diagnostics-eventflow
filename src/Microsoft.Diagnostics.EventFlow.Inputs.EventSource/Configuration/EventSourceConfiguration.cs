// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class EventSourceConfiguration
    {
        public string ProviderName { get; set; }
        public EventLevel Level { get; set; }
        public EventKeywords Keywords { get; set; }

        public EventSourceConfiguration()
        {
            Level = EventLevel.LogAlways;
            Keywords = (EventKeywords) ~0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as EventSourceConfiguration;
            if (other == null)
            {
                return false;
            }

            return ProviderName == other.ProviderName && Level == other.Level && Keywords == other.Keywords;
        }

        public override int GetHashCode()
        {
            return ProviderName.GetHashCode();
        }
    }
}
