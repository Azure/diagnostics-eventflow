// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading;
#if NET451
using Microsoft.Win32;
#endif

namespace Microsoft.Diagnostics.EventFlow.Core.Implementations
{
    internal static class UtcMidnightNotifier
    {
        private static Timer timer;
        public static event EventHandler<EventArgs> DayChanged;

        static UtcMidnightNotifier()
        {
            CreateNewTimer();

            // TODO: Find an event to subscribe for .NET Core application when system time is changed.
#if NET451
            SystemEvents.TimeChanged += OnSystemTimeChanged;
#endif
        }

        private static void CreateNewTimer()
        {
            timer = new Timer(state =>
            {
                OnDayChanged();
            }, null, GetSleepTime(), TimeSpan.FromHours(24));
        }

        private static TimeSpan GetSleepTime()
        {
            DateTime midnightTonight = DateTime.UtcNow.Date.AddDays(1);
            TimeSpan difference = (midnightTonight - DateTime.Now);
            return difference;
        }

        private static void OnDayChanged()
        {
            DayChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void OnSystemTimeChanged(object sender, EventArgs e)
        {
            CreateNewTimer();
        }
    }
}
