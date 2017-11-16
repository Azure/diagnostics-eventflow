// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Diagnostics.EventFlow.Consumers.ConsoleAppCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline("config.json"))
            {
                for (int i = 0; i < 200000; i++)
                {
                    // You shall not need to call this unless you really want to diagnose EventHub issue.
                    pipeline.HealthReporter.ReportHealthy("Some health report content. . .");
                    Thread.Sleep(1);
                }
                Trace.TraceWarning("EventFlow is working!");
                Console.ReadLine();
            }
        }
    }
}
