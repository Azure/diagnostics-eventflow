// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs.DiagnosticSource
{
    public class DiagnosticSourceInput : IObservable<EventData>, IDisposable
    {
        private readonly DiagnosticListenerObserver _listenerObserver;
        private readonly IDisposable _listenerSubscription;
        private readonly EventFlowSubject<EventData> _subject;

        public DiagnosticSourceInput(IReadOnlyCollection<DiagnosticSourceConfiguration> sources, IHealthReporter healthReporter)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            if (healthReporter == null)
            {
                throw new ArgumentNullException(nameof(healthReporter));
            }

            _subject = new EventFlowSubject<EventData>();
            _listenerObserver = new DiagnosticListenerObserver(sources, _subject, healthReporter);
            _listenerSubscription = DiagnosticListener.AllListeners.Subscribe(_listenerObserver);
        }

        public void Dispose()
        {
            _listenerSubscription.Dispose();
            _listenerObserver.Dispose();
            _subject.Dispose();
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return _subject.Subscribe(observer);
        }
    }
}
