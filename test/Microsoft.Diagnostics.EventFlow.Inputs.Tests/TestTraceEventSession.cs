// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
#if NET46
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Diagnostics.EventFlow.Configuration;
    using Microsoft.Diagnostics.Tracing;

    internal class TestTraceEventSession : ITraceEventSession
    {
        public static readonly string TestEtwProviderName = "Microsoft-EventFlow-EtwInputTests";

        private List<EtwProviderConfiguration> providers = new List<EtwProviderConfiguration>();
        private ManualResetEvent disposedEvent = new ManualResetEvent(false);
        private Action<EventData> onEvent;

        public ManualResetEvent ProcessingStarted = new ManualResetEvent(false);

        public bool IsDisposed
        {
            get { return this.disposedEvent == null; }
        }

        public void EnableProvider(Guid providerGuid, TraceEventLevel maximumEventLevel, ulong enabledKeywords)
        {
            providers.Add(new EtwProviderConfiguration
            {
                ProviderGuid = providerGuid,
                Level = maximumEventLevel,
                Keywords = (TraceEventKeyword) enabledKeywords
            });
        }

        public void EnableProvider(string providerName, TraceEventLevel maximumEventLevel, ulong enabledKeywords)
        {
            providers.Add(new EtwProviderConfiguration
            {
                ProviderName = providerName,
                Level = maximumEventLevel,
                Keywords = (TraceEventKeyword)enabledKeywords
            });
        }

        public void Process(Action<EventData> onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            this.onEvent = onEvent;
            this.ProcessingStarted.Set();

            // Using non-zero, but fairly long timeout to ensure that a bad test calling this method will eventually terminate.
            this.disposedEvent.WaitOne(TimeSpan.FromMinutes(5));
        }

        public void Dispose()
        {
            if (this.disposedEvent != null)
            {
                var e = this.disposedEvent;
                this.disposedEvent = null;
                e.Set();
                e.Dispose();
                this.ProcessingStarted.Dispose();
            }
        }

        public void ReportEvent(EventData eventData)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(TestTraceEventSession));
            }

            var provider = this.providers.SingleOrDefault(p => p.ProviderName == eventData.ProviderName);
            if (provider == null)
            {
                // We are not tracking this provider
                return;
            }

            if (eventData.Level > (LogLevel) provider.Level && provider.Level != TraceEventLevel.Always)
            {
                // Event too verbose
                return;
            }

            if (eventData.Keywords != 0 && (eventData.Keywords & (long) provider.Keywords) == 0)
            {
                // Keywords do not match
                return;
            }

            this.onEvent?.Invoke(eventData);
        }

        public void ReportEvent(LogLevel level, long keywords, string message)
        {
            EventData e = new EventData
            {
                Timestamp = DateTimeOffset.UtcNow,
                ProviderName = TestEtwProviderName,
                Level = level,
                Keywords = keywords
            };

            if (!string.IsNullOrEmpty(message))
            {
                e.Payload["Message"] = message;
            }

            this.ReportEvent(e);
        }
    }
#endif
}
