// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Events;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class SerilogInput : IObserver<LogEvent>, IObservable<EventData>, IDisposable
    {
        private static readonly IDictionary<LogEventLevel, LogLevel> ToLogLevel =
            new Dictionary<LogEventLevel, LogLevel>
            {
                [LogEventLevel.Verbose] = LogLevel.Verbose,
                [LogEventLevel.Debug] = LogLevel.Verbose,
                [LogEventLevel.Information] = LogLevel.Informational,
                [LogEventLevel.Warning] = LogLevel.Warning,
                [LogEventLevel.Error] = LogLevel.Error,
                [LogEventLevel.Fatal] = LogLevel.Critical
            };

        private EventFlowSubject<EventData> subject;
        private readonly IHealthReporter healthReporter;

        public SerilogInput(IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.subject = new EventFlowSubject<EventData>();
        }

        void IObserver<LogEvent>.OnCompleted()
        {
            this.subject.OnCompleted();
        }

        void IObserver<LogEvent>.OnError(Exception error)
        {
            this.subject.OnError(error);
        }

        void IObserver<LogEvent>.OnNext(LogEvent value)
        {
            if (value == null)
            {
                return;
            }

            EventData e = ToEventData(value);
            this.subject.OnNext(e);
        }
        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }

        public virtual void Dispose()
        {
            this.subject.Dispose();
        }

        private EventData ToEventData(LogEvent logEvent)
        {
            EventData eventData = new EventData
            {
                ProviderName = nameof(SerilogInput),
                Timestamp = logEvent.Timestamp,
                Level = ToLogLevel[logEvent.Level],
                Keywords = 0
            };

            var payload = eventData.Payload;
            foreach (var property in logEvent.Properties.Where(property => property.Value != null))
            {
                try
                {
                    payload[property.Key] = ToRawValue(property.Value);
                }
                catch (Exception e)
                {
                    healthReporter.ReportWarning($"{nameof(SerilogInput)}: event property '{property.Key}' could not be rendered{Environment.NewLine}{e.ToString()}");
                }
            }

            if (logEvent.Exception != null)
            {
                eventData.AddPayloadProperty("Exception", logEvent.Exception, healthReporter, nameof(SerilogInput));
            }

            // Inability to render the message, or any other LogEvent property, should not stop us from sending the event down the pipeline
            try
            {
                eventData.AddPayloadProperty("Message", logEvent.RenderMessage(), healthReporter, nameof(SerilogInput));
            }
            catch (Exception e)
            {
                healthReporter.ReportWarning($"{nameof(SerilogInput)}: event message could not be rendered{Environment.NewLine}{e.ToString()}");
            }

            return eventData;
        }

        private static object ToRawValue(LogEventPropertyValue logEventValue)
        {
            // Special-case a few types of LogEventPropertyValue that allow us to maintain better type fidelity.
            // For everything else take the default string rendering as the data.
            ScalarValue scalarValue = logEventValue as ScalarValue;
            if (scalarValue != null)
            {
                return scalarValue.Value;
            }

            SequenceValue sequenceValue = logEventValue as SequenceValue;
            if (sequenceValue != null)
            {
                object[] arrayResult = sequenceValue.Elements.Select(e => ToRawScalar(e)).ToArray();
                if (arrayResult.Length == sequenceValue.Elements.Count)
                {
                    // All values extracted successfully, it is a flat array of scalars
                    return arrayResult;
                }
            }

            StructureValue structureValue = logEventValue as StructureValue;
            if (structureValue != null)
            {
                IDictionary<string, object> structureResult = new Dictionary<string, object>(structureValue.Properties.Count);
                foreach (var property in structureValue.Properties)
                {
                    structureResult[property.Name] = ToRawScalar(property.Value);
                }

                if (structureResult.Count == structureValue.Properties.Count)
                {
                    if (structureValue.TypeTag != null)
                    {
                        structureResult["$type"] = structureValue.TypeTag;
                    }

                    return structureResult;
                }
            }

            DictionaryValue dictionaryValue = logEventValue as DictionaryValue;
            if (dictionaryValue != null)
            {
                IDictionary<string, object> dictionaryResult = dictionaryValue.Elements
                    .Where(kvPair => kvPair.Key.Value is string)
                    .ToDictionary(kvPair => (string)kvPair.Key.Value, kvPair => ToRawScalar(kvPair.Value));
                if (dictionaryResult.Count == dictionaryValue.Elements.Count)
                {
                    return dictionaryResult;
                }
            }

            // Fall back to string rendering of the value
            return logEventValue.ToString();
        }

        private static object ToRawScalar(LogEventPropertyValue value)
        {
            ScalarValue scalarValue = value as ScalarValue;
            if (scalarValue != null)
            {
                return scalarValue.Value;
            }

            return value.ToString();
        }
    }
}
