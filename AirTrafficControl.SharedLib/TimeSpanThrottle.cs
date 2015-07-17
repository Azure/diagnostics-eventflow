using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.SharedLib
{
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
