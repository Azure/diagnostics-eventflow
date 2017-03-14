// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal class ProcessInstanceNameCache
    {
        private Dictionary<CacheKey, string> instanceNames = new Dictionary<CacheKey, string>();

        public string GetCounterInstanceNameForCurrentProcess(PerformanceCounterConfiguration counterConfiguration)
        {
            Requires.NotNull(counterConfiguration, nameof(counterConfiguration));

            var key = new CacheKey(counterConfiguration.ProcessIdCounterCategory, counterConfiguration.ProcessIdCounterName);
            string instanceName;
            if (this.instanceNames.TryGetValue(key, out instanceName))
            {
                return instanceName;
            }

            Process currentProcess = Process.GetCurrentProcess();

            if (counterConfiguration.UseDotNetInstanceNameConvention)
            {
                instanceName = VersioningHelper.MakeVersionSafeName(currentProcess.ProcessName, ResourceScope.Machine, ResourceScope.AppDomain);
                // We actually don't need the AppDomain portion, but there is no way to get the runtime ID without AppDomain id attached.
                // So we just filter it out 
                int adIdIndex = instanceName.LastIndexOf("_ad");
                if (adIdIndex > 0)
                {
                    instanceName = instanceName.Substring(0, adIdIndex);
                }
                this.instanceNames[key] = instanceName;
                return instanceName;
            }
            else
            {
                PerformanceCounterCategory category = new PerformanceCounterCategory(counterConfiguration.ProcessIdCounterCategory);

                string[] processInstanceNames = category.GetInstanceNames()
                    .Where(inst => inst.ToLowerInvariant().StartsWith(currentProcess.ProcessName.ToLowerInvariant()))
                    .ToArray();

                foreach (string processInstanceName in processInstanceNames)
                {
                    using (PerformanceCounter cnt = new PerformanceCounter(counterConfiguration.ProcessIdCounterCategory, counterConfiguration.ProcessIdCounterName, processInstanceName, readOnly: true))
                    {
                        int val = (int)cnt.RawValue;
                        if (val == currentProcess.Id)
                        {
                            this.instanceNames[key] = processInstanceName;
                            return processInstanceName;
                        }
                    }
                }
            }

            this.instanceNames[key] = null;
            return null;
        }

        public void Clear()
        {
            this.instanceNames.Clear();
        }

        private class CacheKey : Tuple<string, string>
        {
            public CacheKey(string counterCategory, string counterName) : base(counterCategory, counterName) { }

            public string CounterCategory { get { return this.Item1; } }
            public string CounterName { get { return this.Item2; } }
        }
    }
}
