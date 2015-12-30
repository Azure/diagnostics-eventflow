// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

//Example Usage is very similar to Elastic Listener guide here https://azure.microsoft.com/en-us/documentation/articles/service-fabric-diagnostic-how-to-use-elasticsearch/
//Where the elastic listener is created in program.cs use this:
//
//var configProvider = new FabricConfigurationProvider("AppInsightsEventListener");
//AppInsightsListener appInsightsListener = null;
//if (configProvider.HasConfiguration)
//{
//   appInsightsListener = new AppInsightsListener(configProvider, new FabricHealthReporter("AppInsightsEventListener"));
//}
//
//Then it requires a section in the services settings.xml called "AppInsightsEventListener" with Parameter of "InstrumentationKey" set to your app insights key. 

namespace Microsoft.Diagnostics.EventListeners
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Nest;
    using ApplicationInsights;
    using ApplicationInsights.DataContracts;
    public class ApplicationInsightsEventListener : BufferingEventListener, IDisposable
    {
        private const string AppInsightsKeyName = "InstrumentationKey";
        private readonly TelemetryClient telemetry;


        public ApplicationInsightsEventListener(IConfigurationProvider configurationProvider, IHealthReporter healthReporter) : base(configurationProvider, healthReporter)
        {
            if (this.Disabled)
            {
                return;
            }

            Debug.Assert(configurationProvider != null);

            telemetry = new TelemetryClient();
            telemetry.Context.InstrumentationKey = configurationProvider.GetValue(AppInsightsKeyName);

            this.Sender = new ConcurrentEventSender<EventData>(
                eventBufferSize: 1000,
                maxConcurrency: 2,
                batchSize: 100,
                noEventsDelay: TimeSpan.FromMilliseconds(1000),
                transmitterProc: this.SendEventsAsync,
                healthReporter: healthReporter);
        }



        private Task SendEventsAsync(IEnumerable<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            var completedTask = Task.FromResult(0);

            if (events == null)
            {
                return completedTask;
            }

            try
            {
                foreach (var e in events)
                {
                    var properties = new Dictionary<string, string>
                    {
                        {nameof(e.EventName), e.EventName },
                        {nameof(e.EventId), e.EventId.ToString() },
                        {nameof(e.Keywords), e.Keywords},
                        {nameof(e.Level), e.Level},
                        {nameof(e.Message), e.Message},
                        {nameof(e.ProviderName), e.ProviderName}
                    };


                    foreach (var item in e.Payload)
                    {
                        properties.Add(item.Key, item.Value.ToString());
                    }

                    telemetry.TrackEvent(e.EventName, properties);

                }
                telemetry.Flush();
                this.ReportListenerHealthy();
            }
            catch (Exception e)
            {
                this.ReportListenerProblem("Diagnostics data upload has failed." + Environment.NewLine + e.ToString());
            }

            return completedTask;
        }




    }
}
