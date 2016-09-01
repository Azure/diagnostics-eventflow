// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow
{
    /// <summary>
    /// Allows time-based throttling the execution of a method/delegate. Only one execution per given time span is performed.
    /// </summary>
    internal class TimeSpanThrottle
    {
        private TimeSpan throttlingTimeSpan;
        private DateTimeOffset? lastExecutionTime;
        private object lockObject;

        public TimeSpanThrottle(TimeSpan throttlingTimeSpan)
        {
            this.throttlingTimeSpan = throttlingTimeSpan;
            this.lockObject = new object();
        }

        /// <summary>
        /// Only one action can be triggered during a given timespan.
        /// If the timespan is zero or negative, then there is no throttling.
        /// </summary>
        /// <param name="work">The action to be executed</param>
        public void Execute(Action work)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (TooEarly(now))
            {
                return;
            }

            lock (this.lockObject)
            {
                if (TooEarly(now))
                {
                    return;
                }

                this.lastExecutionTime = now;
            }
            work();
        }

        private bool TooEarly(DateTimeOffset now)
        {
            return throttlingTimeSpan.TotalMilliseconds <= 0
                ? false
                : this.lastExecutionTime != null && (now - this.lastExecutionTime) < this.throttlingTimeSpan;
        }
    }
}