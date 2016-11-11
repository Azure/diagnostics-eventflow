using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Serilog;
using Moq;
using Xunit;
using Serilog.Events;
using System.Threading;

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
    }
}
