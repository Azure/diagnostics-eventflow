// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using log4net.Core;
using System;
using System.Collections.Generic;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;
using Validation;
using log4net.Config;
using System.Linq;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class Log4netInput : AppenderSkeleton, IObservable<EventData>, IDisposable
    {
        private static readonly IDictionary<Level, LogLevel> ToLogLevel =
            new Dictionary<Level, LogLevel>
            {
                [Level.Verbose] = LogLevel.Verbose,
                [Level.Debug] = LogLevel.Verbose,
                [Level.Info] = LogLevel.Informational,
                [Level.Warn] = LogLevel.Warning,
                [Level.Error] = LogLevel.Error,
                [Level.Fatal] = LogLevel.Critical
            };

        private IHealthReporter healthReporter;
        private EventFlowSubject<EventData> subject;
        private Log4netConfiguration _log4NetInputConfiguration;
        private Hierarchy eventFlowRepo;

        public Log4netInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            Requires.NotNull(configuration, nameof(configuration));

            var log4NetInputConfiguration = new Log4netConfiguration();
            try
            {
                configuration.Bind(log4NetInputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(log4NetInputConfiguration)} configuration encountered: '{configuration}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(log4NetInputConfiguration, healthReporter);
        }

        public Log4netInput(Log4netConfiguration log4NetConfigs, IHealthReporter healthReporter)
        {
            Requires.NotNull(log4NetConfigs, nameof(log4NetConfigs));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            Initialize(log4NetConfigs, healthReporter);
        }

        private void Initialize(Log4netConfiguration myLog4NetConfig, IHealthReporter myHealthReporter)
        {
            this.healthReporter = myHealthReporter;
            this._log4NetInputConfiguration = myLog4NetConfig;
            this.subject = new EventFlowSubject<EventData>();

            //Can we determine if the repo exists without try/catch
            try
            {
                eventFlowRepo = (Hierarchy)LogManager.CreateRepository("EventFlowRepo");
                _log4NetInputConfiguration.Log4netLevel = eventFlowRepo.LevelMap[_log4NetInputConfiguration.LogLevel];

                eventFlowRepo.Root.AddAppender(this);
                eventFlowRepo.Configured = true;
                BasicConfigurator.Configure(eventFlowRepo);
            }
            catch (LogException)
            {
                eventFlowRepo = (Hierarchy) LogManager.GetRepository("EventFlowRepo");
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null || !IsEnabledFor(loggingEvent.Level))
            {
                return;
            }

            var eventData = ToEventData(loggingEvent);
            this.subject.OnNext(eventData);
        }

        private bool IsEnabledFor(Level level) => level >= _log4NetInputConfiguration.Log4netLevel;

        private EventData ToEventData(LoggingEvent loggingEvent)
        {
            var eventData = new EventData
            {
                ProviderName = $"{nameof(Log4netInput)}.{loggingEvent.LoggerName}",
                Timestamp = loggingEvent.TimeStamp,
                Level = ToLogLevel[loggingEvent.Level],
                Keywords = 0,
                Payload = { { "Message", loggingEvent.MessageObject } }
            };

            if (loggingEvent.ExceptionObject != null)
            {
                eventData.Payload.Add("Exception", loggingEvent.ExceptionObject);
            }

            foreach (var key in loggingEvent.Properties.GetKeys())
            {
                try
                {
                    eventData.AddPayloadProperty(key, loggingEvent.LookupProperty(key), healthReporter, nameof(Log4netInput));
                }
                catch (Exception ex)
                {
                    healthReporter.ReportWarning($"{nameof(Log4netInput)}: event property '{key}' could not be rendered{Environment.NewLine}{ex}");
                }
            }

            return eventData;
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            eventFlowRepo.Shutdown();
            this.subject.Dispose();
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }
    }
}
