// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Inputs.Tests
{
    public class EventSourceInput
    {
#if NET46
        [Fact(DisplayName = "EventSourceInput constructor should create instance")]
        public void ConstructorShouldCreateInstance()
        {
            // Setup
            Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
            Mock<IHealthReporter> healthReporterMock = new Mock<IHealthReporter>();
            // Execute
            ObservableEventListener target = new ObservableEventListener(
                configurationMock.Object,
                healthReporterMock.Object);

            // Verify
            Assert.NotNull(target);
        }
#endif
    }
}
