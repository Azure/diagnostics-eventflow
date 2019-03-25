// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Diagnostics.EventFlow
{
    public class EventFlowJsonUtilities
    {
        private static readonly JsonSerializerSettings s_defaultSerializerSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public static JsonSerializerSettings DefaultSerializerSettings
        {
            get => s_defaultSerializerSettings;
        }
    }
}