// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using Validation;

namespace Microsoft.Extensions.Diagnostics.Metadata
{
    public class EventMetadata
    {
        public string MetadataKind { get; private set; }
        public string IncludeCondition { get; set; }
        
        public IDictionary<string, string> Properties { get; private set; }

        public EventMetadata(string metadataKind)
        {
            Requires.NotNullOrWhiteSpace(metadataKind, nameof(metadataKind));
            this.MetadataKind = metadataKind;
            this.Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryApplying(EventData e)
        {
            // TODO: evaluate IncludeContition and determine if the metadata applies
            // If yes, call e.SetMetadata(this) and return true
            // If not, return false
            // CONSIDER: allow empty condition as means of saying "always apply"
            return false;
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

            if (MetadataKind != other.MetadataKind || IncludeCondition != other.IncludeCondition)
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
            return this.MetadataKind.GetHashCode() ^ this.IncludeCondition.GetHashCode();
        }
    }
}
