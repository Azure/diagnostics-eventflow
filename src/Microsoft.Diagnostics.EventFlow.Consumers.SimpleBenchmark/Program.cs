// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Correlation.Common;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Diagnostics.EventFlow.Filters;
using Microsoft.Diagnostics.EventFlow.Metadata;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Diagnostics.EventFlow.Consumers.SimpleBenchmark
{
    class Program
    {
        const int EventBatchSize = 1000; // We raise 1000 events before checking the timing etc.
        static TimeSpan WarmUpTime = TimeSpan.FromSeconds(15);
        static TimeSpan MeasurementTime = TimeSpan.FromSeconds(15);
        static TimeSpan CoolDownTime = TimeSpan.FromSeconds(5);


        static void Main(string[] args)
        {
            var healthReporter = new BenchmarkHealthReporter();

            var esConfiguration = new EventSourceConfiguration();
            esConfiguration.ProviderName = "Microsoft-AzureTools-BenchmarkEventSource";
            var eventSourceInput = new EventSourceInput(new EventSourceConfiguration[] { esConfiguration }, healthReporter);

            var metadata = new EventMetadata("importantEvent");
            metadata.Properties.Add("Importance", "High");
            EventMetadataFilter metadataFilter = new EventMetadataFilter(metadata);
            metadataFilter.IncludeCondition = "name==ImportantEvent";

            var oddSequenceNumberFilter = new CallbackFilter((e) => HasLongPropertyWhere(e.Payload, "sequenceNumber", (n) => (n & 0x1) != 0));
            var evenSequenceNumberFilter = new CallbackFilter((e) => HasLongPropertyWhere(e.Payload, "sequenceNumber", (n) => (n & 0x1) == 0));
            var oddSequenceNumberOutput = new BenchmarkOutput("Odd sequence number output");
            var evenSequenceNumberOutput = new BenchmarkOutput("Even sequence number output");
            var sinks = new EventSink[]
            {
                new EventSink(oddSequenceNumberOutput, new IFilter[] {oddSequenceNumberFilter }),
                new EventSink(evenSequenceNumberOutput, new IFilter[] {evenSequenceNumberFilter })
            };

            var pipeline = new DiagnosticPipeline(
                healthReporter, 
                new IObservable<EventData>[] { eventSourceInput },
                new IFilter[] { metadataFilter },
                sinks);

            Console.WriteLine(string.Format("Starting test... will take {0} seconds", (WarmUpTime + MeasurementTime).TotalSeconds));
            Console.WriteLine("A dot represents 10 000 events submitted");

            DateTimeOffset startTime = DateTimeOffset.Now;
            DateTimeOffset measurementStartTime = startTime + WarmUpTime;
            DateTimeOffset measurementStopTime = measurementStartTime + MeasurementTime;
            DateTimeOffset stopTime = measurementStopTime + CoolDownTime;

            bool debugMode = args.Length == 1 && string.Equals(args[0], "debug", StringComparison.OrdinalIgnoreCase);

            long eventSequenceNo = 1;
            bool measuring = debugMode;
            long measuredEventCount = 0;

            while (true)
            {
                // Every tenth event is important
                string name = "RegularEvent";
                if (eventSequenceNo % 10 == 0)
                {
                    name = "ImportantEvent";
                }

                ContextResolver.SetRequestContext(new {correlationId = eventSequenceNo});
                BenchmarkEventSource.Log.ComplexMessage(
                    Guid.NewGuid(), 
                    Guid.NewGuid(), 
                    eventSequenceNo++, 
                    name, 
                    DateTime.Now, 
                    DateTime.UtcNow, 
                    "Complex event message. blah blah");

                if (measuring)
                {
                    measuredEventCount++;
                }

                if (debugMode)
                {
                    if (measuredEventCount == 10)
                    {
                        break;
                    }
                }
                else
                {
                    if (eventSequenceNo % EventBatchSize == 0)
                    {
                        DateTimeOffset now = DateTimeOffset.Now;

                        // In this benchmmark we do not really have a cooldown period, so we do not have to account for not-measuring period at the end.
                        if (!measuring)
                        {
                            bool shouldBeMeasuring = (now >= measurementStartTime && now < measurementStopTime);
                            if (shouldBeMeasuring)
                            {
                                oddSequenceNumberOutput.ResetCounter();
                                evenSequenceNumberOutput.ResetCounter();
                                healthReporter.ResetCounters();
                                measuring = true;
                            }
                        }


                        if (eventSequenceNo % 10000 == 0)
                        {
                            Console.Write(".");
                        }

                        if (now >= stopTime)
                        {
                            break;
                        }
                    }
                }
            }

            Console.WriteLine(string.Empty);
            Console.WriteLine("Enterning cooldown period...");
            Thread.Sleep(CoolDownTime);
            pipeline.Dispose();

            Console.WriteLine(string.Empty);
            Console.WriteLine($"Total events raised during measurement period: {measuredEventCount}");
            Console.WriteLine(oddSequenceNumberOutput.Summary);
            Console.WriteLine(evenSequenceNumberOutput.Summary);
            double processingRate = (oddSequenceNumberOutput.EventCount + evenSequenceNumberOutput.EventCount) / MeasurementTime.TotalSeconds;
            Console.WriteLine($"Processing rate (events/sec): {processingRate}");
            Console.WriteLine(healthReporter.Summary);
            
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(intercept:true);
        }

        private static bool HasLongPropertyWhere(IDictionary<string, object> payload, string propertyName, Func<long, bool> condition)
        {
            Debug.Assert(payload != null);
            Debug.Assert(propertyName != null);
            Debug.Assert(condition != null);

            object value;
            if (!payload.TryGetValue(propertyName, out value))
            {
                return false;
            }

            if (!(value is long))
            {
                return false;
            }

            return condition((long)value);
        }
    }
}
