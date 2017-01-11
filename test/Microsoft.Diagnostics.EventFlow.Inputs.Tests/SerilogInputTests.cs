// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using Moq;
using Serilog;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class SerilogInputTests
    {
        [Fact]
        public void ReportsSimpleInformation()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Observers(events => events.Subscribe(serilogInput)).CreateLogger();

                string message = "Just an information";
                logger.Information(message);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data => 
                       data.Payload["Message"].Equals(message) 
                    && data.Level == LogLevel.Informational
                )));
            }
        }

        [Fact]
        public void ReportsInformationWithCustomProperties()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Observers(events => events.Subscribe(serilogInput)).CreateLogger();

                // Render the string value without quotes; render the double value with fixed decimal point, 1 digit after decimal point
                string message = "{alpha:l}{bravo:f1}{charlie}";
                logger.Information(message, "aaa", 75.5, false);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("aaa75.5" + Boolean.FalseString)
                    && data.Level == LogLevel.Informational
                    && data.Payload["alpha"].Equals("aaa")
                    && data.Payload["bravo"].Equals(75.5)
                    && data.Payload["charlie"].Equals(false)
                )));
            }
        }

        [Fact]
        public void ReportsExceptions()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Observers(events => events.Subscribe(serilogInput)).CreateLogger();

                Exception e = new Exception();
                e.Data["ID"] = 23;
                string message = "Something bad happened but we do not care that much";
                logger.Information(e, message);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals(message)
                    && data.Level == LogLevel.Informational
                    && (data.Payload["Exception"] as Exception).Data["ID"].Equals(23)
                )));
            }
        }

        [Fact]
        public void ReportsLevelsProperly()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Observers(events => events.Subscribe(serilogInput))
                    .CreateLogger();

                logger.Information("Info");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Informational)));
                observer.ResetCalls();

                logger.Debug("Debug");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));
                observer.ResetCalls();

                logger.Verbose("Verbose");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));
                observer.ResetCalls();

                logger.Warning("Warning");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Warning)));
                observer.ResetCalls();

                logger.Error("Error");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Error)));
                observer.ResetCalls();

                logger.Fatal("Fatal");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Critical)));
                observer.ResetCalls();
            }
        }

        [Fact]
        public void HandlesDuplicatePropertyNames()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Observers(events => events.Subscribe(serilogInput)).CreateLogger();

                Exception e = new Exception("Whoa!");
                string message = "I say: {Message} and you pay attention, no {Exception:l}";
                logger.Warning(e, message, "Keyser Söze", "excuses");                

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("Keyser Söze")
                    && data.Payload["Exception"].Equals("excuses")
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("Message") && key != "Message")].Equals("I say: \"Keyser Söze\" and you pay attention, no excuses")
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("Exception") && key != "Exception")].Equals(e)
                )), Times.Exactly(1));

                healthReporterMock.Verify(o => o.ReportWarning(
                        It.Is<string>(s => s.Contains("already exist in the event payload")), 
                        It.Is<string>(s => s == nameof(SerilogInput))), 
                    Times.Exactly(2));
            }
        }

        [Fact]
        public void RepresentsStructuresAsRawDictionaries()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Observers(events => events.Subscribe(serilogInput)).CreateLogger();

                var structure = new { A = "alpha", B = "bravo" };
                logger.Information("Here is {@AStructure}", structure);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                    ((IDictionary<string, object>)data.Payload["AStructure"])["A"].Equals("alpha") &&
                    ((IDictionary<string, object>)data.Payload["AStructure"])["B"].Equals("bravo")
                )));
            }
        }
    }
}
