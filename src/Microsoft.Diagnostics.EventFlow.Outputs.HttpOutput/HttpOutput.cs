// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Utilities;
using Newtonsoft.Json;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{

    public class HttpOutput : IOutput
    {
        private Implementation.IHttpClient httpClient;
        public static readonly string TraceTag = nameof(HttpOutput);

        private readonly IHealthReporter healthReporter;
        private HttpOutputConfiguration configuration;

        public HttpOutput(IConfiguration configuration, IHealthReporter healthReporter, Implementation.IHttpClient httpClient = null)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;
            
            var httpOutputConfiguration = new HttpOutputConfiguration();
            try
            {
                configuration.Bind(httpOutputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(HttpOutput)} configuration encountered: '{configuration.ToString()}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(httpOutputConfiguration, httpClient);
        }

        public HttpOutput(HttpOutputConfiguration configuration, IHealthReporter healthReporter, Implementation.IHttpClient httpClient = null)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            // Clone the configuration instance since we are going to hold onto it (via this.connectionData)
            Initialize(configuration.DeepClone(), httpClient);
        }

        private void Initialize(HttpOutputConfiguration configuration, Implementation.IHttpClient httpClient)
        {
            string errorMessage;

            Debug.Assert(configuration != null);
            Debug.Assert(this.healthReporter != null);

            this.httpClient = httpClient ?? new StandardHttpClient();
            this.configuration = configuration;

            if (string.IsNullOrWhiteSpace(this.configuration.ServiceUri)) {
                var errMsg = $"{nameof(HttpOutput)}: no ServiceUri configured";
                healthReporter.ReportProblem(errMsg);
                throw new Exception(errMsg);
            }

            string userName = configuration.BasicAuthenticationUserName;
            string password = configuration.BasicAuthenticationUserPassword;
            bool credentialsIncomplete = string.IsNullOrWhiteSpace(userName) ^ string.IsNullOrWhiteSpace(password);
            if (credentialsIncomplete)
            {
                errorMessage = $"{nameof(configuration)}: for basic authentication to work both user name and password must be specified";
                healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Configuration);
                userName = password = null;
            }

            if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password)) 
            {
                string httpAuthValue = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password)));
                this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", httpAuthValue);
            }

            switch (this.configuration.Format) 
            {
                case HttpOutputFormat.Json:
                case HttpOutputFormat.JsonLines:
                    if (string.IsNullOrWhiteSpace(this.configuration.HttpContentType))
                    {
                        this.configuration.HttpContentType = "application/json";
                    }
                    break;
            }
        }

        public async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (events == null || events.Count == 0)
            {
                return;
            }

            try
            {
                var payload = new StringBuilder("");

                switch (configuration.Format) 
                {
                    case HttpOutputFormat.Json:
                        payload.Append(JsonConvert.SerializeObject(events));
                        break;

                    case HttpOutputFormat.JsonLines:
                        foreach (EventData evt in events)
                        {
                            payload.AppendLine(JsonConvert.SerializeObject(evt));
                        }
                        break;
                }

                HttpContent contentPost = new StringContent(payload.ToString(), Encoding.UTF8, configuration.HttpContentType);

                HttpResponseMessage response = await httpClient.PostAsync(new Uri(configuration.ServiceUri), contentPost);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                ErrorHandlingPolicies.HandleOutputTaskError(e, () =>
                {
                    string errorMessage = nameof(HttpOutput) + ": diagnostic data upload failed: " + Environment.NewLine + e.ToString();
                    this.healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Output);
                });
            }
        }

        private class StandardHttpClient : Implementation.IHttpClient
        {
            private HttpClient httpClient = new HttpClient();

            public HttpRequestHeaders DefaultRequestHeaders => this.httpClient.DefaultRequestHeaders;

            public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
            {
                return this.httpClient.PostAsync(requestUri, content);
            }
        }
    }
}
