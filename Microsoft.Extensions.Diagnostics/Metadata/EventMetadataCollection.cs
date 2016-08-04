// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public class EventMetadataCollection<TMetadata> where TMetadata: EventMetadata
    {
        private IDictionary source;

        public EventMetadataCollection(IDictionary source)
        {
            Requires.NotNull(source, nameof(source));
            this.source = source;
        }

        public TMetadata GetMetadata(string providerName, string eventName)
        {
            if (string.IsNullOrEmpty(providerName) || string.IsNullOrEmpty(eventName))
            {
                return default(TMetadata);
            }

            string key = EventMetadata.GetCollectionKey(providerName, eventName);
            return (TMetadata) this.source[key];
        }
    }
}
