// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    [EventSource(Name ="UnitTest EventSource")]
    public class UnitTestEventSource : EventSource
    {
        public static UnitTestEventSource Log = new UnitTestEventSource();

        [Event(1, Level = EventLevel.Informational)]
        public void SendInformation(string msg)
        {
            WriteEvent(1, msg);
        }

        [Event(2, Level = EventLevel.Warning)]
        public void SendWarning(string message)
        {
            WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Error)]
        public void SendError(string message)
        {
            WriteEvent(3, message);
        }
    }
}
