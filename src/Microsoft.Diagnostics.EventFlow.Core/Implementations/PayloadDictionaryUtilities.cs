// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
using System;
using System.Collections.Generic;
using Validation;

namespace Microsoft.Diagnostics.EventFlow
{
    public static class PayloadDictionaryUtilities
    {
        public static void AddPayloadProperty(IDictionary<string, object> payload, string key, object value, IHealthReporter healthReporter, string context)
        {
            Requires.NotNull(payload, nameof(payload));
            Requires.NotNull(key, nameof(key));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            if (!payload.ContainsKey(key))
            {
                payload.Add(key, value);
                return;
            }

            string newKey;
            int i = 1;
            //update property key till there is no such key in dict
            do
            {
                newKey = key + "_" + i.ToString("d");
                i++;
            }
            while (payload.ContainsKey(newKey));

            payload.Add(newKey, value);
            healthReporter.ReportWarning($"The property with the key '{key}' already exist in the event payload. Value was added under key '{newKey}'", context);
        }
    }
}
