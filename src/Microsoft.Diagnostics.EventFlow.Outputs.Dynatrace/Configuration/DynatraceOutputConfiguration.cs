// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.Outputs.Dynatrace.Model;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class DynatraceOutputConfiguration : ItemConfiguration
    {
        public string ServiceBaseAddress { get; set; }
        public string ServiceAPIEndpoint { get; set; }
        public string APIToken { get; set; }

        public MonitoredEntityConfig MonitoredEntity { get; set;}
       
        public DynatraceOutputConfiguration()
        {
            
        }

        public DynatraceOutputConfiguration DeepClone()
        {
            var deepCopy = new DynatraceOutputConfiguration()
            {
                ServiceBaseAddress = this.ServiceBaseAddress,
                ServiceAPIEndpoint = this.ServiceAPIEndpoint,
                APIToken = this.APIToken,
                MonitoredEntity = new MonitoredEntityConfig(this.MonitoredEntity)
            };

            return deepCopy;
        }
    }
}
