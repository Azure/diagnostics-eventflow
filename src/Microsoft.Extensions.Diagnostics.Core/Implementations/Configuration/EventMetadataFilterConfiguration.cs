// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    public class EventMetadataFilterConfiguration: ItemConfiguration
    {
        // Indicates metadata kind
        public string Metadata { get; set; }

        public string Include { get; set; }
    }
}
