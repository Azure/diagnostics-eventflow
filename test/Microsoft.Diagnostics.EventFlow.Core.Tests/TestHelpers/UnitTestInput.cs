// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    internal class UnitTestInput : IObservable<EventData>, IDisposable
    {
        private SimpleSubject<EventData> subject;
        public UnitTestInput()
        {
            subject = new SimpleSubject<EventData>();
        }

        public void Dispose()
        {
            if (subject != null)
            {
                this.subject.Dispose();
                this.subject = null;
            }
        }

        public void SendMessage(string message)
        {
            var e = new EventData();
            e.Payload["Message"] = message;
            subject.OnNext(e);
        }

        public void SendData(IEnumerable<KeyValuePair<string, object>> data)
        {
            var e = new EventData();
            foreach (var kvPair in data)
            {
                e.Payload.Add(kvPair.Key, kvPair.Value);
            }
            subject.OnNext(e);
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }
    }

    internal class UnitTestInputFactory : IPipelineItemFactory<UnitTestInput>
    {
        public UnitTestInput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            return new UnitTestInput();
        }
    }
}
