// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Validation;

using Serilog.Core;
using Serilog.Events;

using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    /// <summary>
    /// An input that supplies events from the Serilog structured logging library.
    /// </summary>
    public class SerilogInput : ILogEventSink, IObservable<EventData>, IDisposable
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
        private IHealthReporter healthReporter;
        internal SerilogInputConfiguration inputConfiguration;

        /// <summary>
        /// Creates an instance of <see cref="SerilogInput"/> using default values
        /// </summary>
        /// <param name="healthReporter">A health reporter through which the input can report errors.</param>
        public SerilogInput(IHealthReporter healthReporter) : this(new SerilogInputConfiguration(), healthReporter) { }


        /// <summary>
        /// Construct a <see cref="SerilogInput"/>.
        /// </summary>
        /// <param name="configuration">A configuration to be used to configure the input</param>
        /// <param name="healthReporter">A health reporter through which the input can report errors.</param>
        public SerilogInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            var inputConfiguration = new SerilogInputConfiguration();
            try
            {
                configuration.Bind(inputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(SerilogInputConfiguration)} configuration encountered: '{configuration}'",
                   EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(inputConfiguration, healthReporter);
        }

        /// <summary>
        /// Creates an instance of <see cref="SerilogInput"/>
        /// </summary>
        /// <param name="inputConfiguration">A configuration to be used to configure the input</param>
        /// <param name="healthReporter">A health reporter through which the input can report errors.</param>
        public SerilogInput(SerilogInputConfiguration inputConfiguration, IHealthReporter healthReporter)
        {
            Requires.NotNull(inputConfiguration, nameof(inputConfiguration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            Initialize(inputConfiguration, healthReporter);
        }

        private void Initialize(SerilogInputConfiguration inputConfiguration, IHealthReporter healthReporter)
        {
            this.healthReporter = healthReporter;
            this.inputConfiguration = inputConfiguration;
            this.subject = new EventFlowSubject<EventData>();
        }

        void ILogEventSink.Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                return;
            }

            EventData e = ToEventData(logEvent);
            this.subject.OnNext(e);
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }

        /// <inheritdoc/>
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

            // Prefer the built-in `Message` and `Exception` properties by adding them to the payload
            // first. If other attached data items have conflicting names, they will be added as
            // `Message_1` and so-on.
            if (logEvent.Exception != null)
            {

                if (logEvent.Level >= LogEventLevel.Error) 
                {
                    EventMetadata eventMetadata = new EventMetadata(ExceptionData.ExceptionMetadataKind);
                    eventMetadata.Properties.Add(ExceptionData.ExceptionPropertyMoniker, "Exception");
                    eventData.SetMetadata(eventMetadata);
                }
                
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

            // MessageTemplate is always present on Serilog events
            eventData.AddPayloadProperty("MessageTemplate", logEvent.MessageTemplate.Text, healthReporter, nameof(SerilogInput));

            foreach (var property in logEvent.Properties.Where(property => property.Value != null))
            {
                try
                {
                    eventData.AddPayloadProperty(property.Key, ToRawValue(property.Value), healthReporter, nameof(SerilogInput));
                }
                catch (Exception e)
                {
                    healthReporter.ReportWarning($"{nameof(SerilogInput)}: event property '{property.Key}' could not be rendered{Environment.NewLine}{e.ToString()}");
                }
            }

            return eventData;
        }

        private object ToRawValue(LogEventPropertyValue logEventValue)
        {
            // Special-case a few types of LogEventPropertyValue that allow us to maintain better type fidelity.
            // For everything else take the default string rendering as the data.
            ScalarValue scalarValue = logEventValue as ScalarValue;
            if (scalarValue != null)
            {
                return scalarValue.Value;
            }

            Func<LogEventPropertyValue, object> valueSerializer = this.inputConfiguration.IgnoreSerilogDepthLevel ? 
                (Func<LogEventPropertyValue, object>) this.ToRawScalar : this.ToRawValue;

            SequenceValue sequenceValue = logEventValue as SequenceValue;
            if (sequenceValue != null)
            {
                object[] arrayResult = sequenceValue.Elements.Select(e => valueSerializer(e)).ToArray();
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
                    structureResult[property.Name] = valueSerializer(property.Value);
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
                    .ToDictionary(kvPair => (string)kvPair.Key.Value, kvPair => valueSerializer(kvPair.Value));
                if (dictionaryResult.Count == dictionaryValue.Elements.Count)
                {
                    return dictionaryResult;
                }
            }

            // Fall back to string rendering of the value
            return logEventValue.ToString();
        }

        private object ToRawScalar(LogEventPropertyValue value)
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
