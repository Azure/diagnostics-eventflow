// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class EtwProviderConfiguration
    {
        public string ProviderName { get; set; }
        public TraceEventLevel Level { get; set; }
        public TraceEventKeyword Keywords { get; set; }
        public Guid ProviderGuid { get; set; }

        public EtwProviderConfiguration()
        {
            Level = TraceEventLevel.Always;
            Keywords = TraceEventKeyword.All;
        }

        public override bool Equals(object obj)
        {
            var other = obj as EtwProviderConfiguration;
            if (other == null)
            {
                return false;
            }

            return (ProviderName == other.ProviderName || ProviderGuid == other.ProviderGuid) 
                   && Level == other.Level 
                   && Keywords == other.Keywords;
        }

        public override int GetHashCode()
        {
            if (ProviderGuid != Guid.Empty)
            {
                return ProviderGuid.GetHashCode();
            }
            else
            {
                return ProviderName?.GetHashCode() ?? 0;
            }
        }

        public bool Validate(out string validationError)
        {
            validationError = null;
            if (string.IsNullOrWhiteSpace(this.ProviderName) && this.ProviderGuid == Guid.Empty)
            {
                validationError = "Either ProviderName or ProviderGuid must be specified";
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
