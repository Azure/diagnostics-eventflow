// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal class TrackedPerformanceCounter : IDisposable
    {
        private PerformanceCounter counter;
        private bool disposed;
        private DateTimeOffset lastAccessedOn;
        private string lastInstanceName;

        public PerformanceCounterConfiguration Configuration { get; private set; }

        public TrackedPerformanceCounter(PerformanceCounterConfiguration configuration)
        {
            Requires.NotNull(configuration, nameof(configuration));

            this.Configuration = configuration;
            this.lastAccessedOn = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            this.disposed = false;
        }

        public bool SampleNextValue(ProcessInstanceNameCache processInstanceNameCache, out float newValue)
        {
            Requires.NotNull(processInstanceNameCache, nameof(processInstanceNameCache));

            newValue = 0;
            if (disposed)
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow;
            if (now - this.lastAccessedOn < TimeSpan.FromMilliseconds(this.Configuration.SamplingIntervalMsec))
            {
                return false;
            }

            string instanceName = processInstanceNameCache.GetCounterInstanceNameForCurrentProcess(this.Configuration);
            this.lastAccessedOn = now;
            if (this.counter == null || (!string.IsNullOrEmpty(instanceName) && instanceName != this.lastInstanceName))
            {
                if (this.counter != null)
                {
                    this.counter.Dispose();
                }
                this.counter = new PerformanceCounter(
                    this.Configuration.CounterCategory,
                    this.Configuration.CounterName,
                    instanceName,
                    readOnly: true);
            }
            this.lastInstanceName = instanceName;

            newValue = this.counter.NextValue();
            return true;
        }

        public void Dispose()
        {
            if (this.counter != null)
            {
                this.counter.Dispose();
                this.counter = null;
            }
        }
    }
}
