using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
    public class TimeseriesRegistrationMessage
    {
        public string displayName { get; set; }
        public string unit { get; set; }    
        public string[] dimensions { get; set; }
        public string[] types { get; set; }
    }
}
