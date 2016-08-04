// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public class EventMetadata
    {
        public string ProviderName { get; set; }
        public string EventName { get; set; }

        public static string GetCollectionKey(string providerName, string eventName)
        {
            return providerName + eventName;
        }
    }
}
