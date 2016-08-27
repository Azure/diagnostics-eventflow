// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    internal class EventSourceInputConfiguration: ItemConfiguration
    {
        public List<EventSourceConfiguration> Sources { get; set; }
    }
}
