// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Xunit;
using Microsoft.Diagnostics.EventFlow.Metadata;
using System;

namespace Microsoft.Diagnostics.EventFlow.Core.Tests

{
    public class MetadataTests
    {
        private static readonly int DoublePrecisionTolerance = 5; // Five decimal places

        [Fact]
        public void MetricDataReadSuccessfully()
        {
            EventData eventData = new EventData();
            eventData.Payload.Add("metricValue", 17.4);

            EventMetadata metricMetadata = new EventMetadata(MetricData.MetricMetadataKind);
            metricMetadata.Properties.Add(MetricData.MetricNameMoniker, "SomeMetric");
            metricMetadata.Properties.Add(MetricData.MetricValueMoniker, "33.5");

            // Fixed-value metric
            var result = MetricData.TryGetData(eventData, metricMetadata, out MetricData md);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal(33.5, md.Value, DoublePrecisionTolerance);
            Assert.Equal("SomeMetric", md.MetricName);

            // Value read from event properties
            metricMetadata.Properties.Remove(MetricData.MetricValueMoniker);
            metricMetadata.Properties.Add(MetricData.MetricValuePropertyMoniker, "metricValue");
            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal(17.4, md.Value, DoublePrecisionTolerance);

            // Able to convert event property value to a double as needed
            eventData.Payload["metricValue"] = "3.14";
            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal(3.14, md.Value, DoublePrecisionTolerance);

            //metric name value property            
            eventData.Payload.Add("metricName", "customMetricName");
            metricMetadata.Properties.Remove(MetricData.MetricNameMoniker);
            metricMetadata.Properties.Add(MetricData.MetricNamePropertyMoniker, "metricName");

            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal(3.14, md.Value, DoublePrecisionTolerance);
            Assert.Equal("customMetricName", md.MetricName);
        }

        [Fact]
        public void MetricDataExpectedReadFailures()
        {
            EventData eventData = new EventData();

            // Invalid metadata type
            EventMetadata metricMetadata = new EventMetadata("someOtherType");

            var result = MetricData.TryGetData(eventData, metricMetadata, out MetricData md);
            Assert.Equal(DataRetrievalStatus.InvalidMetadataType, result.Status);

            // No metricName or metricNameProperty on the metadata
            metricMetadata = new EventMetadata(MetricData.MetricMetadataKind);
            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.MetadataPropertyMissing, result.Status);
            Assert.Contains("Expected property 'metricName'", result.Message);

            // metricNameProperty points to a property that does not exist
            metricMetadata.Properties.Add(MetricData.MetricNamePropertyMoniker, "customMetricName");
            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);

