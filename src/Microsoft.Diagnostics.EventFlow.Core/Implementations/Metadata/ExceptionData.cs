// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public class ExceptionData
    {
        public static readonly string ExceptionMetadataKind = "exception";
        public static readonly string ExceptionPropertyMoniker = "exceptionProperty";

        public Exception Exception { get; private set; }

        private ExceptionData(Exception e)
        {
            Debug.Assert(e != null);
            this.Exception = e;
        }

        public static DataRetrievalResult TryGetData(
            EventData eventData,
            EventMetadata exceptionMetadata,
            out ExceptionData exceptionData)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNull(exceptionMetadata, nameof(exceptionMetadata));
            exceptionData = null;

            if (!ExceptionMetadataKind.Equals(exceptionMetadata.MetadataType, System.StringComparison.OrdinalIgnoreCase))
            {
                return DataRetrievalResult.InvalidMetadataType(exceptionMetadata.MetadataType, ExceptionMetadataKind);
            }

            DataRetrievalResult result = exceptionMetadata.GetEventPropertyValue(eventData, ExceptionPropertyMoniker, out Exception e);
            if (result.Status == DataRetrievalStatus.Success)
            {
                exceptionData = new ExceptionData(e);
            }
            return result;
        }
    }
}
