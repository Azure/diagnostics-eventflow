// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public class RequestData: NetworkCallData
    {
        public static readonly string RequestMetadataKind = "request";
        public static readonly string RequestNamePropertyMoniker = "requestNameProperty";

        public string RequestName { get; private set; }

        // Ensure that RequestData can only be created using TryGetRequestData() method
        private RequestData() { }

        public static DataRetrievalResult TryGetData(
            EventData eventData,
            EventMetadata requestMetadata,
            out RequestData request)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNull(requestMetadata, nameof(requestMetadata));
            request = null;

            if (!RequestMetadataKind.Equals(requestMetadata.MetadataType, System.StringComparison.OrdinalIgnoreCase))
            {
                return DataRetrievalResult.InvalidMetadataType(requestMetadata.MetadataType, RequestMetadataKind);
            }

            // Inability to retrieve request name is not a terminating error--ignore the return value from GetEventPropertyValue here.
            requestMetadata.GetEventPropertyValue(eventData, RequestNamePropertyMoniker, out string requestName);

            DataRetrievalResult result = GetSuccessValue(eventData, requestMetadata, out bool? success);
            if (result.Status != DataRetrievalStatus.Success)
            {
                return result;
            }

            result = GetDurationValue(eventData, requestMetadata, out TimeSpan? duration);
            if (result.Status != DataRetrievalStatus.Success)
            {
                return result;
            }

            result = GetResponseCodeValue(eventData, requestMetadata, out string responseCode);
            if (result.Status != DataRetrievalStatus.Success)
            {
                return result;
            }

            request = new RequestData();
            request.RequestName = requestName;
            request.IsSuccess = success;
            request.Duration = duration;
            request.ResponseCode = responseCode;

            return DataRetrievalResult.Success;
        }        
    }
}
