﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class PerformanceCounterInputConfiguration: ItemConfiguration
    {
        public List<PerformanceCounterConfiguration> Counters { get; set; }
        public int SampleIntervalMsec { get; set; }

        public PerformanceCounterInputConfiguration()
        {
            this.SampleIntervalMsec = 10000;  // Default sample interval 10 seconds
        }
    }
}
