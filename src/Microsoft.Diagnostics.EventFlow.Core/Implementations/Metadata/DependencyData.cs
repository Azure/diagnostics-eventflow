// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public class DependencyData: NetworkCallData
    {
        public static readonly string DependencyMetadataKind = "dependency";        
        public static readonly string TargetPropertyMoniker = "targetProperty";
        public static readonly string DependecyTypeMoniker = "dependencyType";
        
        public string Target { get; private set; }
        public string DependencyType { get; private set; }

        // Ensure that DependencyData can only be created using TryGetDependencyData() method
        private DependencyData() { }

        public static DataRetrievalResult TryGetData(
            EventData eventData,
            EventMetadata dependencyMetadata,
            out DependencyData dependency)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNull(dependencyMetadata, nameof(dependencyMetadata));
            dependency = null;

            if (!DependencyMetadataKind.Equals(dependencyMetadata.MetadataType, System.StringComparison.OrdinalIgnoreCase))
            {
                return DataRetrievalResult.InvalidMetadataType(dependencyMetadata.MetadataType, DependencyMetadataKind);
            }

            DataRetrievalResult result = GetSuccessValue(eventData, dependencyMetadata, out bool? success);
            if (result.Status != DataRetrievalStatus.Success)
            {
                return result;
            }

            result = GetDurationValue(eventData, dependencyMetadata, out TimeSpan? duration);
            if (result.Status != DataRetrievalStatus.Success)
            {
                return result;
            }

            result = GetResponseCodeValue(eventData, dependencyMetadata, out string responseCode);
            if (result.Status != DataRetrievalStatus.Success)
            {
                return result;
            }

            result = dependencyMetadata.GetEventPropertyValue(eventData, TargetPropertyMoniker, out string target);
            if (result.Status != DataRetrievalStatus.Success)
            {
                return result;
            }

            string dependencyType = dependencyMetadata[DependecyTypeMoniker];

            dependency = new DependencyData();
            dependency.IsSuccess = success;
            dependency.Duration = duration;
            dependency.ResponseCode = responseCode;
            dependency.Target = target;
            dependency.DependencyType = dependencyType;

            return DataRetrievalResult.Success;
        }
    }
}
