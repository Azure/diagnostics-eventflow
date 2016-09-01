// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public class EventMetadata
    {
        public string MetadataType { get; private set; }
        
        public IDictionary<string, string> Properties { get; private set; }

        public EventMetadata(string metadataKind)
        {
            Requires.NotNullOrWhiteSpace(metadataKind, nameof(metadataKind));
            this.MetadataType = metadataKind;
            this.Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // Rather than throwing KeyNotFoundException we simply return null if the property does not exist.
        public string this[string propertyName]
        {
            get
            {
                string value;
                this.Properties.TryGetValue(propertyName, out value);
                return value;
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as EventMetadata;
            if (obj == null)
            {
                return false;
            }

            if (MetadataType != other.MetadataType)
            {
                return false;
            }

            var sortedKeys = this.Properties.Keys.OrderBy(k => k, StringComparer.Ordinal);
            var otherSortedKeys = other.Properties.Keys.OrderBy(k => k, StringComparer.Ordinal);
            return sortedKeys.SequenceEqual(otherSortedKeys)
                && sortedKeys.Select(k => this.Properties[k]).SequenceEqual(otherSortedKeys.Select(k2 => other.Properties[k2]));
        }

        public override int GetHashCode()
        {
            return this.MetadataType.GetHashCode();
        }
    }
}
