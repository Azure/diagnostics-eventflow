// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public enum DataRetrievalStatus
    {
        Success = 0,
        MetadataPropertyMissing = 1,
        InvalidMetadataValue = 2,
        DataMissingOrInvalid = 3,
        InvalidMetadataType = 4
    }

    public class DataRetrievalResult
    {
        public static readonly DataRetrievalResult Success = new DataRetrievalResult() { Status = DataRetrievalStatus.Success };

        public DataRetrievalStatus Status { get; private set; }
        public string Message { get; private set; }

        // Prevent public construction
        private DataRetrievalResult() { }

        public static DataRetrievalResult MissingMetadataProperty(string missingPropertyName)
        {
            Requires.NotNullOrWhiteSpace(missingPropertyName, nameof(missingPropertyName));

            return new DataRetrievalResult()
            {
                Status = DataRetrievalStatus.MetadataPropertyMissing,
                Message = $"Expected property '{missingPropertyName}' is missing from event metadata"
            };
        }

        public static DataRetrievalResult InvalidMetadataPropertyValue(string invalidPropertyName, string invalidPropertyValue)
        {
            Requires.NotNullOrWhiteSpace(invalidPropertyName, nameof(invalidPropertyName));

            return new DataRetrievalResult()
            {
                Status = DataRetrievalStatus.InvalidMetadataValue,
                Message = $"The value of metadata property '{invalidPropertyName}' is '{invalidPropertyValue}', which is not valid"
            };
        }

        public static DataRetrievalResult DataMissingOrInvalid(string propertyName)
        {
            Requires.NotNullOrWhiteSpace(propertyName, nameof(propertyName));

            return new DataRetrievalResult()
            {
                Status = DataRetrievalStatus.DataMissingOrInvalid,
                Message = $"The expected event property '{propertyName}' is either missing from event data, or its value is invalid"
            };
        }

        public static DataRetrievalResult InvalidMetadataType(string actualMetadataType, string expectedMetadataType)
        {
            Requires.NotNullOrWhiteSpace(expectedMetadataType, nameof(expectedMetadataType));

            return new DataRetrievalResult()
            {
                Status = DataRetrievalStatus.InvalidMetadataType,
                Message = $"Was expecting metadata of type '{expectedMetadataType}' but the actual metadata type is '{actualMetadataType}'"
            };
        }

        public override bool Equals(object obj)
        {
            DataRetrievalResult other = obj as DataRetrievalResult;
            if (other == null)
            {
                return false;
            }

            return this.Status == other.Status && string.Equals(this.Message, other.Message, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return this.Message != null ? this.Message.GetHashCode() ^ (int)this.Status : (int)this.Status;
        }
    }
}
