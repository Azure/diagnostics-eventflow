// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Validation;

using Microsoft.Diagnostics.EventFlow;

namespace Microsoft.Diagnostics.EventFlow.Outputs
{
    public class StdOutput : IOutput
    {
        public static readonly string TraceTag = nameof(StdOutput);

        private readonly IHealthReporter healthReporter;
        private readonly Action<string> writeOutput;

        public StdOutput(IHealthReporter healthReporter)
        {
            Requires.NotNull(healthReporter, nameof(healthReporter));
            this.healthReporter = healthReporter;
            SerializerSettings = EventFlowJsonUtilities.GetDefaultSerializerSettings();
            this.writeOutput = Console.WriteLine;
        }

        // Test constructor
        internal StdOutput(IHealthReporter healthReporter, Action<string> outputWriter): this(healthReporter)
        {
            Requires.NotNull(outputWriter, nameof(outputWriter));
            this.writeOutput = outputWriter;
        }

        public JsonSerializerSettings SerializerSettings { get; set; }

        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            try
            {
                foreach (EventData evt in events)
                {
                    string eventString = JsonConvert.SerializeObject(evt, SerializerSettings);
                    string output = $"[{transmissionSequenceNumber}] {eventString}";

                    this.writeOutput(output);
                }
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                this.healthReporter.ReportProblem($"Fail to send events in batch. Error details: {ex.ToString()}");
                throw;
            }
        }
    }
}
