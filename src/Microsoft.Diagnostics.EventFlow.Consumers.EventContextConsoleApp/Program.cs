// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Diagnostics.Correlation.Common;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventFlow.Consumers.EventContextConsoleApp
{
    public class CorrelationTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            //add request id to every event
            var ctx = ContextResolver.GetRequestContext<MyContext>();
            if (ctx != null)
            {
                telemetry.Context.Operation.Id = ctx.CorrelationId;
            }
        }
    }

    class MyContext
    {
        public string CorrelationId;
        public string OtherId;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            using (JsonWriter writer = new JsonTextWriter(new StringWriter(sb)))
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(CorrelationId));
                writer.WriteValue(CorrelationId);
                writer.WritePropertyName(nameof(OtherId));
                writer.WriteValue(OtherId);
                writer.WriteEnd();
                writer.WriteEndObject();
            }
            return sb.ToString();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new CorrelationTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;

            using (DiagnosticPipeline pipeline = DiagnosticPipelineFactory.CreatePipeline("config.json"))
            {
                for (int i = 0; i < 10; i++)
                {
                    ContextResolver.SetRequestContext(new MyContext
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        OtherId = i.ToString()
                    });
                    Trace.TraceWarning($"{DateTime.UtcNow:o} this is log message");
                }
                Task.Delay(10000).Wait();
            }
        }
    }
}
