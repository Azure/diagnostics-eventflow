using System;
using System.Diagnostics;
using System.IO;
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

            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                using (TraceInput target = new TraceInput(null, healthReporterMock.Object)) { }
            });
            Assert.Equal("Value cannot be null.\r\nParameter name: configuration", ex.Message);
        }

        [Fact]
        public void ConstructorShouldRequireHealthReporter()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                using (TraceInput target = new TraceInput(configurationMock.Object, null)) { }
            });
            Assert.Equal("Value cannot be null.\r\nParameter name: healthReporter", ex.Message);
        }

        [Fact]
        public void ConstructorShouldCheckConfigurationSectionType()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            var healthReporterMock = new Mock<IHealthReporter>();
            TraceInput target = null;
            using (target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
            {
                Assert.NotNull(target);
            }

            configurationMock.Setup(section => section["type"]).Returns("NonTrace");

            Exception ex = Assert.ThrowsAny<Exception>(() =>
            {
                using (target = new TraceInput(configurationMock.Object, healthReporterMock.Object)) { }
            });
            Assert.Equal("Invalid trace configuration", ex.Message);
        }

        [Fact]
        public void TraceShouldSubmitTheData()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                Trace.TraceInformation("Message for unit test");
                subject.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceShouldNotSubmitFilteredData()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("Off");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subjectMock = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
            using (target.Subscribe(subjectMock.Object))
            {
                Trace.TraceInformation("Message for unit test");
                subjectMock.Verify(s => s.OnNext(It.IsAny<EventData>()), Times.Exactly(0));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideWriteLine()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
            using (target.Subscribe(subject.Object))
            {
                target.WriteLine("UnitTest info");
                subject.Verify(s => s.OnNext(It.Is<EventData>(data => data.Payload["Message"].Equals("UnitTest info"))), Times.Exactly(1));
            }
        }

        [Fact]
        public void TraceInputShouldOverrideFail()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
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
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
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
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
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
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
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
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
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
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
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

#if NET46
        [Fact]
        public void TraceInputShouldOverrideTraceTransfer()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(section => section["type"]).Returns("Trace");
            configurationMock.Setup(section => section["traceLevel"]).Returns("All");
            var healthReporterMock = new Mock<IHealthReporter>();
            var subject = new Mock<IObserver<EventData>>();
            using (TraceInput target = new TraceInput(configurationMock.Object, healthReporterMock.Object))
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
#endif

        private static string GetRandomString()
        {
            string path = Path.GetRandomFileName();
            path = path.Replace(".", "");
            return path;
        }
    }
}
