// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    [Flags]
    public enum CapturedActivityEvents
    {
        None = 0,
        Start = 1,
        Stop = 2,
        Both = 3
    }

    public class ActivitySourceConfiguration
    {
        public string ActivityName { get; set; }
        public string ActivitySourceName { get; set; }
        public ActivitySamplingResult CapturedData { get; set; }
        public CapturedActivityEvents CapturedEvents { get; set; }

        public ActivitySourceConfiguration()
        {
            this.ActivityName = this.ActivitySourceName = null;
            this.CapturedData = ActivitySamplingResult.AllData;
            this.CapturedEvents = CapturedActivityEvents.Stop;
        }

        public ActivitySourceConfiguration(ActivitySourceConfiguration other)
        {
            this.ActivityName = other.ActivityName;
            this.ActivitySourceName = other.ActivitySourceName;
            this.CapturedData = other.CapturedData;
            this.CapturedEvents = other.CapturedEvents;
        }
    }
}
