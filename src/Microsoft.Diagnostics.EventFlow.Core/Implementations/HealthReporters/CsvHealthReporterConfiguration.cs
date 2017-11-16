// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.HealthReporters
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class CsvHealthReporterConfiguration
    {
        public string LogFileFolder { get; set; }
        public string LogFilePrefix { get; set; }
        public string MinReportLevel { get; set; }
        public int? ThrottlingPeriodMsec { get; set; }
        public int SingleLogFileMaximumSizeInMBytes { get; set; }
        public int LogRetentionInDays { get; set; } 
    }
}
