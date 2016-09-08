// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow
{

    public enum LogLevel
    {
        Critical = 1,
        Error = 2,
        Warning = 3,
        Informational = 4,
        Verbose = 5
    }

    public static class EventLevelExtensions
    {
        // Micro-optimization: Enum.ToString() uses type information and does a binary search for the value,
        // which is kind of slow. We are going to to the conversion manually instead.
        private static readonly string[] LogLevelNames = new string[]
        {
            "NotUsed",
            "Critical",
            "Error",
            "Warning",
            "Informational",
            "Verbose"
        };

        public static string GetName(this LogLevel level)
        {
            return LogLevelNames[(int) level];
        }
    }
}
