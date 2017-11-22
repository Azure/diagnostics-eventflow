// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Newtonsoft.Json;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{

    public class UdpOutput : IOutput
    {
        private UdpClient udpClient;
        public static readonly string TraceTag = nameof(UdpOutput);

        private readonly IHealthReporter healthReporter;
        private UdpOutputConfiguration configuration;

        public UdpOutput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            var udpOutputConfiguration = new UdpOutputConfiguration();
            try
            {
                configuration.Bind(udpOutputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem($"Invalid {nameof(UdpOutput)} configuration encountered: '{configuration.ToString()}'",
                    EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(udpOutputConfiguration);
        }

        public UdpOutput(UdpOutputConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            this.healthReporter = healthReporter;

            // Clone the configuration instance since we are going to hold onto it (via this.connectionData)
            Initialize(configuration.DeepClone());
        }

        private void Initialize(UdpOutputConfiguration configuration)
        {
            string errorMessage;

            Debug.Assert(configuration != null);
            Debug.Assert(this.healthReporter != null);

            this.udpClient = new UdpClient();
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
        }

        public async Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            if (events == null || events.Count == 0)
            {
                return;
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

                Byte[] buffer = Encoding.ASCII.GetBytes(payload.ToString());
                await udpClient.SendAsync(buffer, buffer.Length, configuration.ServiceHost, configuration.ServicePort);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                this.healthReporter.ReportProblem($"Fail to send events in batch. Error details: {ex.ToString()}");
                throw;
            }
        }
    }
}
