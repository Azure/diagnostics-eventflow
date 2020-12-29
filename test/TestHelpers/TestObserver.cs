// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;

namespace Microsoft.Diagnostics.EventFlow.TestHelpers
{
    public class TestObserver : IObserver<EventData>
    {
        public bool Completed { get; private set; } = false;
        public Exception Error { get; private set; }
        public ConcurrentQueue<EventData> Data { get; } = new ConcurrentQueue<EventData>();

        public void OnCompleted()
        {
            Completed = true;
        }

        public void OnError(Exception error)
        {
            Error = error;
        }

        public void OnNext(EventData value)
        {
            Data.Enqueue(value);
        }
    }
}
