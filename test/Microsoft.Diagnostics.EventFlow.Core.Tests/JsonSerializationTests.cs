// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;
using Xunit;

using Microsoft.Diagnostics.EventFlow.TestHelpers;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests
{
    public class JsonSerializationTests
    {
        [Fact]
        public void SerializesMethodInfoAsFullyQualifiedName()
        {
            var obj = new
            {
                Name = "foo",
                Method = typeof(JsonSerializationTests).GetMethod(nameof(SerializesMethodInfoAsFullyQualifiedName))
            };

            string objString = JsonConvert.SerializeObject(obj, EventFlowJsonUtilities.GetDefaultSerializerSettings());

            Assert.Equal(
            @"{
                ""Name"":""foo"",
                ""Method"":""Microsoft.Diagnostics.EventFlow.Core.Tests.JsonSerializationTests.SerializesMethodInfoAsFullyQualifiedName""
            }".RemoveAllWhitespace(), objString);
        }
    }
}
