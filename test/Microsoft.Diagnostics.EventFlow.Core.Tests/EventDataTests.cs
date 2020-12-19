// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;

using Moq;
using Xunit;

using Microsoft.Diagnostics.EventFlow.TestHelpers;

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
            healthReporterMock.Verify(m => m.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

        [Fact]
        public void MatchingTypePropertyValueQueriesDoNotUseExceptions()
        {
            var data = new ManyPropertiesEventData();

            using (var fceCounter = new FirstChanceExceptionCounter(typeof(InvalidCastException)))
            {
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.BoolProperty, (bool v) => Assert.Equal(ManyPropertiesEventData.ExpectedBoolValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.DateTimeProperty, (DateTime v) => Assert.Equal(ManyPropertiesEventData.ExpectedDateTimeValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.DateTimeOffsetProperty, (DateTimeOffset v) => Assert.Equal(ManyPropertiesEventData.ExpectedDateTimeOffsetValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.GuidProperty, (Guid v) => Assert.Equal(ManyPropertiesEventData.ExpectedGuidValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.TimeSpanProperty, (TimeSpan v) => Assert.Equal(ManyPropertiesEventData.ExpectedTimeSpanValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.FloatProperty, (float v) => Assert.Equal(ManyPropertiesEventData.ExpectedFloatValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.DoubleProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedDoubleValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ByteProperty, (byte v) => Assert.Equal(ManyPropertiesEventData.ExpectedByteValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ShortProperty, (short v) => Assert.Equal(ManyPropertiesEventData.ExpectedShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UShortProperty, (ushort v) => Assert.Equal(ManyPropertiesEventData.ExpectedUShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.IntProperty, (int v) => Assert.Equal(ManyPropertiesEventData.ExpectedIntValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UIntProperty, (uint v) => Assert.Equal(ManyPropertiesEventData.ExpectedUIntValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.LongProperty, (long v) => Assert.Equal(ManyPropertiesEventData.ExpectedLongValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ULongProperty, (ulong v) => Assert.Equal(ManyPropertiesEventData.ExpectedULongValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.StringProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedStringValue, v)));

                Assert.Equal(0, fceCounter.Count);
            }
        }

        [Fact]
        public void StringConversionsDoNotUseExceptions()
        {
            var data = new ManyPropertiesEventData();
            var initialCulture = CultureInfo.CurrentCulture;

            using (new Disposable(() => CultureInfo.CurrentCulture = initialCulture, () => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture))
            using (var fceCounter = new FirstChanceExceptionCounter(typeof(InvalidCastException)))            
            {
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.BoolProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedBoolValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.DateTimeProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedDateTimeValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.DateTimeOffsetProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedDateTimeOffsetValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.GuidProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedGuidValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.TimeSpanProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedTimeSpanValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.FloatProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedFloatValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.DoubleProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedDoubleValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ByteProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedByteValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ShortProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedShortValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UShortProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedUShortValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.IntProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedIntValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UIntProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedUIntValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.LongProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedLongValue.ToString(), v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ULongProperty, (string v) => Assert.Equal(ManyPropertiesEventData.ExpectedULongValue.ToString(), v)));

                Assert.Equal(0, fceCounter.Count);
            }
        }

        [Fact]
        public void ImplicitNumericConversionsDoNotUseExceptions()
        {
            var data = new ManyPropertiesEventData();

            using (var fceCounter = new FirstChanceExceptionCounter(typeof(InvalidCastException)))
            {
                // Note: we don't handle ALL implicit numeric conversions (e.g. byte --> short still involves a failed cast), but the most commonly used types are covered.

                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.FloatProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedFloatValue, v)));

                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ByteProperty, (int v) => Assert.Equal(ManyPropertiesEventData.ExpectedByteValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ByteProperty, (uint v) => Assert.Equal(ManyPropertiesEventData.ExpectedByteValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ByteProperty, (long v) => Assert.Equal(ManyPropertiesEventData.ExpectedByteValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ByteProperty, (ulong v) => Assert.Equal(ManyPropertiesEventData.ExpectedByteValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ByteProperty, (float v) => Assert.Equal(ManyPropertiesEventData.ExpectedByteValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ByteProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedByteValue, v)));

                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ShortProperty, (int v) => Assert.Equal(ManyPropertiesEventData.ExpectedShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ShortProperty, (long v) => Assert.Equal(ManyPropertiesEventData.ExpectedShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ShortProperty, (float v) => Assert.Equal(ManyPropertiesEventData.ExpectedShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ShortProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedShortValue, v)));

                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UShortProperty, (int v) => Assert.Equal(ManyPropertiesEventData.ExpectedUShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UShortProperty, (uint v) => Assert.Equal(ManyPropertiesEventData.ExpectedUShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UShortProperty, (long v) => Assert.Equal(ManyPropertiesEventData.ExpectedUShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UShortProperty, (ulong v) => Assert.Equal(ManyPropertiesEventData.ExpectedUShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UShortProperty, (float v) => Assert.Equal(ManyPropertiesEventData.ExpectedUShortValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UShortProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedUShortValue, v)));

                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.IntProperty, (long v) => Assert.Equal(ManyPropertiesEventData.ExpectedIntValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.IntProperty, (float v) => Assert.Equal(ManyPropertiesEventData.ExpectedIntValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.IntProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedIntValue, v)));

                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UIntProperty, (long v) => Assert.Equal(ManyPropertiesEventData.ExpectedUIntValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UIntProperty, (ulong v) => Assert.Equal(ManyPropertiesEventData.ExpectedUIntValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UIntProperty, (float v) => Assert.Equal(ManyPropertiesEventData.ExpectedUIntValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.UIntProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedUIntValue, v)));

                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.LongProperty, (float v) => Assert.Equal(ManyPropertiesEventData.ExpectedLongValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.LongProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedLongValue, v)));

                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ULongProperty, (float v) => Assert.Equal(ManyPropertiesEventData.ExpectedULongValue, v)));
                Assert.True(data.GetValueFromPayload(ManyPropertiesEventData.ULongProperty, (double v) => Assert.Equal(ManyPropertiesEventData.ExpectedULongValue, v)));

                Assert.Equal(0, fceCounter.Count);
            }
        }
    }
}
