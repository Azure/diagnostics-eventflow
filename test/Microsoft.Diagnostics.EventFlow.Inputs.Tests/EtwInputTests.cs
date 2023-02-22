// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Xunit;
using Moq;

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
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

                // Activation of the input causes creation of an ETW listening session in a separate Task.
                // It takes a bit of time for the session to start capturing events.
                // Use a delay to minimize the chance that the following exception will be missed.
                await Task.Delay(TraceSessionActivationTimeout);
                
                try
                {
                    throw new Exception("We seek perfection");
                }
                catch (Exception)  {  }

                EventData exceptionEvent = null;
                bool exceptionEventRaised = await TaskUtils.PollWaitAsync(() => {
                    var fetched = observer.Data.TryDequeue(out exceptionEvent);
                    if (!fetched)
                    {
                        return false;
                    }

                    return exceptionEvent.Payload.Keys.Contains("ExceptionMessage") 
                        && string.Equals("We seek perfection", exceptionEvent.Payload["ExceptionMessage"]);
                }, TimeSpan.FromSeconds(20));
                Assert.True(exceptionEventRaised);

                VerifyNoErrorsOrWarnings(healthReporterMock);
            }
        }

        [Fact]
        public void CanReadKeywordsInHexFormat()
        {
            string inputConfiguration = @"
                {
                    ""type"": ""ETW"",
                    ""providers"": [
                        { ""providerName"": ""EventSourceInput-TestEventSource"", ""keywords"": ""0xF7"" }
                    ]
                }";

            
            using (var configFile = new TemporaryFile())
            {
                configFile.Write(inputConfiguration);
                var cb = new ConfigurationBuilder();
                cb.AddJsonFile(configFile.FilePath);
                var configuration = cb.Build();

                var healthReporterMock = new Mock<IHealthReporter>();
                var input = new EtwInput(configuration, healthReporterMock.Object);

                Assert.Equal((TraceEventKeyword)0xF7, input.Providers.First().Keywords);

                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
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
}
