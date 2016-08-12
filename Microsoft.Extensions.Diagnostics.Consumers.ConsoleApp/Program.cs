using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.Core.Implementations;
using Microsoft.Extensions.Diagnostics.Inputs;
using Microsoft.Extensions.Diagnostics.Outputs.StdOutput;

namespace Microsoft.Extensions.Diagnostics.Consumers.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // HealthReporter
            IHealthReporter reporter = CsvFileHealthReport.Default;

            // Listeners
            List<IObservable<EventData>> inputs = new List<IObservable<EventData>>();
            inputs.Add(new TraceInput(reporter));

            // Senders
            List<EventDataSender> senders = new List<EventDataSender>();
            senders.Add(new StdSender(reporter));

            DiagnosticsPipeline<EventData> pipeline = new DiagnosticsPipeline<EventData>(reporter, inputs,
                new EventSink<EventData>[] {
                    new EventSink<EventData>(new StdSender(reporter), null)
            });

            // Build up the pipeline
            Console.WriteLine("Pipeline is created.");

            // Send a trace to the pipeline
            Trace.TraceInformation("This is a message from trace . . .");

            // Check the result
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
        }
    }
}
