// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class OmsOutputConfiguration
    {
        // !!ACTION!!
        // If you make any changes here, please update the README.md file to reflect the new configuration
        public string WorkspaceId { get; set; }
        public string WorkspaceKey { get; set; }
        public string LogTypeName { get; set; }
        public bool UseAzureGov { get; set; }
    }
}
