﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class LoggerInputTests
    {
        [Fact]
        public void LoggerShouldSubmitData()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();

            using (LoggerInput target = new LoggerInput(healthReporterMock.Object))
            {
                var diagnosticPipeline = createPipeline(target, healthReporterMock.Object);
                using (target.Subscribe(subject.Object))
                {
                    var factory = new LoggerFactory();
                    factory.AddEventFlow(diagnosticPipeline);
                    var logger = new Logger<LoggerInputTests>(factory);
                    logger.LogInformation("log message {number}", 1);
                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => checkEventData(
                        data,
                        "log message 1",
                        typeof(LoggerInputTests).FullName,
                        LogLevel.Informational,
                        0,
                        null,
                        new Dictionary<string, object> {{ "number", 1 }}))), Times.Exactly(1));
                }
            }
        }

        [Fact]
        public void LoggerShouldSubmitException()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();

            using (LoggerInput target = new LoggerInput(healthReporterMock.Object))
            {
                var diagnosticPipeline = createPipeline(target, healthReporterMock.Object);
                using (target.Subscribe(subject.Object))
                {
                    var factory = new LoggerFactory();
                    factory.AddEventFlow(diagnosticPipeline);
                    var logger = new Logger<LoggerInputTests>(factory);

                    var eventId = new EventId(1, "EventName");
                    var exception = new Exception("Exception");

                    logger.LogError(eventId, exception, "log message {number}", 1);

                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => checkEventData(
                        data,
                        "log message 1",
                        typeof(LoggerInputTests).FullName,
                        LogLevel.Error,
                        eventId,
                        exception,
                        new Dictionary<string, object> { { "number", 1 } }))), Times.Exactly(1));
                }
            }
        }

        [Fact]
        public void LoggerShouldSubmitContext()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();

            using (LoggerInput target = new LoggerInput(healthReporterMock.Object))
            {
                var diagnosticPipeline = createPipeline(target, healthReporterMock.Object);
                using (target.Subscribe(subject.Object))
                {
                    var factory = new LoggerFactory();
                    factory.AddEventFlow(diagnosticPipeline);
                    var logger = new Logger<LoggerInputTests>(factory);
                    using (logger.BeginScope("scope {id}", 123))
                    {
                        logger.LogInformation("log message {number}", 1);

                        subject.Verify(s => s.OnNext(It.Is<EventData>(data => checkEventData(
                            data,
                            "log message 1",
                            typeof(LoggerInputTests).FullName,
                            LogLevel.Informational,
                            0,
                            null,
                            new Dictionary<string, object> {{"id", 123}, {"number", 1}, {"Scope", "scope 123"} }))), Times.Exactly(1));
                    }
                }
            }
        }

        [Fact]
        public void LoggerShouldSubmitGenericContext()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();

            using (LoggerInput target = new LoggerInput(healthReporterMock.Object))
            {
                var diagnosticPipeline = createPipeline(target, healthReporterMock.Object);
                using (target.Subscribe(subject.Object))
                {
                    var factory = new LoggerFactory();
                    factory.AddEventFlow(diagnosticPipeline);
                    var logger = new Logger<LoggerInputTests>(factory);
                    var state = new {id = 1};
                    using (logger.BeginScope(state))
                    {
                        logger.LogInformation("log message {number}", 1);
                        subject.Verify(s => s.OnNext(It.Is<EventData>(data => checkEventData(
                            data,
                            "log message 1",
                            typeof(LoggerInputTests).FullName,
                            LogLevel.Informational,
                            0,
                            null,
                            new Dictionary<string, object> { { "Scope", state }, { "number", 1 } }))), Times.Exactly(1));
                    }
                }
            }
        }

        [Fact]
        public void LoggerShouldSubmitCorrectLevel()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();

            using (LoggerInput target = new LoggerInput(healthReporterMock.Object))
            {
                var diagnosticPipeline = createPipeline(target, healthReporterMock.Object);
                using (target.Subscribe(subject.Object))
                {
                    var factory = new LoggerFactory();
                    factory.AddEventFlow(diagnosticPipeline);
                    var logger = new Logger<LoggerInputTests>(factory);

                    logger.LogTrace("trace");
                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));

                    logger.LogDebug("debug");
                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));

                    logger.LogInformation("information");
                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Informational)));

                    logger.LogWarning("warning");
                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Warning)));

                    logger.LogError("error");
                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Error)));

                    logger.LogCritical("critical");
                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Critical)));

                    logger.LogNone("none");
                    subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));
                }
            }
        }

        [Fact]
        public void LoggerShouldSubmitContextWithDuplicates()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();

            using (LoggerInput target = new LoggerInput(healthReporterMock.Object))
            {
                var diagnosticPipeline = createPipeline(target, healthReporterMock.Object);
                using (target.Subscribe(subject.Object))
                {
                    var factory = new LoggerFactory();
                    factory.AddEventFlow(diagnosticPipeline);
                    var logger = new Logger<LoggerInputTests>(factory);
                    var expectedPayload = new Dictionary<string, object>
                    {
                        ["id"] = 1,
                        ["Message"] = "message",
                        ["EventId"] = 9,
                        ["Scope"] = "scope"
                    };

                    using (logger.BeginScope("scope {id}", 2))
                    {
                        subject.Setup(s => s.OnNext(It.IsAny<EventData>())).Callback((EventData data) =>
                        {
                            foreach (var kv in expectedPayload)
                                Assert.Contains(kv, data.Payload);
                            assertContainsDuplicate(data.Payload, "id", 2);
                            assertContainsDuplicate(data.Payload, "Message", "log message 1");
                            assertContainsDuplicate(data.Payload, "EventId", 0);
                            assertContainsDuplicate(data.Payload, "Scope", "scope 2");
                        });

                        logger.LogInformation("log message {id}, {Message}, {EventId}, {Scope}",
                            expectedPayload["id"], expectedPayload["Message"], expectedPayload["EventId"], expectedPayload["Scope"]);
                    }
                }
            }
        }

        private void assertContainsDuplicate(IDictionary<string, object> payload, string keyPrefix, object expectedValue)
        {
            var duplicates = payload.Keys.Where(k => k.StartsWith("keyPrefix") && k != keyPrefix).ToArray();
            Assert.Equal(1, duplicates.Length);
            Assert.Equal(expectedValue, payload[duplicates.First()]);
        }

        private DiagnosticPipeline createPipeline(LoggerInput input, IHealthReporter reporter)
        {
            return new DiagnosticPipeline(reporter, new[] { input }, null, new[] { new EventSink(new FakeOutput(), null) });
        }

        private bool checkEventData(
            EventData actualData, 
            string expectedMessage,
            string expectedSource,
            LogLevel expectedLevel,
            EventId expectedId,
            Exception expectedException, 
            Dictionary<string, object> expectedScope)
        {
            Assert.NotNull(actualData);
            Assert.Equal(expectedMessage, actualData.Payload["Message"]);
            Assert.Equal(expectedSource, actualData.ProviderName);
            Assert.Equal(expectedLevel, actualData.Level);
            Assert.Equal(expectedId.Id, actualData.Payload["EventId"]);
            if (expectedId.Name != null)
                Assert.Equal(expectedId.Name, actualData.Payload["EventName"]);
            if (expectedException != null)
            {
                Assert.NotNull(actualData.Payload["Exception"] as Exception);
                Assert.Equal(expectedException.GetType(), actualData.Payload["Exception"].GetType());
                Assert.Equal(expectedException.Message, ((Exception) actualData.Payload["Exception"]).Message);
            }
            if (expectedScope != null)
            {
                foreach (var kv in expectedScope)
                {
                    Assert.Contains(kv, actualData.Payload);
                }
            }
            return true;
        }

        private class FakeOutput : IOutput
        {
            public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }

    public static class LoggerExtensions
    {
        public static IDisposable BeginScope<TState>(this ILogger logger, TState state)
        {
            return logger.BeginScope(state);
        }

        public static void LogNone(this ILogger logger, string message)
        {
            logger.Log(Extensions.Logging.LogLevel.None, 0, message, null, (state, exception) => "");
        }
    }
}

