// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class UdpOutputConfiguration: ItemConfiguration
    {
        public static readonly string DefaultFormat = "json";

        public string ServiceHost { get; set; }
        public int ServicePort { get; set; }
        public string Format { get; set; }

        public UdpOutputConfiguration()
        {
            Format = DefaultFormat;
        }

        public UdpOutputConfiguration DeepClone()
        {
            var other = new UdpOutputConfiguration()
            {
                ServiceHost = this.ServiceHost,
                ServicePort = this.ServicePort,
                Format = this.Format
            };

            return other;
        }
    }
}
