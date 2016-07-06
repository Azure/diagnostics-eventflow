// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventListeners
{
    public class OmsEventListener : BufferingEventListener, IDisposable
    {
        private HttpExponentialRetryMessageHandler retryHandler;

        public OmsEventListener(IConfigurationProvider configurationProvider, IHealthReporter healthReporter) : base(configurationProvider, healthReporter)
        {
            if (this.Disabled)
            {
                return;
            }

            this.retryHandler = new HttpExponentialRetryMessageHandler();

            this.Sender = new ConcurrentEventSender<EventData>(
                eventBufferSize: 1000,
                maxConcurrency: 2,
                batchSize: 100,
                noEventsDelay: TimeSpan.FromMilliseconds(1000),
                transmitterProc: this.SendEventsAsync,
                healthReporter: healthReporter);
        }

        private async Task SendEventsAsync(IEnumerable<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            string workspaceId = "30787dd1-cacc-4bca-8944-ba88ccb6f350";
            string url = "/api/logs?api-version=2016-04-01";

            try
            {
                // TODO: the client can be created in advance
                using (var client = new HttpClient(retryHandler))
                {
                    client.BaseAddress = new Uri($"https://{workspaceId}.ods.opinsights.azure.com", UriKind.Absolute);
                    client.DefaultRequestHeaders.Add("Log-Type", "TestLFALogs");

                    HttpContent content = new StringContent("blah blah", System.Text.Encoding.UTF8, "application/json");
                    content.Headers.Add("Authorization", "signature goes here");
                    content.Headers.Add("x-ms-date", "date goes here");

                    HttpResponseMessage response = await client.PostAsync(url, null, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        this.ReportListenerHealthy();
                    }
                    else
                    {
                        this.ReportListenerProblem($"OMS REST API returned an error. Code: {response.StatusCode} Description: ${response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception e)
            {
                this.ReportListenerProblem($"An error occurred while sending data to OMS: {e.ToString()}");
            }
        }
    }
}
