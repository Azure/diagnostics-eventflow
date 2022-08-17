// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal class EventFlowLogger : ILogger
    {
        private readonly string categoryName;
        private readonly LoggerInput loggerInput;
        private readonly IHealthReporter healthReporter;
        private readonly ConcurrentBag<Stack<string>> stackPool;

        public EventFlowLogger(string categoryName, LoggerInput loggerInput, IHealthReporter healthReporter)
        {
            Validation.Requires.NotNull(categoryName, nameof(categoryName));
            Validation.Requires.NotNull(loggerInput, nameof(loggerInput));
            Validation.Requires.NotNull(healthReporter, nameof(healthReporter));

            this.categoryName = categoryName;
            this.loggerInput = loggerInput;
            this.healthReporter = healthReporter;
            this.stackPool = new ConcurrentBag<Stack<string>>();
        }

        public void Log<TState>(Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Validation.Requires.NotNull(formatter, nameof(formatter));

            if (!IsEnabled(logLevel))
                return;

            Dictionary<string, object> properties = new Dictionary<string, object>();

            ApplyOmittingUnformattedMessage(state, item => properties[item.Key] = item.Value);

            var scope = EventFlowLoggerScope.Current;
            if (scope != null)
            {
                Stack<string> scopeValueStack;
                if (!this.stackPool.TryTake(out scopeValueStack))
                {
                    scopeValueStack = new Stack<string>();
                }

                while (scope != null)
                {
                    object scopeState = scope.State;
                    if (scopeState != null)
                    {
                        // Scope data may or may not have a formatted message
                        bool unformattedMessageOmitted = ApplyOmittingUnformattedMessage(scopeState, item => AddPayloadProperty(properties, item.Key, item.Value));
                        if (unformattedMessageOmitted)
                        {
                            scopeValueStack.Push(scopeState.ToString());
                        }
                    }

                    scope = scope.Parent;
                }

                if (scopeValueStack.Count > 0)
                {
                    AddPayloadProperty(properties, "Scope", string.Join("\r\n", scopeValueStack));
                }

                scopeValueStack.Clear();
                this.stackPool.Add(scopeValueStack);
            }

            var message = formatter(state, exception);
            loggerInput.SubmitEventData(message, ToLogLevel(logLevel), eventId, exception, categoryName, properties);
        }

        public bool IsEnabled(Extensions.Logging.LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return EventFlowLoggerScope.Push(state);
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

        private bool ApplyOmittingUnformattedMessage(object src, System.Action<KeyValuePair<string, object>> action)
        {
            // Special case the "{OriginalFormat}" data key that is used to preserve the message before formatting was applied
            // This data is omitted from the payload and replaced by formatted message.
            // See https://github.com/aspnet/Logging/blob/master/src/Microsoft.Extensions.Logging.Abstractions/Internal/LogValuesFormatter.cs
            const string OriginalFormatKey = "{OriginalFormat}";

            // If the data is not structured, it will be omitted
            bool unformattedMessageOmitted = true;

            if (src is IEnumerable<KeyValuePair<string, object>> structuredSrc)
            {
                unformattedMessageOmitted = false;
                foreach(KeyValuePair<string, object> item in structuredSrc)
                {
                    if (!OriginalFormatKey.Equals(item.Key, StringComparison.Ordinal))
                    {
                        action(item);
                    }
                    else
                    {
                        unformattedMessageOmitted = true;
                    }
                }
            }

            return unformattedMessageOmitted;
        }

        private void AddPayloadProperty(IDictionary<string, object> payload, string key, object value)
        {
            Debug.Assert(payload != null);
            Debug.Assert(!string.IsNullOrEmpty(key));

            PayloadDictionaryUtilities.AddPayloadProperty(payload, key, value, this.healthReporter, nameof(EventFlowLogger));
        }
    }
}
