// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    public class PerformanceCounterConfiguration
    {
        public string Name { get; set; }
        public string CounterCategory { get; set; }
        public string CoutnerName { get; set; }
        public int CollectionIntervalInSeconds { get; set; }

        public virtual bool Validate()
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(CounterCategory) && !string.IsNullOrWhiteSpace(CoutnerName);
        }
    }
}
