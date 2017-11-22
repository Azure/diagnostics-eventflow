// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Newtonsoft.Json;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{

    public class TcpOutput : IOutput
    {
        private TcpClient tcpClient;
        public static readonly string TraceTag = nameof(TcpOutput);
        private static readonly Task CompletedTask = Task.FromResult<object>(null);

        private readonly IHealthReporter healthReporter;
        private TcpOutputConfiguration configuration;

        public TcpOutput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            var httpOutputConfiguration = new TcpOutputConfiguration();
            try
            {
                configuration.Bind(httpOutputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(TcpOutput)} configuration encountered: '{configuration.ToString()}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(httpOutputConfiguration);
        }

        public TcpOutput(TcpOutputConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            // Clone the configuration instance since we are going to hold onto it (via this.connectionData)
            Initialize(configuration.DeepClone());
        }

        private void Initialize(TcpOutputConfiguration configuration)
        {
            string errorMessage;

            Debug.Assert(configuration != null);
            Debug.Assert(this.healthReporter != null);

            this.configuration = configuration;

            switch (this.configuration.Format)
            {
                case "text":
                case "json":
                case "json-lines":
                    break;

                default:
                    errorMessage = $"{nameof(configuration)}: unknown Format \"{nameof(this.configuration.Format)}\" specified";
                    healthReporter.ReportWarning(errorMessage, EventFlowContextIdentifiers.Configuration);
                    break;
            }

            var task = Connect();
            task.Wait();
        }

        async Task Connect()
        {
            this.tcpClient = new TcpClient(AddressFamily.InterNetwork);
            var dnsResult = await Dns.GetHostEntryAsync(configuration.ServiceHost);
            tcpClient.Client.Connect(dnsResult.AddressList, configuration.ServicePort);
        }

        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (events == null || events.Count == 0)
            {
                return CompletedTask;
            }

            try
            {
                var payload = new StringBuilder();

                switch (configuration.Format)
                {
                    case "text":
                        foreach (EventData evt in events)
                        {
                            payload.AppendLine(evt.ToString());
                        }
                        break;

                    case "json":
                        payload.Append(JsonConvert.SerializeObject(events));
                        break;

                    case "json-lines":
                        foreach (EventData evt in events)
                        {
                            payload.AppendLine(JsonConvert.SerializeObject(evt));
                        }
                        break;
                }

                if (tcpClient == null || !tcpClient.Connected)
                {
                    // try reconnection
                    var task = Connect();
                    task.Wait();
                }

                lock (tcpClient)
                {
                    StreamWriter writer = new StreamWriter(tcpClient.GetStream(), Encoding.UTF8);
                    writer.Write(payload.ToString());
                    writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                this.healthReporter.ReportProblem($"Fail to send events in batch. Error details: {ex.ToString()}");
                throw;
            }

            return CompletedTask;
        }
    }
}
