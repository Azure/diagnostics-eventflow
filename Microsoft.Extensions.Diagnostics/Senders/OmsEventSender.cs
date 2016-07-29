// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Validation;

namespace Microsoft.Diagnostics.EventListeners
{
    public class OmsEventSender : SenderBase<EventData>
    {
        const string OmsDataUploadResource = "/api/logs";
        const string OmsDataUploadUrl = OmsDataUploadResource + "?api-version=2016-04-01";
        const string MsDateHeaderName = "x-ms-date";
        const string JsonContentId = "application/json";

        private OmsConnectionData connectionData;

        public OmsEventSender(IConfiguration configuration, IHealthReporter healthReporter) : base(healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.connectionData = CreateConnectionData(configuration, healthReporter);
        }

        private OmsConnectionData CreateConnectionData(IConfiguration configuration, IHealthReporter healthReporter)
        {
            string workspaceId = configuration["workspaceId"];
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                healthReporter.ReportProblem($"{nameof(OmsEventSender)}: 'workspaceId' configuration parameter is not set");
                return null;
            }
            string omsWorkspaceKeyBase64 = configuration["workspaceKey"];
            if (string.IsNullOrWhiteSpace(omsWorkspaceKeyBase64))
            {
                healthReporter.ReportProblem($"{nameof(OmsEventSender)}: 'workspaceKey' configuration parameter is not set");
                return null;
            }

            var hasher = new HMACSHA256(Convert.FromBase64String(omsWorkspaceKeyBase64));

            var retryHandler = new HttpExponentialRetryMessageHandler();
            var httpClient = new HttpClient(retryHandler);
            httpClient.BaseAddress = new Uri($"https://{workspaceId}.ods.opinsights.azure.com", UriKind.Absolute);

            string logTypeName = configuration["logTypeName"];
            if (string.IsNullOrWhiteSpace(logTypeName))
            {
                logTypeName = "ETWEvent";
            }
            httpClient.DefaultRequestHeaders.Add("Log-Type", logTypeName);

            return new OmsConnectionData { HttpClient = httpClient, Hasher = hasher, WorkspaceId = workspaceId };
        }

        public override async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (this.connectionData == null || events == null || events.Count == 0)
            {
                return;
            }

            try
            {
                string jsonData = JsonConvert.SerializeObject(events);

                string dateString = DateTime.UtcNow.ToString("r");

                string signature = BuildSignature(jsonData, dateString);

                HttpContent content = new StringContent(jsonData, Encoding.UTF8, JsonContentId);
                content.Headers.ContentType = new MediaTypeHeaderValue(JsonContentId);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, OmsDataUploadUrl);
                request.Headers.Add("Authorization", signature);
                request.Headers.Add(MsDateHeaderName, dateString);
                request.Content = content;

                // SendAsync is thread safe
                HttpResponseMessage response = await this.connectionData.HttpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    this.ReportSenderHealthy();
                }
                else
                {
                    this.ReportSenderProblem($"OMS REST API returned an error. Code: {response.StatusCode} Description: ${response.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                this.ReportSenderProblem($"An error occurred while sending data to OMS: {e.ToString()}");
            }
        }

        private string BuildSignature(string message, string dateString)
        {
            string dateHeader = $"{MsDateHeaderName}:{dateString}";
            string signatureInput = $"POST\n{message.Length}\n{JsonContentId}\n{dateHeader}\n{OmsDataUploadResource}";
            byte[] signatureInputBytes = Encoding.ASCII.GetBytes(signatureInput);
            byte[] hash;
            lock(this.connectionData.Hasher)
            {
                hash = this.connectionData.Hasher.ComputeHash(signatureInputBytes);
            }
            string signature = $"SharedKey {this.connectionData.WorkspaceId}:{Convert.ToBase64String(hash)}";
            return signature;
        }

        private class OmsConnectionData
        {
            public HttpClient HttpClient { get; set; }
            public HMACSHA256 Hasher { get; set; }
            public string WorkspaceId { get; set; }
        }
    }
}
