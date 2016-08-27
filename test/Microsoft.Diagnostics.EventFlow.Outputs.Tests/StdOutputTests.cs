using System;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class StdOutputTests
    {
        [Fact]
        public void ConstructorShouldCreateTheInstance()
        {
            // Setup
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();

            // Execute
            StdOutput sender = new StdOutput(healthReporterMock.Object);

            // Verify
            Assert.NotNull(sender);
        }

        [Fact]
        public void ConstructorShouldRequireHealthReporter()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                StdOutput target = new StdOutput(null);
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: healthReporter", ex.Message);
        }
    }
}
