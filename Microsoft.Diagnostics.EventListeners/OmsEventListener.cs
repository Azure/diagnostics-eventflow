// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Validation;

namespace Microsoft.Diagnostics.EventListeners
{
    public class OmsEventListener : BufferingEventListener, IDisposable
    {
        const string OmsDataUploadResource = "/api/logs";
        const string OmsDataUploadUrl = OmsDataUploadResource + "?api-version=2016-04-01";
        const string MsDateHeaderName = "x-ms-date";
        const string JsonContentId = "application/json";

        private HttpClient httpClient;
        private HMACSHA256 hasher;
        private string workspaceId;

        public OmsEventListener(ICompositeConfigurationProvider configurationProvider, IHealthReporter healthReporter) : base(configurationProvider, healthReporter)
        {
            if (this.Disabled)
            {
                return;
            }

            ICompositeConfigurationProvider omsConfigurationProvider = configurationProvider.GetConfiguration("OmsEventListener");
            Verify.Operation(omsConfigurationProvider != null, "OmsEventListener configuration section is missing");
            this.workspaceId = omsConfigurationProvider.GetValue("workspaceId");
            Verify.Operation(!string.IsNullOrWhiteSpace(this.workspaceId), "workspaceId configuration parameter is not set");
            string omsWorkspaceKeyBase64 = omsConfigurationProvider.GetValue("workspaceKey");
            Verify.Operation(!string.IsNullOrWhiteSpace(omsWorkspaceKeyBase64), "workspaceKey configuration parameter is not set");
            this.hasher = new HMACSHA256( Convert.FromBase64String(omsWorkspaceKeyBase64));

            var retryHandler = new HttpExponentialRetryMessageHandler();
            this.httpClient = new HttpClient(retryHandler);
            this.httpClient.BaseAddress = new Uri($"https://{this.workspaceId}.ods.opinsights.azure.com", UriKind.Absolute);
            this.httpClient.DefaultRequestHeaders.Add("Log-Type", "TestLFALogs");

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
            try
            {
                var jsonData = JsonConvert.SerializeObject(events);
                string dateString = DateTime.UtcNow.ToString("r");

                string signature = BuildSignature(jsonData, dateString);

                HttpContent content = new StringContent(jsonData, Encoding.UTF8, JsonContentId);
                content.Headers.Add("Authorization", signature);
                content.Headers.Add("x-ms-date", dateString);

                // PostAsync is thread safe
                HttpResponseMessage response = await this.httpClient.PostAsync(OmsDataUploadUrl, null, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    this.ReportListenerHealthy();
                }
                else
                {
                    this.ReportListenerProblem($"OMS REST API returned an error. Code: {response.StatusCode} Description: ${response.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                this.ReportListenerProblem($"An error occurred while sending data to OMS: {e.ToString()}");
            }
        }

        private string BuildSignature(string message, string dateString)
        {
            string dateHeader = $"{MsDateHeaderName}:{dateString}";
            string signatureInput = $"POST\n{message.Length}\n{JsonContentId}\n{dateHeader}\n{OmsDataUploadResource}";
            byte[] signatureInputBytes = Encoding.ASCII.GetBytes(signatureInput);
            byte[] hash;
            lock(this.hasher)
            {
                hash = this.hasher.ComputeHash(signatureInputBytes);
            }
            string signature = $"SharedKey {this.workspaceId}: {Convert.ToBase64String(hash)}";
            return signature;
        }
    }
}
