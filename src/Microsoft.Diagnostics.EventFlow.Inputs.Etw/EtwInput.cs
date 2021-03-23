// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Utilities.Etw;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Configuration;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    public class EtwInput : IObservable<EventData>, IDisposable, IRequireActivation
    {
        private const string ProvidersSectionName = "providers";
        private static readonly string DefaultSessionnamePrefix = $"EventFlow-{nameof(EtwInput)}";

        private IHealthReporter healthReporter;
        private EventFlowSubject<EventData> subject;
        private ITraceEventSession session;
        private bool isDisposed;

        // Optional flag that allows to decode incoming events as MsgPack
        private bool isMsgPack = false;

        public EtwInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            string sessionNamePrefix = configuration.GetValue<string>("sessionNamePrefix", DefaultSessionnamePrefix);
            bool cleanupOldSessions = configuration.GetValue<bool>("cleanupOldSessions", false);
            bool reuseExistingSession = configuration.GetValue<bool>("reuseExistingSession", false);

            // ETW event payload contains MsgPack binary-encoded event
            isMsgPack = (configuration.GetValue<string>("encoding", "") == "MsgPack");

            IConfiguration providersConfiguration = configuration.GetSection(ProvidersSectionName);
            if (providersConfiguration == null)
            {
                healthReporter.ReportProblem($"{nameof(EtwInput)}: required configuration section '{ProvidersSectionName}' is missing");
                return;
            }
            var providers = new List<EtwProviderConfiguration>();
            ConfigUtil.ConvertKeywordsToDecimal(providersConfiguration);
            try
            {
                providersConfiguration.Bind(providers);
            }
            catch
            {
                healthReporter.ReportProblem($"{nameof(EtwInput)}: configuration is invalid", EventFlowContextIdentifiers.Configuration);
                return;
            }

            Initialize(providers, sessionNamePrefix, cleanupOldSessions, reuseExistingSession, healthReporter);
        }

        public EtwInput(IReadOnlyCollection<EtwProviderConfiguration> providers, IHealthReporter healthReporter)
        {
            Requires.NotNull(providers, nameof(providers));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            Initialize(providers, DefaultSessionnamePrefix, false, false, healthReporter);
        }

        public IEnumerable<EtwProviderConfiguration> Providers { get; private set; }
        public Func<ITraceEventSession> SessionFactory { get; set; }

        public void Activate()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(EtwInput));
            }

            if (this.session != null)
            {
                throw new InvalidOperationException($"{nameof(EtwInput)} has already been activated");
            }

            if (this.Providers.Count() == 0)
            {
                healthReporter.ReportWarning($"{nameof(EtwInput)}: no providers configured", nameof(EtwInput));
                return;
            }

            this.session = SessionFactory();

            foreach (var providerConfiguration in this.Providers)
            {
                string validationError;
                if (!providerConfiguration.Validate(out validationError))
                {
                    this.healthReporter.ReportWarning($"{nameof(EtwInput)}: one of the providers is configured improperly. {validationError}", EventFlowContextIdentifiers.Configuration);
                    continue;
                }
                if (providerConfiguration.ProviderGuid != Guid.Empty)
                {
                    this.session.EnableProvider(providerConfiguration.ProviderGuid, providerConfiguration.Level, (ulong)providerConfiguration.Keywords);
                }
                else
                {
                    this.session.EnableProvider(providerConfiguration.ProviderName, providerConfiguration.Level, (ulong)providerConfiguration.Keywords);
                }
            }

            // Also enable .NET Task Parallel Library hierarchical activity tracking
            this.session.EnableProvider(TplActivities.TplEventSourceGuid, TraceEventLevel.Always, TplActivities.TaskFlowActivityIdsKeyword);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    this.session.Process((eventData) => this.subject.OnNext(eventData));
                }
                catch (Exception e)
                {
                    this.healthReporter.ReportProblem($"{nameof(EtwInput)}: ETW session has terminated unexpectedly and events are no longer collected. {e.ToString()}");
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }
            this.isDisposed = true;

            if (this.session != null)
            {
                // Might have been null if no providers were configured.
                this.session.Dispose();
                this.session = null;
            }

            this.subject.Dispose();
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(EtwInput));
            }

            return this.subject.Subscribe(observer);
        }

        private void Initialize(
            IReadOnlyCollection<EtwProviderConfiguration> providers,
            string sessionNamePrefix,
            bool cleanupOldSessions,
            bool restartExisting,
            IHealthReporter healthReporter)
        {
            this.healthReporter = healthReporter;
            this.subject = new EventFlowSubject<EventData>();
            this.isDisposed = false;
            this.SessionFactory = () => ((isMsgPack) ?
            new MsgPackTraceEventSession(sessionNamePrefix, cleanupOldSessions, restartExisting, healthReporter) :
            new StandardTraceEventSession(sessionNamePrefix, cleanupOldSessions, restartExisting, healthReporter));

            this.Providers = providers;
            // The session is not started until Activate() is called.
        }
    }
}
