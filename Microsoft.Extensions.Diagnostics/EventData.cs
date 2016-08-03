// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Validation;

namespace Microsoft.Extensions.Diagnostics
{
    public class EventData
    {
        private HybridDictionary metadata;

        public DateTimeOffset Timestamp { get; set; }

        public string ProviderName { get; set; }

        public int EventId { get; set; }

        public string Message { get; set; }

        public string Level { get; set; }

        public string Keywords { get; set; }

        public string EventName { get; set; }

        public string ActivityID { get; set; }

        public IDictionary<string, object> Payload { get; set; }

        public object GetMetadata(Type metadataType)
        {
            if (this.metadata == null)
            {
                return null;
            }

            return this.metadata[metadataType];
        }

        public void SetMetadata(Type metadataType, object value)
        {
            Requires.NotNull(metadataType, nameof(metadataType));
            if (this.metadata == null)
            {
                this.metadata = new HybridDictionary();
            }

            this.metadata[metadataType] = value;
        }
    }
}