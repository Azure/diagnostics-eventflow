// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Configuration;

namespace Microsoft.Extensions.Diagnostics
{
    public class EventMetadataFilterConfiguration: ItemConfiguration
    {
        // Indicates metadata kind
        public string Metadata { get; set; }

        public string Include { get; set; }
    }
}
