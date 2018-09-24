// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Moq;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class EventDataTests
    {
        [Fact]
        public void GetPropertyValueCoverAllCommonProperty()
        {
            var data = new EventData();
            var props = data.GetType().GetProperties();
            object dummy;

            foreach(var prop in props)
            {
                if (prop.Name != "Payload")
                {
                    Assert.True(data.TryGetPropertyValue(prop.Name, out dummy));
                }
            }
        }

        [Fact]
        public void GetPropertyValueGetValueFromPayload()
        {
            var data = new EventData();
            data.Payload.Add("prop", "value");

            object value;
            Assert.True(data.TryGetPropertyValue("prop", out value));
            Assert.Equal("value", value);
        }

        [Fact]
        public void AddPayloadPropertyDoesNotAddDuplicatesIfKeyAndValueEquals()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var data = new EventData();
            data.AddPayloadProperty("prop", "value", healthReporterMock.Object, "tests");
            data.AddPayloadProperty("prop", "value", healthReporterMock.Object, "tests");

            Assert.Equal(1, data.Payload.Keys.Count);
            object value;
            Assert.True(data.TryGetPropertyValue("prop", out value));
            Assert.Equal("value", value);
            healthReporterMock.Verify(m => m.ReportWarning(It.Is<string>(s => s.StartsWith("The property with the key 'prop' already exist in the event payload with equivalent value")), It.IsIn("tests")));
        }

        [Fact]
        public void AddPayloadPropertyDoesNotAddDuplicatesIfKeyAndValueEqualsAtIndexedValues()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var data = new EventData();
            data.AddPayloadProperty("prop", "value1", healthReporterMock.Object, "tests");
            data.AddPayloadProperty("prop", "value2", healthReporterMock.Object, "tests");
            data.AddPayloadProperty("prop", "value3", healthReporterMock.Object, "tests");
            data.AddPayloadProperty("prop", "value3", healthReporterMock.Object, "tests");

            Assert.Equal(3, data.Payload.Keys.Count);
            object value;
            Assert.True(data.TryGetPropertyValue("prop", out value));
            Assert.Equal("value1", value);
            Assert.True(data.TryGetPropertyValue("prop_1", out value));
            Assert.Equal("value2", value);
            Assert.True(data.TryGetPropertyValue("prop_2", out value));
            Assert.Equal("value3", value);
            healthReporterMock.Verify(m => m.ReportWarning(It.Is<string>(s => s.StartsWith("The property with the key 'prop' already exist in the event payload with equivalent value under key 'prop_2'")), It.IsIn("tests")));
        }

        [Fact]
        public void AddPayloadPropertyAddsIndexedValuesIfKeyEqualsButValueDiffers()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var data = new EventData();
            data.AddPayloadProperty("prop", "value1", healthReporterMock.Object, "tests");
            data.AddPayloadProperty("prop", "value2", healthReporterMock.Object, "tests");

            Assert.Equal(2, data.Payload.Keys.Count);
            object value;
            Assert.True(data.TryGetPropertyValue("prop", out value));
            Assert.Equal("value1", value);

            Assert.True(data.TryGetPropertyValue("prop_1", out value));
            Assert.Equal("value2", value);
        }

        [Fact]
        public void AddPayloadPropertyAddsIndexedValuesIfKeyEqualsButInitialValueIsNull()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var data = new EventData();
            data.AddPayloadProperty("prop", null, healthReporterMock.Object, "tests");
            data.AddPayloadProperty("prop", "value2", healthReporterMock.Object, "tests");

            Assert.Equal(2, data.Payload.Keys.Count);
            object value;
            Assert.True(data.TryGetPropertyValue("prop", out value));
            Assert.Null(value);

            Assert.True(data.TryGetPropertyValue("prop_1", out value));
            Assert.Equal("value2", value);
        }

        [Fact]
        public void AddPayloadPropertyAddsIndexedValuesIfKeyEqualsButSecondValueIsNull()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var data = new EventData();
            data.AddPayloadProperty("prop", "value1", healthReporterMock.Object, "tests");
            data.AddPayloadProperty("prop", null, healthReporterMock.Object, "tests");

            Assert.Equal(2, data.Payload.Keys.Count);
            object value;
            Assert.True(data.TryGetPropertyValue("prop", out value));
            Assert.Equal("value1", value);

            Assert.True(data.TryGetPropertyValue("prop_1", out value));
            Assert.Null(value);
        }
    }
}
