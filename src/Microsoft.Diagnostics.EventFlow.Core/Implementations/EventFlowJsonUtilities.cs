// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.EventFlow
{
    public static class EventFlowJsonUtilities
    {
        private static Lazy<JsonSerializerSettings> settings = new Lazy<JsonSerializerSettings>(GetDefaultSerializerSettings, System.Threading.LazyThreadSafetyMode.PublicationOnly);

        [Obsolete("Use GetDefaultSerializerSettings() instead")]
        public static JsonSerializerSettings DefaultSerializerSettings => settings.Value;

        public static JsonSerializerSettings GetDefaultSerializerSettings()
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            settings.Converters.Add(new MemberInfoConverter());
            return settings;
        }
    }
}