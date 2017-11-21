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
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Newtonsoft.Json;
using Validation;
using ExtendedXmlSerializer.Configuration;
using ExtendedXmlSerializer.ExtensionModel.Xml;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{

    public class Http : IOutput
    {
        private HttpClient httpClient;
        public static readonly string TraceTag = nameof(Http);

        private readonly IHealthReporter healthReporter;
        private HttpOutputConfiguration configuration;

        public Http(IConfiguration configuration, IHealthReporter healthReporter)
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
                healthReporter.ReportProblem($"Invalid {nameof(Http)} configuration encountered: '{configuration.ToString()}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(httpOutputConfiguration);
        }

        public Http(HttpOutputConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            // Clone the configuration instance since we are going to hold onto it (via this.connectionData)
            Initialize(configuration.DeepClone());
        }

        private void Initialize(HttpOutputConfiguration configuration)
        {
            Debug.Assert(configuration != null);
            Debug.Assert(this.healthReporter != null);

            this.httpClient = new HttpClient();
            this.configuration = configuration;

            string errorMessage;
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

            switch (this.configuration.ContentType) 
            {
                case "text":
                    if (string.IsNullOrWhiteSpace(this.configuration.HttpContentType))
                    {
                        this.configuration.HttpContentType = "text/plain";
                    }
                    break;

                case "json":
                case "json-lines":
                    if (string.IsNullOrWhiteSpace(this.configuration.HttpContentType))
                    {
                        this.configuration.HttpContentType = "application/json";
                    }
                    break;

                case "xml":
                    if (string.IsNullOrWhiteSpace(this.configuration.HttpContentType))
                    {
                        this.configuration.HttpContentType = "text/xml";
                    }
                    break;

                default:
                    errorMessage = $"{nameof(configuration)}: unknown ContentType \"{nameof(this.configuration.ContentType)}\" specified";
                    healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Configuration);
                    break;
            }
        }

        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            try
            {
                string payload = "";

                switch (configuration.ContentType) 
                {
                    case "text":
                        foreach (EventData evt in events)
                        {
                            payload += evt.ToString() + "\n";
                        }
                        break;

                    case "json":
                        payload = JsonConvert.SerializeObject(events);
                        break;

                    case "json-lines":
                        foreach (EventData evt in events)
                        {
                            payload += JsonConvert.SerializeObject(evt) + "\n";
                        }
                        break;

                    case "xml":
                        var serializer = new ConfigurationContainer().Create();
                        payload = serializer.Serialize(new XmlWriterSettings { Indent = false }, events);
                        break;

                    default:
                        break;
                }

                HttpContent contentPost = new StringContent(payload, Encoding.UTF8, configuration.HttpContentType);

                var result = httpClient.PostAsync(new Uri(configuration.ServiceUri), contentPost).Result;

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail to send events in batch. Error details: {ex.ToString()}");
                this.healthReporter.ReportProblem($"Fail to send events in batch. Error details: {ex.ToString()}");
                throw;
            }
        }
    }
}
