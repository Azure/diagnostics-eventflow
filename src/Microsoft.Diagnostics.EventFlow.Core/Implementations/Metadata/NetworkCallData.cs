// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public abstract class NetworkCallData
    {
        public static readonly string IsSuccessPropertyMoniker = "isSuccessProperty";
        public static readonly string DurationPropertyMoniker = "durationProperty";
        public static readonly string DurationUnitMoniker = "durationUnit";
        public static readonly string ResponseCodePropertyMoniker = "responseCodeProperty";

        public TimeSpan? Duration { get; protected set; }
        public bool? IsSuccess { get; protected set; }
        public string ResponseCode { get; protected set; }

        protected static DataRetrievalResult GetSuccessValue(EventData eventData, EventMetadata networkCallMetadata, out bool? success)
        {
            success = null;

            string isSuccessProperty = networkCallMetadata[IsSuccessPropertyMoniker];
            if (!string.IsNullOrWhiteSpace(isSuccessProperty))
            {
                bool? localSuccessVal = null;
                if (!eventData.GetValueFromPayload<bool>(isSuccessProperty, (v) => localSuccessVal = v))
                {
                    return DataRetrievalResult.DataMissingOrInvalid(isSuccessProperty);
                }
                else
                {
                    success = localSuccessVal;
                }
            }

            return DataRetrievalResult.Success;
        }

        protected static DataRetrievalResult GetDurationValue(EventData eventData, EventMetadata networkCallMetadata, out TimeSpan? duration)
        {
            duration = null;
            string durationProperty = networkCallMetadata[DurationPropertyMoniker];
            if (!string.IsNullOrWhiteSpace(durationProperty))
            {
                DurationUnit durationUnit;
                string durationUnitOverride = networkCallMetadata[DurationUnitMoniker];
                if (string.IsNullOrEmpty(durationUnitOverride) || !Enum.TryParse<DurationUnit>(durationUnitOverride, ignoreCase: true, result: out durationUnit))
                {
                    // By default we assume duration is stored as a double value representing milliseconds
                    durationUnit = DurationUnit.Milliseconds;
                }

                if (durationUnit != DurationUnit.TimeSpan)
                {
                    double tempDuration = 0.0;
                    if (!eventData.GetValueFromPayload<double>(durationProperty, (v) => tempDuration = v))
                    {
                        return DataRetrievalResult.DataMissingOrInvalid(durationProperty);
                    }
                    duration = ToTimeSpan(tempDuration, durationUnit);
                }
                else
                {
                    TimeSpan? localDurationVal = null;
                    if (!eventData.GetValueFromPayload<TimeSpan>(durationProperty, (v) => localDurationVal = v))
                    {
                        return DataRetrievalResult.DataMissingOrInvalid(durationProperty);
                    }
                    else
                    {
                        duration = localDurationVal;
                    }
                }
            }

            return DataRetrievalResult.Success;
        }

        protected static DataRetrievalResult GetResponseCodeValue(EventData eventData, EventMetadata networkCallMetadata, out string responseCode)
        {
            responseCode = null;
            string responseCodeProperty = networkCallMetadata[ResponseCodePropertyMoniker];
            if (!string.IsNullOrWhiteSpace(responseCodeProperty))
            {
                string localResponseCodeVal = null;
                if (!eventData.GetValueFromPayload<string>(responseCodeProperty, (v) => localResponseCodeVal = v))
                {
                    return DataRetrievalResult.DataMissingOrInvalid(responseCodeProperty);
                }
                else
                {
                    responseCode = localResponseCodeVal;
                }
            }

            return DataRetrievalResult.Success;
        }

        private static TimeSpan ToTimeSpan(double value, DurationUnit durationUnit)
        {
            switch (durationUnit)
            {
                case DurationUnit.Milliseconds:
                    return TimeSpan.FromMilliseconds(value);
                case DurationUnit.Seconds:
                    return TimeSpan.FromSeconds(value);
                case DurationUnit.Minutes:
                    return TimeSpan.FromMinutes(value);
                case DurationUnit.Hours:
                    return TimeSpan.FromHours(value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(durationUnit), "Error during request data extraction: unexpected durationUnit value");
            }
        }
    }
}
