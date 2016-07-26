// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventListeners
{
    public class ObservableEventListener : IObservable<EventData>
    {
        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            throw new NotImplementedException();
        }
    }
}
