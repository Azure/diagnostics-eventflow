// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Diagnostics.EventFlow.HealthReporters;

namespace Microsoft.Diagnostics.EventFlow
{
    internal class UtcMidnightNotifier : INewReportFileTrigger
    {
        private Timer timer;

        public event EventHandler<EventArgs> DayChanged;
        public event EventHandler<EventArgs> NewReportFileRequested;

        private static Lazy<UtcMidnightNotifier> instance = new Lazy<UtcMidnightNotifier>(() =>
        {
            return new UtcMidnightNotifier();
        });

        public static UtcMidnightNotifier Instance
        {
            get
            {
                return instance.Value;
            }
        }

        private UtcMidnightNotifier()
        {
            CreateNewTimer();
        }

        private void CreateNewTimer()
        {
            timer = new Timer(state =>
            {
                OnDayChanged();
            }, null, GetSleepTime(), TimeSpan.FromHours(24));
        }

        private static TimeSpan GetSleepTime()
        {
            DateTime midnightTonight = DateTime.UtcNow.Date.AddDays(1);
            TimeSpan difference = (midnightTonight - DateTime.UtcNow);
            return difference;
        }

        private void OnDayChanged()
        {
            DayChanged?.Invoke(this, EventArgs.Empty);
            NewReportFileRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
