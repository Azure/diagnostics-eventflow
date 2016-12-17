// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal class EventFlowLogger : ILogger
    {
        private readonly string categoryName;
        private readonly LoggerInput loggerInput;
        private readonly IHealthReporter healthReporter;

        private Scope<object> scope;

        public EventFlowLogger(string categoryName, LoggerInput loggerInput, IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(categoryName, nameof(categoryName));
            Validation.Requires.NotNull(loggerInput, nameof(loggerInput));
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));

            this.categoryName = categoryName;
            this.loggerInput = loggerInput;
            this.healthReporter = healthReporter;
        }

        public void Log<TState>(Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Validation.Requires.NotNull(formatter, nameof(formatter));

            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);

            Dictionary<string, object> properties = new Dictionary<string, object>();
            if (state is FormattedLogValues)
            {
                var formattedState = state as FormattedLogValues;
                //last KV is the whole message, we will pass it separately
                for (int i = 0; i < formattedState.Count - 1; i++)
                    properties.Add(formattedState[i].Key, formattedState[i].Value);
            }
            if (scope?.State != null)
            {
                var formattedState = scope.State as FormattedLogValues;
                if (formattedState != null)
                {
                    for (int i = 0; i < formattedState.Count - 1; i++)
                    {
                        KeyValuePair<string, object> current = formattedState[i];
                        AddPayloadProperty(properties, current.Key, current.Value);
                    }

                    //last KV is the whole 'scope' message, we will add it formatted
                    AddPayloadProperty(properties, "Scope", formattedState.ToString());
                }
                else
                {
                    AddPayloadProperty(properties, "Scope", scope.State);
                }
            }

            loggerInput.SubmitEventData(message, ToLogLevel(logLevel), eventId, exception, categoryName, properties);
        }

        public bool IsEnabled(Extensions.Logging.LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
                return null;
            scope = new Scope<object>(state);
            return scope;
        }

        private class Scope<TState> : IDisposable
        {
            public TState State { get; private set; }

            public Scope(TState state)
            {
                this.State = state;
            }

            public void Dispose()
            {
                var disposable = State as IDisposable;
                disposable?.Dispose();
                State = default(TState);
            }
        }

        private LogLevel ToLogLevel(Extensions.Logging.LogLevel loggerLevel)
        {
            switch (loggerLevel)
            {
                case Extensions.Logging.LogLevel.Critical:
                    return LogLevel.Critical;
                case Extensions.Logging.LogLevel.Error:
                    return LogLevel.Error;
                case Extensions.Logging.LogLevel.Warning:
                    return LogLevel.Warning;
                case Extensions.Logging.LogLevel.Information:
                    return LogLevel.Informational;
            }
            return LogLevel.Verbose;
        }

        private void AddPayloadProperty(IDictionary<string, object> payload, string key, object value)
        {
            Debug.Assert(payload != null);
            Debug.Assert(!string.IsNullOrEmpty(key));

            PayloadDictionaryUtilities.AddPayloadProperty(payload, key, value, this.healthReporter, nameof(EventFlowLogger));
        }
    }
}
