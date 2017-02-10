// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Diagnostics.EventFlow.Inputs;

using Microsoft.Diagnostics.EventFlow.Configuration;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
#if NET46
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

        private void VerifyNoErrorsOrWarnings(Mock<IHealthReporter> healthReporterMock)
        {
            healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Exactly(0));
            healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(0));
        }
    }
#endif
}
