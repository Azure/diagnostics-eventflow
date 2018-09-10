// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Moq;
using NLog;
using Xunit;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class NLogInputTests
    {
        [Fact]
        public void ReportsSimpleInformation()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var nlogTarget = new NLogInput(healthReporterMock.Object))
            using (nlogTarget.Subscribe(observer.Object))
            {
                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(nlogTarget, NLog.LogLevel.Info);
                var logger = NLog.LogManager.GetCurrentClassLogger();

                string message = "Just an information";
                logger.Info(message);

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
            using (var nlogTarget = new NLogInput(healthReporterMock.Object))
            using (nlogTarget.Subscribe(observer.Object))
            {
                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(nlogTarget, NLog.LogLevel.Info);
                var logger = NLog.LogManager.GetCurrentClassLogger();
                NLog.LogManager.Configuration.DefaultCultureInfo = System.Globalization.CultureInfo.InvariantCulture;

                // Render the string value without quotes; render the double value with fixed decimal point, 1 digit after decimal point
                string message = "{alpha:l}{bravo}{charlie}";
                logger.Info(message, "aaa", 75.5, false);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("aaa75.5false")
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
            using (var nlogTarget = new NLogInput(healthReporterMock.Object))
            using (nlogTarget.Subscribe(observer.Object))
            {
                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(nlogTarget, NLog.LogLevel.Info);
                var logger = NLog.LogManager.GetCurrentClassLogger();

                Exception e = new Exception();
                e.Data["ID"] = 23;
                string message = "Something bad happened but we do not care that much";
                logger.Info(e, message);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals(message)
                    && data.Level == LogLevel.Informational
                    && (data.Payload["Exception"] as Exception).Data["ID"].Equals(23)
                )));
            }
        }

        [Fact]
        public void ReportsError()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var nlogTarget = new NLogInput(healthReporterMock.Object))
            using (nlogTarget.Subscribe(observer.Object))
            {
                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(nlogTarget, NLog.LogLevel.Info);
                var logger = NLog.LogManager.GetCurrentClassLogger();

                Exception e = new Exception();
                e.Data["ID"] = 23;
                string message = "Something bad happened but we do not care that much";
                logger.Error(e, message);


                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                                                                   data.Payload["Message"].Equals(message)
                                                                   && ContainsException(data, e)
                                                                   && data.Level == LogLevel.Error
                                                                   && (data.Payload["Exception"] as Exception).Data["ID"].Equals(23)
                                              )));
            }
        }

        private bool ContainsException(EventData data, Exception expectedException)
        {
            IReadOnlyCollection<EventMetadata> metaData;
            data.TryGetMetadata(ExceptionData.ExceptionMetadataKind, out metaData);
            foreach (EventMetadata eventMetadata in metaData)
            {
                Exception exception = (Exception)data.Payload[eventMetadata.Properties[ExceptionData.ExceptionPropertyMoniker]];
                return exception == expectedException;
            }
            return false;
        }

        [Fact]
        public void ReportsTargetContextProperties()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var nlogTarget = new NLogInput(healthReporterMock.Object))
            using (nlogTarget.Subscribe(observer.Object))
            {
                nlogTarget.ContextProperties.Add(new NLog.Targets.TargetPropertyWithContext() { Name = "ThreadId", Layout = "${threadid}" });

                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(nlogTarget, NLog.LogLevel.Info);
                var logger = NLog.LogManager.GetCurrentClassLogger();

                string messageTemplate = "Hello, {Name}!";
                logger.Info(messageTemplate, "World");

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("Hello, \"World\"!")
                    && data.Level == LogLevel.Informational
                    && !data.Payload["ThreadId"].Equals(0)
                )));
            }
        }

        [Fact]
        public void ReportsLevelsProperly()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var nlogTarget = new NLogInput(healthReporterMock.Object))
            using (nlogTarget.Subscribe(observer.Object))
            {
                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(nlogTarget, NLog.LogLevel.Trace);
                var logger = NLog.LogManager.GetCurrentClassLogger();

                logger.Info("Info");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Informational)));
                observer.ResetCalls();

                logger.Debug("Debug");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));
                observer.ResetCalls();

                logger.Trace("Verbose");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));
                observer.ResetCalls();

                logger.Warn("Warning");
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
            using (var nlogTarget = new NLogInput(healthReporterMock.Object))
            using (nlogTarget.Subscribe(observer.Object))
            {
                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(nlogTarget, NLog.LogLevel.Trace);
                var logger = NLog.LogManager.GetCurrentClassLogger();

                Exception e = new Exception("Whoa!");
                const string message = "I say: {Message} and you pay attention, no {Exception:l}";
                logger.Warn(e, message, "Keyser Söze", "excuses");

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("I say: \"Keyser Söze\" and you pay attention, no excuses")
                    && data.Payload["Exception"].Equals(e)
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("Message") && !key.EndsWith("Template") && key != "Message")].Equals("Keyser Söze")
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("Exception") && key != "Exception")].Equals("excuses")
                )), Times.Exactly(1));

                healthReporterMock.Verify(o => o.ReportWarning(
                        It.Is<string>(s => s.Contains("already exist in the event payload")),
                        It.Is<string>(s => s == nameof(NLogInput))),
                    Times.Exactly(2));
            }
        }

        [Fact]
        public void ValidConfigurationCanWriteToInputPipeline()
        {
            var healthReportMock = new Mock<IHealthReporter>();
            var mockOutput = new Mock<IOutput>();

            using (var nlogInput = new NLogInput(healthReportMock.Object))
            using (var pipeline = new DiagnosticPipeline(
                healthReportMock.Object,
                new[] { nlogInput },
                new IFilter[0],
                new[] { new EventSink(mockOutput.Object, new IFilter[0]) }))
            {
                var input = pipeline.ConfigureNLogInput(NLog.LogLevel.Info);
                Assert.NotNull(input);
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger.Info("some message");
            }
            mockOutput.Verify(
                output => output.SendEventsAsync(It.Is<IReadOnlyCollection<EventData>>(c => c.Count == 1),
                    It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}
