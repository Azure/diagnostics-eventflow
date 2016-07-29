// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class EventSink<EventDataType>: IDisposable
    {
        public EventSink(IEventSender<EventDataType> sender, IEnumerable<IEventFilter<EventDataType>> filters)
        {
            Requires.NotNull(sender, nameof(sender));
            this.Sender = sender;
            this.Filters = filters;
        }

        public IEventSender<EventDataType> Sender { get; private set; }
        public IEnumerable<IEventFilter<EventDataType>> Filters { get; private set; }

        public void Dispose()
        {
            (Sender as IDisposable)?.Dispose();

            if (Filters != null)
            {
                foreach (var f in Filters)
                {
                    (f as IDisposable)?.Dispose();
                }
            }
        }
    }
}
