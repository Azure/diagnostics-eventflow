using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
   
    public class MonitoredEntity
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string displayName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] ipAddresses { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] listenPorts { get; set; }
        public string type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string favicon { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string configUrl { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] tags  { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> properties { get; set; }
    }
}
