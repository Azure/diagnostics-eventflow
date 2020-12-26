// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Microsoft.Diagnostics.EventFlow.JsonConverters
{
#if NET471 || NETSTANDARD2_0
    public class MemberInfoConverter : JsonConverter<MemberInfo>
#else
    public class MemberInfoConverter : JsonConverter
#endif
    {

#if NET452 || NETSTANDARD1_6
        public override bool CanConvert(Type objectType)
        {
            return typeof(MemberInfo).GetTypeInfo().IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null && !(value is MemberInfo))
            {
                throw new JsonSerializationException($"{nameof(MemberInfoConverter)} cannot convert given value to JSON");
            }

            this.WriteJson(writer, (MemberInfo)value, serializer);
        }
#endif

#if NET471 || NETSTANDARD2_0
        public override MemberInfo ReadJson(JsonReader reader, Type objectType, MemberInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
#else
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
#endif
        {
            throw new NotImplementedException();
        }

#if NET471 || NETSTANDARD2_0
        public override void WriteJson(JsonWriter writer, MemberInfo value, JsonSerializer serializer)
#else
        public void WriteJson(JsonWriter writer, MemberInfo value, JsonSerializer serializer)
#endif
        {
            if (value == null)
            {
                writer.WriteValue(string.Empty);
                return;
            }

            string fullName;
#if NETSTANDARD1_6
            Type parentType = value.DeclaringType;
#else
            Type parentType = value.ReflectedType;
#endif

            if (parentType == null)
            {
                fullName = value.Name;
            }
            else
            {
                fullName = parentType.FullName + "." + value.Name;
            }

            writer.WriteValue(fullName);
        }
    }
}
