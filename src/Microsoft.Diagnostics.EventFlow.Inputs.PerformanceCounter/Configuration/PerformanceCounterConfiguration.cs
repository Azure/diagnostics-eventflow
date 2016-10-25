// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class PerformanceCounterConfiguration
    {
        private static readonly string DotNetProcessIdCounterCategory = ".NET CLR Memory";
        private static readonly string DotNetProcessIdCounterName = "Process ID";
        private static readonly string WindowsProcessIdCounterCategory = "Process";
        private static readonly string WindowsProcessIdCounterName = "ID Process";

        private string counterCategory;
        private string processIdCounterCategory;

        public string CounterCategory
        {
            get
            {
                return this.counterCategory;
            }
            set {
                this.counterCategory = value;
                
                // Infer ProcessIdCounterCategory and ProcessIdCounterName for well-known categories
                if (IsDotNetPerformanceCounterCategory(value))
                {
                    this.ProcessIdCounterCategory = DotNetProcessIdCounterCategory;
                    this.ProcessIdCounterName = DotNetProcessIdCounterName;
                }
                if (WindowsProcessIdCounterCategory.Equals(value, StringComparison.Ordinal))
                {
                    this.ProcessIdCounterCategory = WindowsProcessIdCounterCategory;
                    this.ProcessIdCounterName = WindowsProcessIdCounterName;
                }
            }
        }

        public string CounterName { get; set; }
        public int SamplingIntervalMsec { get; set; }

        // The following configuration options govern how the library finds the correct counter instance name for the current process

        // The name of the performance counter that provides instance name-to-process ID mapping        
        public string ProcessIdCounterName { get; set; }

        // The name of the performance counter category that should be used for the process ID counter
        public string ProcessIdCounterCategory
        {
            get
            {
                // If ProcessIdCounterCategory is not set explicitly, use CounterCategory
                return this.processIdCounterCategory ?? this.CounterCategory;
            }
            set
            {
                this.processIdCounterCategory = value;
            }
        }

        // If set, it will be assumed that the instance name of the counter will follow the new .NET name format 
        // (ProcessNameFormat set to 1, see https://msdn.microsoft.com/en-us/library/dd537616(v=vs.110).aspx for more information).
        // No need to set ProcessIdCounterName or ProcessIdCounterCategory.
        public bool UseDotNetInstanceNameConvention { get; set; }

        public PerformanceCounterConfiguration()
        {
            this.SamplingIntervalMsec = 30000;
        }

        public virtual bool Validate()
        {
            return !string.IsNullOrWhiteSpace(CounterCategory)
                && !string.IsNullOrWhiteSpace(CounterName)
                && (UseDotNetInstanceNameConvention || !string.IsNullOrWhiteSpace(ProcessIdCounterName));
        }

        private bool IsDotNetPerformanceCounterCategory(string categoryName)
        {
            return categoryName != null && categoryName.StartsWith(".NET", StringComparison.Ordinal);
        }
    }
}
