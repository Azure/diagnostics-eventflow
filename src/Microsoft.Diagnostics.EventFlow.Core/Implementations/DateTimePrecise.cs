// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
#if NET462 || NET471
using System.Runtime.InteropServices;
#endif

namespace Microsoft.Diagnostics.EventFlow
{
    public static class DateTimePrecise
    {
#if NET462 || NET471
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        private static readonly bool UseSystemTimePrecise =
            Environment.OSVersion.Platform == PlatformID.Win32NT
         && (Environment.OSVersion.Version.Major >= 10 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 2));
#endif

        public static DateTime UtcNow
        {
            get
            {
#if NET462 || NET471
                if (UseSystemTimePrecise)
                {
                    GetSystemTimePreciseAsFileTime(out var filetime);
                    return DateTime.FromFileTimeUtc(filetime);
                }
#endif
                return DateTime.UtcNow;
            }
        }
    }
}
