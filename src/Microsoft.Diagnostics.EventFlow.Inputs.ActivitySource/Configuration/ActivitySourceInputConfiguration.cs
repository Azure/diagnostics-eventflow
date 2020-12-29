// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class ActivitySourceInputConfiguration: ItemConfiguration
    {
        public List<ActivitySourceConfiguration> Sources { get; set; } = new List<ActivitySourceConfiguration>();

        public ActivitySourceInputConfiguration DeepClone()
        {
            ActivitySourceInputConfiguration clone = new ActivitySourceInputConfiguration();
            clone.Sources.AddRange(Sources.Select(s => new ActivitySourceConfiguration(s)));
            return clone;
        }
    }
}
