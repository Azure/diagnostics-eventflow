// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class TcpOutputConfiguration: ItemConfiguration
    {
        public static readonly string DefaultFormat = "json";

        public string ServiceHost { get; set; }
        public int ServicePort { get; set; }
        public string Format { get; set; }

        public TcpOutputConfiguration()
        {
            Format = DefaultFormat;
        }

        public TcpOutputConfiguration DeepClone()
        {
            var other = new TcpOutputConfiguration()
            {
                ServiceHost = this.ServiceHost,
                ServicePort = this.ServicePort,
                Format = this.Format
            };

            return other;
        }
    }
}
