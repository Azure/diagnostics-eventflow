using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
    public class TagInfo
    {
        public string context { get; set; }
        public string key { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string value { get; set; }

    }
}
