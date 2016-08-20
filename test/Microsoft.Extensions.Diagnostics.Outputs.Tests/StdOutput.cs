using System;
using Microsoft.Extensions.Diagnostics.Outputs;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Outputs.Tests
{
    public class StdOutput
    {
        [Fact(DisplayName = "StdOutput constructor should create the instance")]
        public void ConstructorShouldCreateTheInstance()
        {
            // Setup
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();

            // Execute
            StdSender sender = new StdSender(healthReporterMock.Object);

            // Verify
            Assert.NotNull(sender);
        }

        [Fact(DisplayName = "StdOutput constructor should require a health reporter")]
        public void ConstructorShouldRequireHealthReporter()
        {
            Exception ex = Assert.Throws<ArgumentNullException>(() =>
            {
                StdSender target = new StdSender(null);
            });

            Assert.Equal("Value cannot be null.\r\nParameter name: healthReporter", ex.Message);
        }
    }
}
