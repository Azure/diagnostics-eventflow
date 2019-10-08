// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
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

            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            {
                target.CreateItem(null, healthReporterMock.Object);
            });
            Assert.Equal("configuration", ex.ParamName);
        }

        [Fact]
        public void CreateShouldRequireHealthReporter()
        {
            var configurationMock = new Mock<IConfigurationSection>();

            TraceInputFactory target = new TraceInputFactory();

            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => {
                target.CreateItem(configurationMock.Object, null);
            });
            Assert.Equal("healthReporter", ex.ParamName);
        }
    }
}
