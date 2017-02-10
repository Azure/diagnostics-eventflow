// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public interface ITraceEventSession: IDisposable
    {
        void EnableProvider(Guid providerGuid, TraceEventLevel maximumEventLevel, ulong enabledKeywords);
        void EnableProvider(string providerName, TraceEventLevel maximumEventLevel, ulong enabledKeywordsOptions);
        void Process(Action<EventData> onEvent);
    }
}
