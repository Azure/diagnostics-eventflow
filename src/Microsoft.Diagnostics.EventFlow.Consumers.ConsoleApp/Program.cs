// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Net.Http;

namespace Microsoft.Diagnostics.EventFlow.Consumers.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            using (DiagnosticPipeline pipeline = DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
            {
                // Build up the pipeline
                Console.WriteLine("Pipeline is created.");

                // Send a trace to the pipeline
                Trace.TraceInformation("This is a message from trace . . .");
                MyEventSource.Log.Message("This is a message from EventSource ...");

                // Make a simple get request to bing.com just to generate some HTTP trace
                HttpClient client = new HttpClient();
                client.GetStringAsync("http://www.bing.com").Wait();

                // Check the result
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadKey(true);
            }
        }
    }
}
