// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Diagnostics.Tracing;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class EventSourceConfiguration
    {
        public string ProviderName { get; set; }
        public EventLevel Level { get; set; } = EventLevel.LogAlways;
        public EventKeywords Keywords { get; set; } = EventKeywords.All;
        public string DisabledProviderNamePrefix { get; set; }
        public string ProviderNamePrefix { get; set; }

        public bool Validate()
        {
            bool[] enabledSettings = new bool[]
            {
                !string.IsNullOrWhiteSpace(ProviderName),
                !string.IsNullOrWhiteSpace(ProviderNamePrefix),
                !string.IsNullOrWhiteSpace(DisabledProviderNamePrefix)
            };

            // One and only one setting should be present
            if (enabledSettings.Count(setting => setting) != 1)
            {
                return false;
            }

            // If disabling, Keywords and Level must remain at default values (the are ineffective)
            if (!string.IsNullOrWhiteSpace(DisabledProviderNamePrefix))
            {
                if (Keywords != EventKeywords.All || Level != EventLevel.LogAlways)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Enables(EventSource eventSource)
        {
            Requires.NotNull(eventSource, nameof(eventSource));

            if (!string.IsNullOrWhiteSpace(ProviderName))
            {
                return eventSource.Name == ProviderName;
            }
            else if (!string.IsNullOrWhiteSpace(ProviderNamePrefix))
            {
                return eventSource.Name?.StartsWith(ProviderNamePrefix, StringComparison.Ordinal) ?? false;
            }
            else return false;
        }

        public bool Disables(EventSource eventSource)
        {
            Requires.NotNull(eventSource, nameof(eventSource));

            if (!string.IsNullOrWhiteSpace(DisabledProviderNamePrefix))
            {
                return eventSource.Name?.StartsWith(DisabledProviderNamePrefix, StringComparison.Ordinal) ?? false;
            }
            else return false;
        }

        public override bool Equals(object obj)
        {
            var other = obj as EventSourceConfiguration;
            if (other == null)
            {
                return false;
            }

            if (ProviderName != other.ProviderName || 
                ProviderNamePrefix != other.ProviderNamePrefix || 
                DisabledProviderNamePrefix != other.DisabledProviderNamePrefix || 
                Level != other.Level || 
                Keywords != other.Keywords)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (ProviderName != null)
            {
                return ProviderName.GetHashCode();
            }
            else if (ProviderNamePrefix != null)
            {
                return ProviderNamePrefix.GetHashCode();
            }
            else if (DisabledProviderNamePrefix != null)
            {
                return DisabledProviderNamePrefix.GetHashCode();
            }
            else return 0;
        }
    }
}
