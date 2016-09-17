// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class EventHubOutputConfiguration: ItemConfiguration
    {
        public string ConnectionString { get; set; }
        public string EventHubName { get; set; }
    }
}
