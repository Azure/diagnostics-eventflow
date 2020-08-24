// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Net;
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

        [Fact]
        public void SerializesIPAddress()
        {
            var ipAddress = IPAddress.Parse("127.0.0.1");

            var obj = new
            {
                IpAddress = ipAddress
            };

            // ensure assumption that IPAddress cannot be serialized with default JSON settings
            Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(obj));

            string objString = JsonConvert.SerializeObject(obj, EventFlowJsonUtilities.GetDefaultSerializerSettings());

            Assert.Equal(
            @"{
                ""IpAddress"":""127.0.0.1""
            }".RemoveAllWhitespace(), objString);
        }

        [Fact]
        public void SerializesIPEndPoint()
        {
            var ipAddress = IPAddress.Parse("127.0.0.1");
            var endpoint = new IPEndPoint(ipAddress, 5000);

            var obj = new
            {
                Endpoint = endpoint
            };

            // ensure assumption that IPEndPoint cannot be serialized with default JSON settings
            Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(obj));

            string objString = JsonConvert.SerializeObject(obj, EventFlowJsonUtilities.GetDefaultSerializerSettings());

            Assert.Equal(
            @"{
                ""Endpoint"":""127.0.0.1:5000""
            }".RemoveAllWhitespace(), objString);
        }
    }
}
