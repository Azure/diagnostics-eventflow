// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Diagnostics.EventFlow.Metadata;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    [Target(NLogTargetTypeName)]
    public class NLogInput : TargetWithContext, IObservable<EventData>
    {
        internal const string NLogTargetTypeName = "EventFlowInput";

        private static readonly IDictionary<NLog.LogLevel, LogLevel> ToLogLevel = new Dictionary<NLog.LogLevel, LogLevel>
        {
            [NLog.LogLevel.Trace] = LogLevel.Verbose,
            [NLog.LogLevel.Debug] = LogLevel.Verbose,
            [NLog.LogLevel.Info] = LogLevel.Informational,
            [NLog.LogLevel.Warn] = LogLevel.Warning,
            [NLog.LogLevel.Error] = LogLevel.Error,
            [NLog.LogLevel.Fatal] = LogLevel.Critical
        };

        private readonly EventFlowSubject<EventData> _eventFlowSubject;
        private readonly IHealthReporter _healthReporter;

        /// <inheritdoc/>
        public override IList<TargetPropertyWithContext> ContextProperties { get; } = new List<TargetPropertyWithContext>();

        public Layout ProviderName { get; set; }

        public NLogInput(IHealthReporter healthReporter)
        {
            _healthReporter = healthReporter;
            OptimizeBufferReuse = true;
            IncludeEventProperties = true;
            Layout = "${message}";

            _eventFlowSubject = new EventFlowSubject<EventData>();
        }

        protected override void CloseTarget()
        {
            _eventFlowSubject.Dispose();
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return _eventFlowSubject.Subscribe(observer);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            EventData e = ToEventData(logEvent);
            _eventFlowSubject.OnNext(e);
        }

        private EventData ToEventData(LogEventInfo logEvent)
        {
            var providerName = string.Empty;
            try
            {
                providerName = ProviderName?.Render(logEvent);
            }
            catch (Exception ex)
            {
                NLog.Common.InternalLogger.Warn(ex, NLogTargetTypeName + "(Name={0}): Failed to render event providername", Name);
                _healthReporter.ReportWarning($"{nameof(NLogInput)}: event providername could not be rendered{Environment.NewLine}{ex.ToString()}");
            }
            
            if (string.IsNullOrEmpty(providerName))
                    providerName = string.IsNullOrEmpty(logEvent.LoggerName) ? nameof(NLogInput) : logEvent.LoggerName;

            EventData eventData = new EventData
            {
                ProviderName = providerName,
                Timestamp = logEvent.TimeStamp.ToUniversalTime(),
                Level = ToLogLevel[logEvent.Level],
                Keywords = 0,
            };

            var payload = eventData.Payload;

            // Prefer the built-in `Message` and `Exception` properties by adding them to the payload
            // first. If other attached data items have conflicting names, they will be added as
            // `Message_1` and so-on.
            if (logEvent.Exception != null)
            {
                if (logEvent.Level.Ordinal >= NLog.LogLevel.Error.Ordinal)
                {
                    EventMetadata eventMetadata = new EventMetadata(ExceptionData.ExceptionMetadataKind);
                    eventMetadata.Properties.Add(ExceptionData.ExceptionPropertyMoniker, "Exception");
                    eventData.SetMetadata(eventMetadata);
                }

                eventData.AddPayloadProperty("Exception", logEvent.Exception, _healthReporter, nameof(NLogInput));
            }

            // Inability to render the message, or any other LogEvent property, should not stop us from sending the event down the pipeline
            try
            {
                eventData.AddPayloadProperty("Message", RenderLogEvent(Layout, logEvent), _healthReporter, nameof(NLogInput));
            }
            catch (Exception ex)
            {
                NLog.Common.InternalLogger.Warn(ex, NLogTargetTypeName + "(Name={0}): Failed to render event message", Name);
                _healthReporter.ReportWarning($"{nameof(NLogInput)}: event message could not be rendered{Environment.NewLine}{ex.ToString()}");
                eventData.AddPayloadProperty("Message", logEvent.FormattedMessage, _healthReporter, nameof(NLogInput));
            }

            if (ContextProperties.Count > 0)
            {
                // Include fixed properties like ThreadId, HostName, MessageTemplate, etc.
                CaptureTargetContextEventData(logEvent, eventData);
            }

            if (IncludeEventProperties && logEvent.HasProperties)
            {
                // Include properties coming from LogEvent
                CaptureLogEventPropertiesEventData(logEvent.Properties, eventData);
            }

            if (IncludeMdc || IncludeMdlc)
            {
                // Include any random bonus information
                CaptureLogEventContextProperties(GetContextProperties(logEvent), eventData);
            }

            return eventData;
        }

        private void CaptureLogEventContextProperties(IDictionary<string, object> properties, EventData eventData)
        {
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    try
                    {
                        eventData.AddPayloadProperty(property.Key, property.Value, _healthReporter, nameof(NLogInput));
                    }
                    catch (Exception ex)
                    {
                        NLog.Common.InternalLogger.Warn(ex, NLogTargetTypeName + "(Name={0}): Failed to render event context property: {1}", Name, property.Key);
                        _healthReporter.ReportWarning($"{nameof(NLogInput)}: event context property '{property.Key}' could not be rendered{Environment.NewLine}{ex.ToString()}");
                    }
                }
            }
        }

        private void CaptureLogEventPropertiesEventData(IDictionary<object, object> properties, EventData eventData)
        {
            foreach (var property in properties)
            {
                string propertyKey = null;

                try
                {
                    propertyKey = property.Key?.ToString();
                    if (string.IsNullOrEmpty(propertyKey))
                        continue;

                    eventData.AddPayloadProperty(propertyKey, ToRawValue(property.Value), _healthReporter, nameof(NLogInput));
                }
                catch (Exception ex)
                {
                    NLog.Common.InternalLogger.Warn(ex, NLogTargetTypeName + "(Name={0}): Failed to render event property: {1}", Name, propertyKey);
                    _healthReporter.ReportWarning($"{nameof(NLogInput)}: event property '{propertyKey}' could not be rendered{Environment.NewLine}{ex.ToString()}");
                }
            }
        }

        private void CaptureTargetContextEventData(LogEventInfo logEvent, EventData eventData)
        {
            for (int i = 0; i < ContextProperties.Count; ++i)
            {
                var property = ContextProperties[i];

                try
                {
                    eventData.AddPayloadProperty(property.Name, RenderLogEvent(property.Layout, logEvent), _healthReporter, nameof(NLogInput));
                }
                catch (Exception ex)
                {
                    NLog.Common.InternalLogger.Warn(ex, NLogTargetTypeName + "(Name={0}): Failed to render event target property: {1}", Name, property.Name);
                    _healthReporter.ReportWarning($"{nameof(NLogInput)}: event target property '{property.Name}' could not be rendered{Environment.NewLine}{ex.ToString()}");
                }
            }
        }

        protected override bool SerializeItemValue(LogEventInfo logEvent, string name, object value, out object serializedValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                serializedValue = null;
                return false;
            }

            try
            {
                serializedValue = ToRawValue(value);
                return true;
            }
            catch (Exception ex)
            {
                NLog.Common.InternalLogger.Warn(ex, NLogTargetTypeName + "(Name={0}): Failed to render event property: {1}", Name, name);
                _healthReporter.ReportWarning($"{nameof(NLogInput)}: event property '{name}' could not be rendered{Environment.NewLine}{ex.ToString()}");
                serializedValue = null;
                return false;
            }
        }

        private static object ToRawValue(object logEventValue)
        {
            if (Convert.GetTypeCode(logEventValue) != TypeCode.Object)
            {
                return logEventValue;
            }

            IDictionary dictionaryValue = logEventValue as IDictionary;
            if (dictionaryValue != null)
            {
                IDictionary<string, object> dictionaryResult = new Dictionary<string, object>(dictionaryValue.Count);
                foreach (DictionaryEntry item in dictionaryValue)
                {
                    var itemKey = item.Key?.ToString();
                    if (string.IsNullOrEmpty(itemKey))
                        continue;

                    dictionaryResult[itemKey] = ToRawSimpleValue(item.Value);
                }
            }

            IEnumerable sequenceValue = logEventValue as IEnumerable;
            if (sequenceValue != null)
            {
                List<object> sequenceResult = null;
                foreach (var item in sequenceValue)
                {
                    sequenceResult = sequenceResult ?? new List<object>();
                    sequenceResult.Add(ToRawSimpleValue(item));
                }
                return sequenceResult?.ToArray() ?? new object[0];
            }

            // Fall back to string rendering of the value
            return logEventValue.ToString();
        }

        private static object ToRawSimpleValue(object item)
        {
            return Convert.GetTypeCode(item) != TypeCode.Object ? item : item.ToString();
        }
    }
}
