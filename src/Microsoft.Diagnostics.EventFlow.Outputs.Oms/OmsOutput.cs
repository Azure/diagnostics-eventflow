// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Validation;

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Utilities;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class OmsOutput : IOutput
    {
        const string OmsDataUploadResource = "/api/logs";
        const string OmsDataUploadUrl = OmsDataUploadResource + "?api-version=2016-04-01";
        const string MsDateHeaderName = "x-ms-date";
        const string JsonContentId = "application/json";

        private readonly IHealthReporter healthReporter;
        private OmsConnectionData connectionData;

        public OmsOutput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            var omsOutputConfiguration = new OmsOutputConfiguration();
            try
            {
                configuration.Bind(omsOutputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(OmsOutput)} configuration encountered: '{configuration.ToString()}'",
                   EventFlowContextIdentifiers.Configuration);
                throw;
            }

            this.connectionData = CreateConnectionData(omsOutputConfiguration);
        }

        public OmsOutput(OmsOutputConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            this.connectionData = CreateConnectionData(configuration);
        }

        private OmsConnectionData CreateConnectionData(OmsOutputConfiguration configuration)
        {
            Debug.Assert(this.healthReporter != null);
            Debug.Assert(configuration != null);

            string workspaceId = configuration.WorkspaceId;
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                this.healthReporter.ReportProblem($"{nameof(OmsOutput)}: 'workspaceId' configuration parameter is not set");
                return null;
            }
            string omsWorkspaceKeyBase64 = configuration.WorkspaceKey;
            if (string.IsNullOrWhiteSpace(omsWorkspaceKeyBase64))
            {
                this.healthReporter.ReportProblem($"{nameof(OmsOutput)}: 'workspaceKey' configuration parameter is not set");
                return null;
            }

            var hasher = new HMACSHA256(Convert.FromBase64String(omsWorkspaceKeyBase64));

            var retryHandler = new HttpExponentialRetryMessageHandler();
            var httpClient = new HttpClient(retryHandler);

            if (configuration.UseAzureGov)
            {
                httpClient.BaseAddress = new Uri($"https://{workspaceId}.ods.opinsights.azure.us", UriKind.Absolute);
            }
            else
            {
                httpClient.BaseAddress = new Uri($"https://{workspaceId}.ods.opinsights.azure.com", UriKind.Absolute);
            }            

            string logTypeName = configuration.LogTypeName;
            if (string.IsNullOrWhiteSpace(logTypeName))
            {
                logTypeName = "Event";
            }
            httpClient.DefaultRequestHeaders.Add("Log-Type", logTypeName);

            return new OmsConnectionData { HttpClient = httpClient, Hasher = hasher, WorkspaceId = workspaceId };
        }

        public async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
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
                HttpResponseMessage response = await this.connectionData.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    this.healthReporter.ReportHealthy();
                }
                else
                {
                    string responseContent = string.Empty;
                    try
                    {
                        responseContent = await response.Content.ReadAsStringAsync();
                    }
                    catch { }

                    string errorMessage = $"{nameof(OmsOutput)}: OMS REST API returned an error. Code: {response.StatusCode} Description: {response.ReasonPhrase} {responseContent}";
                    this.healthReporter.ReportProblem(errorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorHandlingPolicies.HandleOutputTaskError(e, () => 
                {
                    string errorMessage = nameof(OmsOutput) + ": an error occurred while sending data to OMS: " + Environment.NewLine + e.ToString();
                    this.healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Output);
                });
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
