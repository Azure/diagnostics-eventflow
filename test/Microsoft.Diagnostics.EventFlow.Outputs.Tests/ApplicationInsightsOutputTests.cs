// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Tests
{
    public class ApplicationInsightsOutputTests
    {
        [Fact]
        public void UsesIsoDateFormat()
        {
            EventData e = new EventData();
            e.Payload.Add("DateTimeProperty", new DateTime(2017, 4, 19, 10, 15, 23, DateTimeKind.Utc));
            e.Payload.Add("DateTimeOffsetProperty", new DateTimeOffset(2017, 4, 19, 10, 16, 07, TimeSpan.Zero));

            var healthReporterMock = new Mock<IHealthReporter>();
            var config = new ApplicationInsightsOutputConfiguration();
            var aiOutput = new ApplicationInsightsOutput(config, healthReporterMock.Object);
            var propertyBag = new PropertyBag();

            aiOutput.AddProperties(propertyBag, e);

            var dateTimeRegex = new Regex("2017-04-19T10:15:23(\\.0+)?Z", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            Assert.Matches(dateTimeRegex, propertyBag.Properties["DateTimeProperty"]);

            var dateTimeOffsetRegex = new Regex("2017-04-19T10:16:07(\\.0+)?\\+00:00", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            Assert.Matches(dateTimeOffsetRegex, propertyBag.Properties["DateTimeOffsetProperty"]);
        }

        private class PropertyBag: ISupportProperties
        {
            public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();
        }
    }
}
