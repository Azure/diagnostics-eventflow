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

using Microsoft.Diagnostics.EventFlow.Metadata;

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
                            new Dictionary<string, object> { { "Scope", state.ToString() }, { "number", 1 } }))), Times.Exactly(1));
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
            EventData savedData = null;
            subject.Setup(s => s.OnNext(It.IsAny<EventData>())).Callback((EventData data) => savedData = data);

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
                        logger.LogInformation("log message {id}, {Message}, {EventId}, {Scope}",
                            expectedPayload["id"], expectedPayload["Message"], expectedPayload["EventId"], expectedPayload["Scope"]);

                        subject.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(1));

                        // Ilogger eventId and formatted message win over other properties
                        Assert.Equal("log message 1, message, 9, scope", savedData.Payload["Message"]);
                        Assert.Equal(0, savedData.Payload["EventId"]);
                        assertContainsDuplicate(savedData.Payload, "Message", expectedPayload["Message"]);
                        assertContainsDuplicate(savedData.Payload, "EventId", expectedPayload["EventId"]);

                        // Log property win over scope property
                        Assert.Equal(expectedPayload["id"], savedData.Payload["id"]);
                        Assert.Equal(expectedPayload["Scope"], savedData.Payload["Scope"]);
                        assertContainsDuplicate(savedData.Payload, "id", 2);
                        assertContainsDuplicate(savedData.Payload, "Scope", "scope 2");
                    }
                }
            }
        }

        [Fact]
        public void LoggerShouldSubmitContextWithNestedScope()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            EventData savedData = null;
            subject.Setup(s => s.OnNext(It.IsAny<EventData>())).Callback((EventData data) => savedData = data);

            using (LoggerInput target = new LoggerInput(healthReporterMock.Object))
            {
                var diagnosticPipeline = createPipeline(target, healthReporterMock.Object);
                using (target.Subscribe(subject.Object))
                {
                    var factory = new LoggerFactory();
                    factory.AddEventFlow(diagnosticPipeline);
                    var logger = new Logger<LoggerInputTests>(factory);

                    using (logger.BeginScope("scope {prop1}", "value1"))
                    {
                        logger.LogInformation("First level");

                        subject.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(1));
                        Assert.True(savedData.Payload.Contains(new KeyValuePair<string, object>("prop1", "value1")));
                        Assert.True(!savedData.Payload.ContainsKey("prop2"));

                        using (logger.BeginScope("scope2 {prop2}", "value2"))
                        {
                            logger.LogInformation("Second level");

                            subject.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(2));
                            Assert.True(savedData.Payload.Contains(new KeyValuePair<string, object>("prop1", "value1")));
                            Assert.True(savedData.Payload.Contains(new KeyValuePair<string, object>("prop2", "value2")));
                        }

                        logger.LogInformation("First level again");

                        subject.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(3));
                        Assert.True(savedData.Payload.Contains(new KeyValuePair<string, object>("prop1", "value1")));
                        Assert.True(!savedData.Payload.ContainsKey("prop2"));
                    }
                }
            }
        }

        [Fact]
        public void LoggerShouldHandleNestedScopeRunInParallel()
        {
            AutoResetEvent waitOnTask1 = new AutoResetEvent(false);
            AutoResetEvent waitOnTask2 = new AutoResetEvent(false);

            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            EventData savedData = null;
            subject.Setup(s => s.OnNext(It.IsAny<EventData>())).Callback((EventData data) => savedData = data);

            using (LoggerInput target = new LoggerInput(healthReporterMock.Object))
            {
                var diagnosticPipeline = createPipeline(target, healthReporterMock.Object);
                using (target.Subscribe(subject.Object))
                {
                    var factory = new LoggerFactory();
                    factory.AddEventFlow(diagnosticPipeline);
                    var logger = new Logger<LoggerInputTests>(factory);

                    // The two tasks are running in the following sequence, but the scope properties are local to the execution path.
                    //      Begin scope 1 outer
                    //      Begin scope 2 outer
                    //      Begin scope 2 inner
                    //      Begin scope 1 inner
                    //      Log and verify task1
                    //      Log and verify task2
                    var task1 = Task.Run(() =>
                    {
                        using (logger.BeginScope("scope1 outer"))
                        {
                            waitOnTask1.Set();
                            waitOnTask2.WaitOne();
                            using (logger.BeginScope("scope1 inner"))
                            {
                                logger.LogInformation("task1 information");

                                subject.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(1));
                                var scopeProperties = savedData.Payload.Where(kvp => kvp.Key.StartsWith("Scope")).ToArray();
                                Assert.True(scopeProperties.Length == 1);
                                Assert.True((string)scopeProperties[0].Value == "scope1 outer\r\nscope1 inner");

                                waitOnTask1.Set();
                            }
                        }
                    });

                    var task2 = Task.Run(() =>
                    {
                        waitOnTask1.WaitOne();
                        using (logger.BeginScope("scope2 outer"))
                        {
                            using (logger.BeginScope("scope2 inner"))
                            {
                                waitOnTask2.Set();
                                waitOnTask1.WaitOne();

                                logger.LogInformation("task2 information");

                                subject.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(2));
                                var scopeProperties = savedData.Payload.Where(kvp => kvp.Key.StartsWith("Scope")).ToArray();
                                Assert.True(scopeProperties.Length == 1);
                                Assert.True((string)scopeProperties[0].Value == "scope2 outer\r\nscope2 inner");
                            }
                        }
                    });

                    Task.WaitAll(task1, task2);
                }
            }
        }

        private void assertContainsDuplicate(IDictionary<string, object> payload, string keyPrefix, object expectedValue)
        {
            var duplicates = payload.Keys.Where(k => k.StartsWith(keyPrefix) && k != keyPrefix).ToArray();
            Assert.Single(duplicates);
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
            {
                Assert.Equal(expectedId.Name, actualData.Payload["EventName"]);
            }

            if (expectedException != null)
            {
                Assert.NotNull(actualData.Payload["Exception"] as Exception);
                Assert.Equal(expectedException.GetType(), actualData.Payload["Exception"].GetType());
                Assert.Equal(expectedException.Message, ((Exception) actualData.Payload["Exception"]).Message);
                Assert.True(actualData.TryGetMetadata(ExceptionData.ExceptionMetadataKind, out IReadOnlyCollection<EventMetadata> metadataCollection));
                var exceptionMetadata = metadataCollection.Single();
                Assert.Same(expectedException, actualData.Payload[exceptionMetadata.Properties[ExceptionData.ExceptionPropertyMoniker]]);
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

