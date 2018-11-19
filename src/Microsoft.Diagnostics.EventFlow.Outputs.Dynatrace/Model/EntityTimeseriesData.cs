using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
    public class EntityTimeseriesData
    {
        public string timeseriesId { get; set; }
        public Dictionary<string, string> dimensions { get; set; }
        public object[][] dataPoints { get; set; }
    }
}
