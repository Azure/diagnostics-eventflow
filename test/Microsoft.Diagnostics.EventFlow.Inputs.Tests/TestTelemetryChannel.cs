// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#if NET46

using System;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class TestTelemetryChannel : ITelemetryChannel
    {
        public bool? DeveloperMode { get => false; set {} }
        public string EndpointAddress { get; set; }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        public void Send(ITelemetry item)
        {
        }
    }
}

#endif
