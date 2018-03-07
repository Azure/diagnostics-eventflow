// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Diagnostics.EventFlow.Inputs;

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
#if NET46
    using Microsoft.Diagnostics.Tracing;

    public class EtwInputTests
    {
        private TimeSpan TraceSessionActivationTimeout = TimeSpan.FromSeconds(2);

        [Fact]
        public void ReportsEvents()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();

            var inputConfiguration = new List<EtwProviderConfiguration>();
            inputConfiguration.Add(new EtwProviderConfiguration
            {
                ProviderName = TestTraceEventSession.TestEtwProviderName
            });
            
            using (var input = new EtwInput(inputConfiguration, healthReporterMock.Object))
            using (input.Subscribe(observer.Object))
            {
                var traceSession = new TestTraceEventSession();
                input.SessionFactory = () => traceSession;
                input.Activate();
                traceSession.ProcessingStarted.WaitOne(TraceSessionActivationTimeout);

                traceSession.ReportEvent(LogLevel.Informational, 0, "Hey");

                observer.Verify(o => o.OnNext(It.Is<EventData>(data =>
                        data.Payload["Message"].Equals("Hey")
                    &&  data.Level == LogLevel.Informational
                )));

                VerifyNoErrorsOrWarnings(healthReporterMock);
            }
        }

        [Fact]
        public void StopsReportingAfterDisposed()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();

            var inputConfiguration = new List<EtwProviderConfiguration>();
            inputConfiguration.Add(new EtwProviderConfiguration
            {
                ProviderName = TestTraceEventSession.TestEtwProviderName
            });

            var input = new EtwInput(inputConfiguration, healthReporterMock.Object);
            using (input.Subscribe(observer.Object))
            {
                var traceSession = new TestTraceEventSession();
                input.SessionFactory = () => traceSession;
                input.Activate();
                traceSession.ProcessingStarted.WaitOne(TraceSessionActivationTimeout);

                traceSession.ReportEvent(LogLevel.Informational, 0, "First");

                input.Dispose();

                Assert.True(traceSession.IsDisposed);

                observer.Verify(o => o.OnNext(It.Is<EventData>(data =>
                        data.Payload["Message"].Equals("First")
                    && data.Level == LogLevel.Informational
                )), Times.Exactly(1));

                observer.Verify(o => o.OnCompleted(), Times.Exactly(1));                
            }

            VerifyNoErrorsOrWarnings(healthReporterMock);
        }

        [Fact]
        public void DoesNotReportEventsBeforeActivation()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();

            var inputConfiguration = new List<EtwProviderConfiguration>();
            inputConfiguration.Add(new EtwProviderConfiguration
            {
                ProviderName = TestTraceEventSession.TestEtwProviderName
            });

            using (var input = new EtwInput(inputConfiguration, healthReporterMock.Object))
            using (input.Subscribe(observer.Object))
            {
                var traceSession = new TestTraceEventSession();
                input.SessionFactory = () => traceSession;

                traceSession.ReportEvent(LogLevel.Informational, 0, "First");

                input.Activate();
                traceSession.ProcessingStarted.WaitOne(TraceSessionActivationTimeout);

                traceSession.ReportEvent(LogLevel.Informational, 0, "Second");

                observer.Verify(o => o.OnNext(It.IsAny<EventData>()), Times.Exactly(1));
                observer.Verify(o => o.OnNext(It.Is<EventData>(data =>
                        data.Payload["Message"].Equals("Second")
                    && data.Level == LogLevel.Informational
                )));

                VerifyNoErrorsOrWarnings(healthReporterMock);
            }
        }

        [Fact]
        public void ReportsProblemsWhenConfigurationIsInvalid()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();

            var inputConfiguration = new List<EtwProviderConfiguration>();            

            using (var input = new EtwInput(inputConfiguration, healthReporterMock.Object))
            {
                var traceSession = new TestTraceEventSession();
                input.SessionFactory = () => traceSession;
                input.Activate();
                traceSession.ProcessingStarted.WaitOne(TraceSessionActivationTimeout);

                healthReporterMock.Verify(o => o.ReportWarning(It.Is<string>(s => s.Contains("no providers configured")), 
                    It.IsAny<string>()), Times.Exactly(1));
            }

            healthReporterMock.ResetCalls();

            var providerConfiguration = new EtwProviderConfiguration();
            string validationError;
            Assert.False(providerConfiguration.Validate(out validationError));
            inputConfiguration.Add(providerConfiguration);

            using (var input = new EtwInput(inputConfiguration, healthReporterMock.Object))
            {
                var traceSession = new TestTraceEventSession();
                input.SessionFactory = () => traceSession;
                input.Activate();

                healthReporterMock.Verify(o => o.ReportWarning(It.Is<string>(s => s.Contains(validationError)),
                    It.IsAny<string>()), Times.Exactly(1));
            }
        }

        [Fact]
        public async Task ReportsClrEvents()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new TestObserver();

            var inputConfiguration = new List<EtwProviderConfiguration>();
            inputConfiguration.Add(new EtwProviderConfiguration
            {
                // For more info about this provider configuration see https://docs.microsoft.com/en-us/dotnet/framework/performance/exception-thrown-v1-etw-event
                // and  https://docs.microsoft.com/en-us/dotnet/framework/performance/clr-etw-events 
                ProviderGuid = new Guid("e13c0d23-ccbc-4e12-931b-d9cc2eee27e4"),
                Level = TraceEventLevel.Warning,
                Keywords = (TraceEventKeyword)0x8000
            });

            using (var input = new EtwInput(inputConfiguration, healthReporterMock.Object))
            using (input.Subscribe(observer))
            {
                input.Activate();

                try
                {
                    throw new Exception("We seek perfection");
                }
                catch (Exception)  {  }

                EventData exceptionEvent = null;
                bool exceptionEventRaised = await TaskUtils.PollWaitAsync(() => observer.Data.TryDequeue(out exceptionEvent), TraceSessionActivationTimeout);
                Assert.True(exceptionEventRaised);
                Assert.Equal("We seek perfection", exceptionEvent.Payload["ExceptionMessage"]);

                VerifyNoErrorsOrWarnings(healthReporterMock);
            }
        }


        private void VerifyNoErrorsOrWarnings(Mock<IHealthReporter> healthReporterMock)
        {
            healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Exactly(0));
            healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(0));
        }

        private class TestObserver : IObserver<EventData>
        {
            public bool Completed { get; private set; } = false;
            public Exception Error { get; private set; }
            public ConcurrentQueue<EventData> Data { get; } = new ConcurrentQueue<EventData>();

            public void OnCompleted()
            {
                Completed = true;
            }

            public void OnError(Exception error)
            {
                Error = error;
            }

            public void OnNext(EventData value)
            {
                Data.Enqueue(value);
            }
        }
    }
#endif
}
