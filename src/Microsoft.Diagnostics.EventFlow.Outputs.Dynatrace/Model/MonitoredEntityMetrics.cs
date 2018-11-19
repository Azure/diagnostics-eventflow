using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
   
    public class MonitoredEntityMetrics
    {
        public string type { get; set; }
        public List<EntityTimeseriesData> series { get; set; }
    }
}
