using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs.DiagnosticSource
{
    internal sealed class DiagnosticListenerObserver : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly IHealthReporter _healthReporter;
        private readonly object _lock = new object();
        private readonly IObserver<EventData> _output;
        private readonly IReadOnlyCollection<DiagnosticSourceConfiguration> _sources;
        private readonly List<IDisposable> _subscriptions;

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
                    _subscriptions.Add(listener.Subscribe(new EventObserver(listener.Name, _output, _healthReporter)));
                }
            }
        }
    }
}