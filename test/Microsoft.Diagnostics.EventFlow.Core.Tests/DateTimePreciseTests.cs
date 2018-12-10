// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#if !NETCOREAPP1_0
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class DateTimePreciseTests
    {
        private static double GetAverageResolution()
        {
            const int count = 1000;
            var precisions = new List<double>(count);
            for (var i = 0; i < count; ++i)
            {
                precisions.Add(GetResolution().TotalMilliseconds);
            }
            return precisions.Average();
        }

        private static TimeSpan GetResolution()
        {
            var t1 = DateTimePrecise.UtcNow;
            DateTime t2;
            while ((t2 = DateTimePrecise.UtcNow) == t1)
            {
                // Spin until the time changes
            }
            return t2 - t1;
        }

        [Fact]
        public void DateTimeIsHighPrecision()
        {
            if (!(Environment.OSVersion.Platform == PlatformID.Win32NT
               && (Environment.OSVersion.Version.Major >= 10 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 2))))
            {
                // We only use explicit high-precision timestamping on Windows, relying on .NET implementation defaults for other platforms
                return;
            }

            // We should be able to use 0.001 here. It works from a command line application but not the test runner.
            const double expectedMs = 1.1;

            var actualMs = GetAverageResolution();

            Assert.True(actualMs <= expectedMs, $"Expected a higher resolution than {expectedMs}ms but got {actualMs:0.000}ms");
        }
    }
}
#endif
