// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs.DiagnosticSource
{
    internal class DiagnosticListenerObserver : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly IHealthReporter _healthReporter;
        private readonly object _lock = new object();
        private readonly IObserver<EventData> _output;
        private readonly IReadOnlyCollection<DiagnosticSourceConfiguration> _sources;
        private readonly List<IDisposable> _subscriptions;
        private bool _disposed;

        public DiagnosticListenerObserver(IReadOnlyCollection<DiagnosticSourceConfiguration> sources, IObserver<EventData> output, IHealthReporter healthReporter)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _healthReporter = healthReporter ?? throw new ArgumentNullException(nameof(healthReporter));
            _subscriptions = new List<IDisposable>();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;

                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }

                _subscriptions.Clear();
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (_sources.Any(s => string.Equals(s.ProviderName, listener.Name, StringComparison.Ordinal)))
            {
                lock (_lock)
                {
                    if (!_disposed)
                    {
                        _subscriptions.Add(listener.Subscribe(new EventObserver(listener.Name, _output, _healthReporter)));
                    }
                }
            }
        }
    }
}