            // No metricValue or metricValueProperty on the metadata
            metricMetadata.Properties.Remove(MetricData.MetricNamePropertyMoniker);
            metricMetadata.Properties.Add(MetricData.MetricNameMoniker, "SomeMetric");
            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.MetadataPropertyMissing, result.Status);
            Assert.Contains("Expected property 'metricValue'", result.Message);

            // metricValue cannot be parsed
            metricMetadata.Properties.Add("metricValue", "not_a_number");
            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.InvalidMetadataValue, result.Status);

            // metricValueProperty points to a property that does not exist
            metricMetadata.Properties.Remove("metricValue");
            metricMetadata.Properties.Add("metricValueProperty", "value");
            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);

            // metricValueProperty points to a property that does not containa a value that can be parsed as double
            eventData.Payload.Add("value", "not-a-number");
            result = MetricData.TryGetData(eventData, metricMetadata, out md);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);
        }

        [Fact]
        public void RequestDataReadSuccessfully()
        {
            EventData eventData = new EventData();
            eventData.Payload.Add("requestName", "GetCountOfStuff");
            eventData.Payload.Add("isSuccess", true);
            eventData.Payload.Add("responseCode", "200 OK");

            EventMetadata requestMetadata = new EventMetadata("request");
            requestMetadata.Properties.Add("requestNameProperty", "requestName");
            requestMetadata.Properties.Add("isSuccessProperty", "isSuccess");
            requestMetadata.Properties.Add("responseCodeProperty", "responseCode");
            requestMetadata.Properties.Add("durationProperty", "duration");

            // Duration is milliseconds, seconds, minutes, hours, timespan
            eventData.Payload.Add("duration", 34.7);
            requestMetadata.Properties.Add("durationUnit", "milliseconds");
            var result = RequestData.TryGetData(eventData, requestMetadata, out RequestData rd);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal("GetCountOfStuff", rd.RequestName);
            Assert.True(rd.IsSuccess);
            Assert.Equal("200 OK", rd.ResponseCode);

            // Note the TimeSpan.FromMilliseconds will round to the nearest millisecond on everything older than .NET Core 3.0
#if NETCOREAPP3_1 || NET6_0 || NET8_0
            Assert.Equal(34.7, rd.Duration.Value.TotalMilliseconds, DoublePrecisionTolerance);
#else
            Assert.Equal(35, rd.Duration.Value.TotalMilliseconds, DoublePrecisionTolerance);
#endif
            requestMetadata.Properties["durationUnit"] = "seconds";
            result = RequestData.TryGetData(eventData, requestMetadata, out rd);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal(34.7, rd.Duration.Value.TotalSeconds, DoublePrecisionTolerance);

            requestMetadata.Properties["durationUnit"] = "minutes";
            result = RequestData.TryGetData(eventData, requestMetadata, out rd);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal(34.7, rd.Duration.Value.TotalMinutes, DoublePrecisionTolerance);

            requestMetadata.Properties["durationUnit"] = "hours";
            result = RequestData.TryGetData(eventData, requestMetadata, out rd);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal(34.7, rd.Duration.Value.TotalHours, DoublePrecisionTolerance);

            requestMetadata.Properties["durationUnit"] = "TimeSpan";
            eventData.Payload["duration"] = TimeSpan.FromMilliseconds(124);
            result = RequestData.TryGetData(eventData, requestMetadata, out rd);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal(124, rd.Duration.Value.TotalMilliseconds, DoublePrecisionTolerance);

            // Duration is not specified--default to milliseconds
            requestMetadata.Properties.Remove("durationUnit");
            eventData.Payload["duration"] = 65.7;
            result = RequestData.TryGetData(eventData, requestMetadata, out rd);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
#if NETCOREAPP3_1 || NET6_0 || NET8_0
            Assert.Equal(65.7, rd.Duration.Value.TotalMilliseconds, DoublePrecisionTolerance);
#else
            Assert.Equal(66, rd.Duration.Value.TotalMilliseconds, DoublePrecisionTolerance);
#endif
        }

        [Fact]
        public void RequestDataExpectedReadFailure()
        {
            // Invalid metadata type
            EventMetadata requestMetadata = new EventMetadata("someOtherType");
            EventData eventData = new EventData();

            var result = RequestData.TryGetData(eventData, requestMetadata, out RequestData md);
            Assert.Equal(DataRetrievalStatus.InvalidMetadataType, result.Status);

            requestMetadata = new EventMetadata("request");
            requestMetadata.Properties.Add("requestNameProperty", "requestName");
            requestMetadata.Properties.Add("isSuccessProperty", "isSuccess");
            requestMetadata.Properties.Add("responseCodeProperty", "responseCode");
            requestMetadata.Properties.Add("durationProperty", "duration");

            // isSuccessProperty points to non-existing property
            result = RequestData.TryGetData(eventData, requestMetadata, out md);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);
            Assert.Contains("The expected event property 'isSuccess'", result.Message);

            eventData.Payload.Add("isSuccess", false);

            // durationProperty points to non-existing property
            result = RequestData.TryGetData(eventData, requestMetadata, out md);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);
            Assert.Contains("The expected event property 'duration'", result.Message);

            // duration is not a number
            eventData.Payload.Add("duration", "not-a-number");
            result = RequestData.TryGetData(eventData, requestMetadata, out md);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);
            Assert.Contains("The expected event property 'duration'", result.Message);

            eventData.Payload["duration"] = 15.7;

            // responseCodeProperty points to non-existing property
            result = RequestData.TryGetData(eventData, requestMetadata, out md);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);
            Assert.Contains("The expected event property 'responseCode'", result.Message);

            eventData.Payload.Add("responseCode", "401 Not Found");

            // Just make sure that after addressing all the issues you can read all the data successfully
            result = RequestData.TryGetData(eventData, requestMetadata, out md);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
        }

        [Fact]
        public void DependencyDataReadSuccessfully()
        {
            // The handling of isSuccess, duration and response code is tested fairly thoroughly by Request tests,
            // so here wie will just focus on Dependency-specific properties: target and dependency type.
            EventData eventData = new EventData();
            eventData.Payload.Add("isSuccess", true);
            eventData.Payload.Add("responseCode", "200 OK");
            eventData.Payload.Add("duration", 212);
            eventData.Payload.Add("targetServiceUrl", "http://customerdata");

            EventMetadata dependencyMetadata = new EventMetadata("dependency");
            dependencyMetadata.Properties.Add("isSuccessProperty", "isSuccess");
            dependencyMetadata.Properties.Add("responseCodeProperty", "responseCode");
            dependencyMetadata.Properties.Add("durationProperty", "duration");
            dependencyMetadata.Properties.Add("targetProperty", "targetServiceUrl");
            dependencyMetadata.Properties.Add("dependencyType", "CustomerDataService");

            var result = DependencyData.TryGetData(eventData, dependencyMetadata, out DependencyData dd);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.True(dd.IsSuccess);
            Assert.Equal("200 OK", dd.ResponseCode);
            Assert.Equal(212, dd.Duration.Value.TotalMilliseconds, DoublePrecisionTolerance);
            Assert.Equal("http://customerdata", dd.Target);
            Assert.Equal("CustomerDataService", dd.DependencyType);
        }

        [Fact]
        public void DependencyDataExpectedReadFailure()
        {
            // The handling of isSuccess, duration and response code is tested fairly thoroughly by Request tests,
            // so here wie will just focus on Dependency-specific properties: target and dependency type.

            // Target property points to non-existent property
            EventData eventData = new EventData();

            EventMetadata dependencyMetadata = new EventMetadata("dependency");
            dependencyMetadata.Properties.Add("targetProperty", "targetServiceUrl");

            var result = DependencyData.TryGetData(eventData, dependencyMetadata, out DependencyData dd);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);
            Assert.Contains("The expected event property 'targetServiceUrl'", result.Message);

            // Just make sure that after addressing all the issues you can read all the data successfully
            eventData.Payload.Add("targetServiceUrl", "http://customerdata");
            result = DependencyData.TryGetData(eventData, dependencyMetadata, out dd);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
        }

        [Fact]
        public void ExceptionDataReadSuccessfully()
        {
            EventData eventData = new EventData();

            try
            {
                throw new Exception("Oops!");
            }
            catch (Exception e)
            {
                eventData.Payload.Add("exception", e);
            }

            EventMetadata exceptionMetadata = new EventMetadata("exception");
            exceptionMetadata.Properties.Add("exceptionProperty", "exception");

            var result = ExceptionData.TryGetData(eventData, exceptionMetadata, out ExceptionData exceptionData);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
            Assert.Equal("Oops!", exceptionData.Exception.Message);
        }

        [Fact]
        public void ExceptionDataExpectedReadFailure()
        {
            // exceptionProperty points to non-existent property
            EventData eventData = new EventData();

            EventMetadata exceptionMetadata = new EventMetadata("exception");
            exceptionMetadata.Properties.Add("exceptionProperty", "exception");

            var result = ExceptionData.TryGetData(eventData, exceptionMetadata, out ExceptionData exceptionData);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);
            Assert.Contains("The expected event property 'exception'", result.Message);

            // exceptionProperty does not contain and Exception object
            eventData.Payload.Add("exception", Guid.NewGuid());
            result = ExceptionData.TryGetData(eventData, exceptionMetadata, out exceptionData);
            Assert.Equal(DataRetrievalStatus.DataMissingOrInvalid, result.Status);
            Assert.Contains("The expected event property 'exception'", result.Message);

            // Just make sure that after addressing all the issues you can read all the data successfully
            eventData.Payload["exception"] = new Exception();
            result = ExceptionData.TryGetData(eventData, exceptionMetadata, out exceptionData);
            Assert.Equal(DataRetrievalStatus.Success, result.Status);
        }
    }
}
