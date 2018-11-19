using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
    public class EventPushMessage
    {
        public string eventType { get; set;}
        public long start {get; set;}
        public PushEventAttachRules attachRules { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string source { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string annotationType { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string annotationDescription { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string description { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string deploymentName { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string deploymentVersion { get; set;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string configuration { get; set; }
    }
}
