// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class EventMetadataFilterConfiguration: IncludeConditionFilterConfiguration
    {
        // Indicates metadata kind
        public string Metadata { get; set; }        
    }
}
