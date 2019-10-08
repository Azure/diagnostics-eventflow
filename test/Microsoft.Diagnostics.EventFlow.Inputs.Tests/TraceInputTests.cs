// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class TraceInputTests
    {
        [Fact]
        public void ConstructorShouldRequireConfiguration()
        {
            var healthReporterMock = new Mock<IHealthReporter>();

            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            {
                using (TraceInput target = new TraceInput((TraceInputConfiguration) null, healthReporterMock.Object)) { }
            });
            Assert.Equal("traceInputConfiguration", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(() =>
            {
                using (TraceInput target = new TraceInput((IConfiguration) null, healthReporterMock.Object)) { }
            });
            Assert.Equal("configuration", ex.ParamName);
        }

        [Fact]
        public void ConstructorShouldRequireHealthReporter()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            {
                using (TraceInput target = new TraceInput(configurationMock.Object, null)) { }
            });
            Assert.Equal("healthReporter", ex.ParamName);
        }

        [Fact]
        public void ConstructorShouldCheckConfigurationSectionType()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>() {
                ["type"] = "Trace"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            TraceInput target = null;
            using (target = new TraceInput(configuration, healthReporterMock.Object))
            {
                Assert.NotNull(target);
            }

            configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "NonTrace"
            }).Build();

            using (target = new TraceInput(configuration, healthReporterMock.Object))
            {
                healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.Is<string>(s => s == EventFlowContextIdentifiers.Configuration)), Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceShouldSubmitTheData()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                Trace.TraceInformation("Message for unit test");
                subject.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceShouldNotSubmitFilteredData()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "Off"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subjectMock = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subjectMock.Object))
            {
                Trace.TraceInformation("Message for unit test");
                subjectMock.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(0));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideWriteLine()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                target.WriteLine("UnitTest info");
                subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Payload["Message"].Equals("UnitTest info"))), Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideFail()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                string message = "Failure message";
                target.Fail(message);
                subject.Verify(s =>
                    s.OnNext(It.Is<EventData>(data => data.Payload["Message"].Equals(message) && data.Level == LogLevel.Error)),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideFailWithDetails()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                string message = "Failure message";
                string details = "Details";
                target.Fail(message, details);
                subject.Verify(s =>
                    s.OnNext(It.Is<EventData>(data => data.Payload["Message"].Equals(message + Environment.NewLine + details) && data.Level == LogLevel.Error)),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideTraceSingleData()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                string message = "Message";
                int id = (new Random()).Next();
                target.TraceData(null, null, TraceEventType.Warning, id, message);
                subject.Verify(s =>
                    s.OnNext(It.Is<EventData>(data => data.Payload["Message"].Equals(message) && data.Payload["EventId"].Equals(id))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideTraceMultipleData()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                string message = "Message";
                string message2 = "Message2";
                int id = (new Random()).Next();
                target.TraceData(null, null, TraceEventType.Warning, id, message, message2);
                subject.Verify(s =>
                    s.OnNext(It.Is<EventData>(data => data.Payload["Message"].Equals($"{message}, {message2}") && data.Payload["EventId"].Equals(id))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideTraceSingleEvent()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                string message = GetRandomString();
                int id = (new Random()).Next();
                target.TraceEvent(null, null, TraceEventType.Warning, id, message);
                subject.Verify(s =>
                    s.OnNext(It.Is<EventData>(data => data.Payload["Message"].Equals(message) && data.Payload["EventId"].Equals(id))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideTraceEventWithFormat()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                string message = GetRandomString();
                string format = "{0}_{0}";
                int id = (new Random()).Next();
                target.TraceEvent(null, null, TraceEventType.Warning, id, format, message);
                subject.Verify(s =>
                    s.OnNext(It.Is<EventData>(data => data.Payload["Message"].Equals($"{message}_{message}") && data.Payload["EventId"].Equals(id))),
                    Times.Exactly(1));
            }
        }

#if !NETCOREAPP1_0
        [Fact]
        public void TraceInputShouldOverrideTraceTransfer()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                string message = GetRandomString();
                int id = (new Random()).Next();
                Guid relatedId = Guid.NewGuid();
                target.TraceTransfer(null, null, id, message, relatedId);
                subject.Verify(s =>
                    s.OnNext(It.Is<EventData>(data => data.Payload["EventId"].Equals(id) && data.Payload["RelatedActivityID"].Equals(relatedId.ToString()))),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceInputTracksActivityID()
        {
            IConfiguration configuration = (new ConfigurationBuilder()).AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["type"] = "Trace",
                ["traceLevel"] = "All"
            }).Build();
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configuration, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                string message = GetRandomString();
                int id = (new Random()).Next();
                Guid activityID = Guid.NewGuid();
                Trace.CorrelationManager.ActivityId = activityID;

                target.TraceData(null, null, TraceEventType.Information, id, message);
                subject.Verify(s =>
                    s.OnNext(It.Is<EventData>(data => data.Payload["EventId"].Equals(id) && data.Payload["ActivityID"].Equals(activityID) && data.Payload["Message"].Equals(message))),
                    Times.Exactly(1));
            }
        }
#endif

        private static string GetRandomString()
        {
            string path = Path.GetRandomFileName();
            path = path.Replace(".", "");
            return path;
        }


        private class MyContext
        {
            public readonly string CorrelationId;

            public MyContext(string correlationId)
            {
                CorrelationId = correlationId;
            }
        }

        private bool checkContext(EventData data, string correlationId)
        {
            object context;
            if (data.TryGetPropertyValue("EventContext", out context))
            {
                var ctx = context as MyContext;
                if (ctx != null)
                {
                    return ctx.CorrelationId == correlationId;
                }
            }
            return false;
        }
    }
}
