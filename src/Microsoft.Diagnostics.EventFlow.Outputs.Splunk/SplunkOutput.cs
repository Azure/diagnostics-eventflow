// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Outputs.Splunk.Configuration;
using Microsoft.Diagnostics.EventFlow.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Splunk
{
    public class SplunkOutput : IOutput
    {
        private const string JsonContentId = "application/json";
        private const string HttpEventCollectorResource = "/services/collector/event/1.0";
        private const string AuthorizationHeaderScheme = "Splunk";
        private readonly IHealthReporter healthReporter;
        private readonly SplunkConnectionData connectionData;

        public SplunkOutput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            var splunkOutputConfiguration = new SplunkOutputConfiguration();
            try
            {
                configuration.Bind(splunkOutputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(SplunkOutput)} configuration encountered: '{configuration}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            connectionData = CreateConnectionData(splunkOutputConfiguration);
        }

        public SplunkOutput(SplunkOutputConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            // Clone the configuration instance since we are going to hold onto it (via this.connectionData)
            connectionData = CreateConnectionData(configuration.DeepClone());
        }

        public async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (connectionData == null || events == null || events.Count == 0)
            {
                return;
            }

            try
            {
                var serializedEvents = new StringBuilder();
                foreach (var eventData in events)
                {
                    string jsonData = JsonConvert.SerializeObject(
                        new SplunkEventData(eventData, connectionData.Host, connectionData.Index, connectionData.Source, connectionData.SourceType));
                    serializedEvents.Append(jsonData);
                }
                
                HttpContent content = new StringContent(serializedEvents.ToString(), Encoding.UTF8, JsonContentId);

                // SendAsync is thread safe
                HttpResponseMessage response = await connectionData.HttpClient.PostAsync(HttpEventCollectorResource, content, cancellationToken).ConfigureAwait(false);               
                if (response.IsSuccessStatusCode)
                {
                    healthReporter.ReportHealthy();
                }
                else
                {
                    string responseContent = string.Empty;
                    try
                    {
                        responseContent = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception e)
                    {
                        healthReporter.ReportProblem($"{nameof(SplunkOutput)}: An error occurred trying to read the content of a failure response. Exception: {e}");
                    }

                    string errorMessage = $"{nameof(SplunkOutput)}: Splunk HTTP Event Collector REST API returned an error. Code: {response.StatusCode} Description: {response.ReasonPhrase} {responseContent}";
                    healthReporter.ReportProblem(errorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorHandlingPolicies.HandleOutputTaskError(e, () =>
                {
                    string errorMessage = $"{nameof(SplunkOutput)}: An error occurred while sending data to Splunk. Exception: {e}";
                    healthReporter.ReportProblem(errorMessage, EventFlowContextIdentifiers.Output);
                });
            }            
        }

        private SplunkConnectionData CreateConnectionData(SplunkOutputConfiguration configuration)
        {
            string serviceBaseAddress = configuration.ServiceBaseAddress;
            if (string.IsNullOrWhiteSpace(serviceBaseAddress))
            {
                var errorMessage = $"{nameof(SplunkOutput)}: 'serviceBaseAddress' configuration parameter is not set";
                healthReporter.ReportProblem(errorMessage);
                throw new Exception(errorMessage);
            }
            string authenticationToken = configuration.AuthenticationToken;
            if (string.IsNullOrWhiteSpace(authenticationToken))
            {
                var errorMessage = $"{nameof(SplunkOutput)}: 'authenticationToken' configuration parameter is not set";
                healthReporter.ReportProblem(errorMessage);
                throw new Exception(errorMessage);
            }

            string host = !string.IsNullOrWhiteSpace(configuration.Host) ? configuration.Host : Environment.MachineName;
            string index = !string.IsNullOrWhiteSpace(configuration.Index) ? configuration.Index : null;
            string source = !string.IsNullOrWhiteSpace(configuration.Source) ? configuration.Source : null;
            string sourceType = !string.IsNullOrWhiteSpace(configuration.SourceType) ? configuration.SourceType : null;            

            var handler = new HttpExponentialRetryMessageHandler();
            var httpClient = new HttpClient(handler);            
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthorizationHeaderScheme, authenticationToken);            
            httpClient.BaseAddress = new Uri(serviceBaseAddress, UriKind.Absolute);

            return new SplunkConnectionData(httpClient, host, index, source, sourceType);
        }

        private class SplunkConnectionData
        {
            public SplunkConnectionData(
                HttpClient httpClient, 
                string host,
                string index, 
                string source,
                string sourceType)
            {
                HttpClient = httpClient;
                Host = host;
                SourceType = sourceType;
                Index = index;
                Source = source;
            }

            public HttpClient HttpClient { get; }    
            
            public string Host { get; }

            public string Index { get; }

            public string Source { get; }

            public string SourceType { get; }
        }
    }
}