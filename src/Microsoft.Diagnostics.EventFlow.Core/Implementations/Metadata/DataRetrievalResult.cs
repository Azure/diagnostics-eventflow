// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public enum DataRetrievalStatus
    {
        Success = 0,
        MetadataPropertyMissing = 1,
        InvalidMetadataValue = 2,
        DataMissingOrInvalid = 3
    }

    public class DataRetrievalResult
    {
        private static readonly DataRetrievalResult SuccessResult = new DataRetrievalResult() { Status = DataRetrievalStatus.Success };

        public DataRetrievalStatus Status { get; private set; }
        public string Message { get; private set; }

        // Prevent public construction
        private DataRetrievalResult() { }

        public static DataRetrievalResult Success()
        {
            return SuccessResult;
        }

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
    }
}
