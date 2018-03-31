// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.FilterEvaluators;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests.FilterParsing
{
    public class BitwiseEqualityEvaluatorTests
    {
        private readonly FilterParser parser = new FilterParser();

        [Fact]
        public void MissingPropertyEquality()
        {
            var evaluator = new EqualityEvaluator("InvalidPropertyName", "0xFF");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));
        }

        [Fact]
        public void BitwiseEqualityNonHexFormat()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, 0xF3);

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "3");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "4");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void BitwiseEqualityHexFormat()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, 0xF3);

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0x03");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xA4");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void BitwiseInequalityByNegation()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, 0xF3);
            
            var evaluator = new NegationEvaluator(new BitwiseEqualityEvaluator(tempPropertyName, "0x03"));
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new NegationEvaluator(new BitwiseEqualityEvaluator(tempPropertyName, "0xA4"));
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void IntPropertyEquality()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, int.Parse("FFFFABCD", System.Globalization.NumberStyles.HexNumber));

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFABC0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFABCE");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void LongPropertyEquality()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, long.Parse("FFFFABCDFFFFABCD", System.Globalization.NumberStyles.HexNumber));

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFABC0FFFFABC0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFABCEFFFFABCE");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload[tempPropertyName] = long.MaxValue;
            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0x0FFFFFFFFFFFFFFF");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFFFFFFFFFFFFF");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload[tempPropertyName] = long.MinValue;
            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0x8000000000000000");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0x8000000000000001");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void ShortPropertyEquality()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, short.Parse("FFBF", System.Globalization.NumberStyles.HexNumber));

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFBF");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFF");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void SbytePropertyEquality()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, sbyte.Parse("F3", System.Globalization.NumberStyles.HexNumber));

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xF3");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xF5");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void UintPropertyEquality()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, uint.Parse("FFFFABCD", System.Globalization.NumberStyles.HexNumber));

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFABC0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFABCE");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void UlongPropertyEquality()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, ulong.Parse("FFFFABCDFFFFABCD", System.Globalization.NumberStyles.HexNumber));

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFABC0FFFFABC0");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFABCEFFFFABCE");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload[tempPropertyName] = ulong.MaxValue;
            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFFFFFFFFFFFFFF");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void UshortPropertyEquality()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, ushort.Parse("FFBF", System.Globalization.NumberStyles.HexNumber));

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFBF");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xFFFF");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }

        [Fact]
        public void BytePropertyEquality()
        {
            var tempPropertyName = Guid.NewGuid().ToString();
            FilteringTestData.ManyPropertiesEvent.Payload.Add(tempPropertyName, byte.Parse("F3", System.Globalization.NumberStyles.HexNumber));

            var evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xF3");
            Assert.True(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            evaluator = new BitwiseEqualityEvaluator(tempPropertyName, "0xF5");
            Assert.False(evaluator.Evaluate(FilteringTestData.ManyPropertiesEvent));

            FilteringTestData.ManyPropertiesEvent.Payload.Remove(tempPropertyName);
        }
    }
}
