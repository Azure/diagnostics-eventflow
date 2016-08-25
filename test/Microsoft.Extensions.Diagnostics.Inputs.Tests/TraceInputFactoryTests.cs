using System;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Inputs.Tests
{
    public class TraceInputFactoryTests
    {
        [Fact]
        public void ShouldHaveADefaultConstructor()
        {
            TraceInputFactory factory = new TraceInputFactory();
            Assert.NotNull(factory);
        }

        [Fact]
        public void CreateShouldRequireConfiguration()
        {
            var healthReporterMock = new Mock<IHealthReporter>();

            TraceInputFactory target = new TraceInputFactory();

            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                target.CreateItem(null, healthReporterMock.Object);
            });
            Assert.Equal("Value cannot be null.\r\nParameter name: configuration", ex.Message);
        }

        [Fact]
        public void CreateShouldRequireHealthReporter()
        {
            var configurationMock = new Mock<IConfigurationSection>();

            TraceInputFactory target = new TraceInputFactory();

            Exception ex = Assert.Throws<ArgumentNullException>(() => {
                target.CreateItem(configurationMock.Object, null);
            });
            Assert.Equal("Value cannot be null.\r\nParameter name: healthReporter", ex.Message);
        }
    }
}
