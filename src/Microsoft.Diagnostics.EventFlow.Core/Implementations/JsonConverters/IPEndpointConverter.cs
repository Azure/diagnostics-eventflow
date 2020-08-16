// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Net;
#if NET451 || NETSTANDARD1_6
using System.Reflection;
#endif
using Newtonsoft.Json;

namespace Microsoft.Diagnostics.EventFlow.JsonConverters
{
    internal class IPEndpointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
#if NET451 || NETSTANDARD1_6
            return typeof(EndPoint).GetTypeInfo().IsAssignableFrom(objectType);
#else
            return typeof(EndPoint).IsAssignableFrom(objectType);
#endif
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // this converter used only for Serialization
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

}
