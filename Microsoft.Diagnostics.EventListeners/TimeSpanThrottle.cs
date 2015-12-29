// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventListeners
{
    using System;

    /// <summary>
    /// Allows time-based throttling the execution of a method/delegate. Only one execution per given time span is performed.
    /// </summary>
    public class TimeSpanThrottle
    {
        private TimeSpan throttlingTimeSpan;
        private DateTimeOffset? lastExecutionTime;
        private object lockObject;

        public TimeSpanThrottle(TimeSpan throttlingTimeSpan)
        {
            this.throttlingTimeSpan = throttlingTimeSpan;
            this.lockObject = new object();
        }

        public void Execute(Action work)
        {
            lock (this.lockObject)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (this.lastExecutionTime != null && (now - this.lastExecutionTime) < this.throttlingTimeSpan)
                {
                    return;
                }

                this.lastExecutionTime = now;
            }
            work();
        }
    }
}