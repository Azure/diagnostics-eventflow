// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using log4net.Core;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class Log4netConfiguration : ItemConfiguration
    {
        public string LogLevel { get; set; }

        public Level Log4netLevel { get; set; }
    }
}
