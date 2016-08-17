// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Validation;

#if NET45
using System.Collections.Specialized;
#endif

namespace Microsoft.Extensions.Diagnostics
{
    public class EventData
    {
#if NET45
        private HybridDictionary metadata;
#elif NETSTANDARD1_6
        private IDictionary<string,object> metadata;
#endif

        public DateTimeOffset Timestamp { get; set; }

        public string ProviderName { get; set; }

        public int EventId { get; set; }

        public string Message { get; set; }

        public string Level { get; set; }

        public string Keywords { get; set; }

        public string EventName { get; set; }

        public string ActivityID { get; set; }

        public IDictionary<string, object> Payload { get; set; }

        public object GetMetadata(string metadataKind)
        {
            if (this.metadata == null)
            {
                return null;
            }

            return this.metadata[metadataKind];
        }

        public void SetMetadata(string metadataKind, object value)
        {
            Requires.NotNull(metadataKind, nameof(metadataKind));
            if (this.metadata == null)
            {
#if NET45
                this.metadata = new HybridDictionary();
#elif NETSTANDARD1_6
                this.metadata = new Dictionary<string, object>();
#endif
            }

            // CONSIDER: we'll probably need to be able to attach more than one piece of metadata of the same kind to an event
            // For now just one will suffice.
            this.metadata[metadataKind] = value;
        }

        public object GetPropertyValue(string propertyName)
        {
            throw new NotImplementedException("Just make the parser work for now. This method needs to be implemented to make evaluators work.");
        }
    }
}