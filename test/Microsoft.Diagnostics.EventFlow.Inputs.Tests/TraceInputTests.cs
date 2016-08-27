using System;
using System.Diagnostics;
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
    }
}
