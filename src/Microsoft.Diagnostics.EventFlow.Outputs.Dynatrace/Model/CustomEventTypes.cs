using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model
{
    public class CustomEventTypes
    {
        public static readonly string ANNOTATION = "CUSTOM_ANNOTATION";
        public static readonly string CONFIGURATION = "CUSTOM_CONFIGURATION";
        public static readonly string DEPLOYMENT = "CUSTOM_DEPLOYMENT";
        public static readonly string INFO = "CUSTOM_INFO";

        public static readonly string AVAILABILITY = "AVAILABILITY_EVENT";
        public static readonly string ERROR = "ERROR_EVENT";
        public static readonly string PERFORMANCE = "PERFORMANCE_EVENT";
        public static readonly string RESOURCE = "RESOURCE_CONTENTION";
    }
}
