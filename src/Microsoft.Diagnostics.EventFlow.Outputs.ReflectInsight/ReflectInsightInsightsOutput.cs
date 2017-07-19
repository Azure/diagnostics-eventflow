// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;
using ReflectSoftware.Insight;
using ReflectSoftware.Insight.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Diagnostics.EventFlow.IOutput" />
    public class ReflectInsightOutput : IOutput
    {
        private static readonly Task CompletedTask = Task.FromResult<object>(null);
        public static readonly string TraceTag = nameof(ReflectInsightOutput);

        private readonly IHealthReporter _healthReporter;
        private readonly IReflectInsight _reflectInsight;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectInsightOutput" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="healthReporter">The health reporter.</param>
        public ReflectInsightOutput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            _healthReporter = healthReporter;

            var riConfig = new ReflectInsightOutputConfiguration();
            try
            {
                configuration.Bind(riConfig);
                _reflectInsight = RILogManager.Get(riConfig.InstanceName ?? TraceTag);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(TraceTag)} configuration encountered: '{configuration.ToString()}'", EventFlowContextIdentifiers.Configuration);
                throw;
            }
        }

        /// <summary>
        /// Sends the events asynchronous.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="transmissionSequenceNumber">The transmission sequence number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (events == null || !events.Any())
            {
                return CompletedTask;
            }

            try
            {
                foreach (var evt in events)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return CompletedTask;
                    }

                    RIExtendedMessageProperty.AttachToRequest(TraceTag, "Provider", evt.ProviderName);

                    evt.Payload.TryGetValue("Message", out object message);
                    _reflectInsight.SendJSON((message as string) ?? evt.ProviderName, evt);
                }

                _healthReporter.ReportHealthy();

                return CompletedTask;
            }
            catch (Exception ex)
            {
                _healthReporter.ReportProblem($"{TraceTag}: Fail to send events in batch. Error details: {ex.ToString()}");
                throw;
            }
        }
    }

}
