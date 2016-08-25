// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    public class PerformanceCounterConfiguration
    {
        public string CounterCategory { get; set; }
        public string CounterName { get; set; }
        public int CollectionIntervalInSeconds { get; set; }

        // The following configuration options govern how the library finds the correct counter instance name for the current process

        // The name of the performance counter that provides instance name-to-process ID mapping        
        public string ProcessIdCounterName { get; set; }

        // The name of the performance counter category that should be used for the process ID counter
        // If this is not specified, CounterCategory value will be used for the process ID counter.
        public string ProcessIdCounterCategory { get; set; }

        // If set, it will be assumed that the instance name of the counter will follow the new .NET name format
        // See https://msdn.microsoft.com/en-us/library/dd537616(v=vs.110).aspx for more information
        // No need to set ProcessIdCounterName or ProcessIdCounterCategory
        public bool UseDotNetInstanceNameConvention { get; set; }

        public PerformanceCounterConfiguration()
        {
            this.CollectionIntervalInSeconds = 30;
        }

        public virtual bool Validate()
        {
            // CONSIDER: for well-known categories like Process and all .NET categories we could just assume default ProcessIdCounterName
            // when configuration is missing.
            return !string.IsNullOrWhiteSpace(CounterCategory) 
                && !string.IsNullOrWhiteSpace(CounterName)
                && (UseDotNetInstanceNameConvention || !string.IsNullOrWhiteSpace(ProcessIdCounterName));
        }
    }
}
