﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class GreaterOrEqualsEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void IdPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("EventId", "1234");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("EventId", "-34");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("EventId", "1235");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("EventId", "notIntegerValue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MessagePropertyGreaterOrEquals()
        {
            var evaluator = new GreaterOrEqualsEvaluator("Message", "Test event with many properties of different types");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("Message", "aaa");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("Message", "zzz");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void TimestampPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("Timestamp", "2015-05-29T10:45:00.537Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same timestamp
            evaluator = new GreaterOrEqualsEvaluator("Timestamp", "2015-05-29T10:45:00.537+00:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("Timestamp", "2015-05-29T10:50:00Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("Timestamp", "2015-05-29T10:45:00Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("Timestamp", "not a timestamp value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void MissingPropertyGreaterOrEquals()
        {
            var evaluator = new GreaterOrEqualsEvaluator("InvalidPropertyName", "somevalue");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void StringPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("StringProperty", "Ala ma kota");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Comparison should be case-insensitive
            evaluator = new GreaterOrEqualsEvaluator("StringProperty", "Ala MA kota");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Greater value
            evaluator = new GreaterOrEqualsEvaluator("StringProperty", "Kota ma Ala");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("StringProperty", "AAla");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void IntPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("IntProperty", "-65000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("IntProperty", "65000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("IntProperty", "-65001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("IntProperty", "not an int value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void LongPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("LongProperty", "-5000000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("LongProperty", "5000000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("LongProperty", "-5000000001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("LongProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("LongProperty", "not a long value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void ShortPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("ShortProperty", "-18000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("ShortProperty", "18000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("ShortProperty", "-18001");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("ShortProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for short values
            evaluator = new GreaterOrEqualsEvaluator("ShortProperty", "70000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void SbytePropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("SbyteProperty", "-20");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("SbyteProperty", "20");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("SbyteProperty", "-21");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("SbyteProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new GreaterOrEqualsEvaluator("SbyteProperty", "234");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UintPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("UintProperty", "80000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("UintProperty", "80001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("UintProperty", "70500");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("UintProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for uint
            evaluator = new GreaterOrEqualsEvaluator("UintProperty", "-80000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UlongPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("UlongProperty", "5100000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("UlongProperty", "5100000001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("UlongProperty", "5000000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("UlongProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new GreaterOrEqualsEvaluator("UlongProperty", "-5100000000");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void UshortPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("UshortProperty", "18200");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("UshortProperty", "18201");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("UshortProperty", "18199");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("UshortProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new GreaterOrEqualsEvaluator("UshortProperty", "-18200");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BytePropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("ByteProperty", "7");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("ByteProperty", "8");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("ByteProperty", "3");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("ByteProperty", "0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range for byte
            evaluator = new GreaterOrEqualsEvaluator("ByteProperty", "-3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void FloatPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("FloatProperty", "-7.4");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new GreaterOrEqualsEvaluator("FloatProperty", "-74E-1");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("FloatProperty", "74E-1");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("FloatProperty", "-7.5");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("FloatProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Out of range
            evaluator = new GreaterOrEqualsEvaluator("FloatProperty", "-2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DoublePropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("DoubleProperty", "-2.347E43");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Different representation of the same number
            evaluator = new GreaterOrEqualsEvaluator("DoubleProperty", "-234.7E41");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("DoubleProperty", "2.347E43");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("DoubleProperty", "-2.3471E43");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Zero as special case
            evaluator = new GreaterOrEqualsEvaluator("DoubleProperty", "0");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("DoubleProperty", "not a double value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimePropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("DateTimeProperty", "2015-03-30T09:15:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new GreaterOrEqualsEvaluator("DateTimeProperty", "2015-03-30 09:15:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("DateTimeProperty", "2015-03-30T09:15:01");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("DateTimeProperty", "2015-03-30T09:14:59");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("DateTimeProperty", "not a DateTime value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void DateTimeOffsetPropertyGreaterOrEquals()
        {
            // Same value
            var evaluator = new GreaterOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Alternative representation of the same value
            evaluator = new GreaterOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:00");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value in different time zone
            evaluator = new GreaterOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17.485+00:01");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Lower value
            evaluator = new GreaterOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:17Z");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            // Higher value
            evaluator = new GreaterOrEqualsEvaluator("DateTimeOffsetProperty", "2015-05-29T10:39:18Z");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("DateTimeOffsetProperty", "not a DateTimeOffset value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BoolPropertyGreaterOrEquals()
        {
            // Bool properties do not work with greater-or-equals operator, so no matter what RHS value we use the evaluation should always be false.
            var evaluator = new GreaterOrEqualsEvaluator("BoolProperty", "true");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("BoolProperty", "false");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("BoolProperty", "not a bool value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void GuidPropertyGreaterOrEquals()
        {
            // GUID properties do not work with greater-or-equals operator, so no matter what RHS value we use the evaluation should always be false.
            var evaluator = new GreaterOrEqualsEvaluator("GuidProperty", "8DCE9920-E985-4B63-8ECE-A22160421FA3");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("GuidProperty", "{8DCE9920-E985-4B63-8ECE-A22160421FA3}");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("GuidProperty", "67FED6E7-23C5-45DB-96DE-F763E169C922");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new GreaterOrEqualsEvaluator("GuidProperty", "not a GUID value");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        // Ensure that expressions using only "time" portion of the timestamp are compared using today's date as a default.            
        [Fact]
        public void PartialTimestampTest()
        {
            DateTime timestamp = DateTime.Now;
            if (timestamp.Hour == 0 && timestamp.Minute <= 5)
            {
                // The range of timestamps checked is about 4 minutes, so the test will fail if it is shortly past midnight. 
                // In that case we simply do not perform the test, to avoid false negatives.

                // The test is using current machine time and cannot produce a result within 5 minutes past midnight local time;
                return;
            }

            string timestampString = timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var evaluator = new GreaterOrEqualsEvaluator("TwoMinutesAgoProperty", timestampString);
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            timestamp = DateTime.Now - TimeSpan.FromMinutes(4);
            timestampString = timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            evaluator = new GreaterOrEqualsEvaluator("TwoMinutesAgoProperty", timestampString);
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }
    }
}
