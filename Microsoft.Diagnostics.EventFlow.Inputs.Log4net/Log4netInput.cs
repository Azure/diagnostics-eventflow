using log4net.Core;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Diagnostics.EventFlow.Inputs.Log4net.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Log4net
{
    public class Log4NetInput : ILogger, IObservable<EventData>, IDisposable
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

        private readonly IHealthReporter healthReporter;
        private readonly IConfiguration configuration;
        private EventFlowSubject<EventData> subject;
        private EventSourceConfiguration _log4NetInputConfiguration;

        public Log4NetInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.subject = new EventFlowSubject<EventData>();
            Name = $"EventFlow.{typeof(Log4NetInput)}";

            _log4NetInputConfiguration = new EventSourceConfiguration();
            try
            {
                configuration.Bind(_log4NetInputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(_log4NetInputConfiguration)} configuration encountered: '{configuration}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }
        }

        public string Name {get;}

        public ILoggerRepository Repository => LoggerManager.GetRepository(Assembly.GetExecutingAssembly());

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            this.subject.Dispose();
        }

        public bool IsEnabledFor(Level level) => level.Value > _log4NetInputConfiguration.LogLevel.Value;

        public void Log(Type callerStackBoundaryDeclaringType, Level level, object message, Exception exception)
        {
            if (message == null || IsEnabledFor(level))
            {
                return;
            }

            var eventData = ToEventData(callerStackBoundaryDeclaringType, level, message, exception);

            throw new NotImplementedException();
        }

        private EventData ToEventData(Type callerStackBoundaryDeclaringType, Level level, object message, Exception exception)
        {
            var eventData = new EventData
            {
                ProviderName = nameof(Log4NetInput),
                //todo remove sideeffect
                Timestamp = DateTime.UtcNow,
                Level = ToLogLevel[level],
                Keywords = 0

            };
            return eventData;
        }

        public void Log(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null || IsEnabledFor(loggingEvent.Level))
            {
                return;
            }

            var eventData = ToEventData(loggingEvent);
            this.subject.OnNext(eventData);
        }

        private EventData ToEventData(LoggingEvent loggingEvent)
        {
            var eventData = new EventData
            {
                ProviderName = nameof(Log4NetInput),
                Timestamp = loggingEvent.TimeStamp,
                Level = ToLogLevel[loggingEvent.Level],
                Keywords = 0
            };

            foreach (var key in loggingEvent.Properties.GetKeys())
            {
                try
                {
                    eventData.AddPayloadProperty(key, loggingEvent.LookupProperty(key), healthReporter, nameof(Log4NetInput));
                }
                catch (Exception ex)
                {
                    healthReporter.ReportWarning($"{nameof(Log4NetInput)}: event property '{key}' could not be rendered{Environment.NewLine}{ex}");
                }
            }

            return eventData;
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return this.subject.Subscribe(observer);
        }
    }
}
