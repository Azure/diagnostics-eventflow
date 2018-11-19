using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
    public class TagMatchRule
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] meTypes { get; set; }
        public TagInfo[] tags { get; set; }
    }
}
