// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.EventFlow.Consumers.SimpleBenchmark
{
    [EventSource(Name = "Microsoft-AzureTools-BenchmarkEventSource")]
    internal class BenchmarkEventSource: EventSource
    {
        public static BenchmarkEventSource Log = new BenchmarkEventSource();

        [Event(1, Level = EventLevel.Informational, Message = "ComplexMessage event {2}")]
        public void ComplexMessage(Guid guid, Guid guid2, long sequenceNumber, string name, DateTime date, DateTime date2, string additionalInfo)
        {
            if (this.IsEnabled())
            {
                WriteEvent(1, guid, guid2, sequenceNumber, name, date, date2, additionalInfo);
            }
        }
    }
}
