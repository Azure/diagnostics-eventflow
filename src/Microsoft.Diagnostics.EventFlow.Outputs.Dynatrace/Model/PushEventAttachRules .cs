using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
    public class PushEventAttachRules
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] entityIds { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TagMatchRule[] tagRule { get; set; }
    }
}
