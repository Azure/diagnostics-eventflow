// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using Validation;

namespace Microsoft.Diagnostics.EventFlow.Metadata
{
    public class DynatraceEventData
    {
        public static readonly string MetadataKind = "dynatrace-event";

        public string TagMatchEntityType { get; private set; }
        public string TagMatchContext { get; private set; }
        public string TagMatchKey { get; private set; }
        public string TagMatchValue { get; private set; }
        
        public bool HasTargetEntity
        {
            get
            {
                return (!String.IsNullOrEmpty(TagMatchEntityType) && !String.IsNullOrEmpty(TagMatchKey));
            }
        }

        public string Source { get; private set; }
        public string EventType { get; private set; }
        public string AnnotationType { get; private set; }
        public string AnnotationDescription { get; private set; }
        public string Description { get; private set; }
        public string DeploymentName { get; private set; }
        public string DeploymentVersion { get; private set; }
        public string Configuration { get; private set; }

        // Ensure that DynatraceEventData can only be created using TryGetData() method
        private DynatraceEventData() { }

        public static DataRetrievalResult TryGetData(
            EventData eventData,
            EventMetadata eventMetadata,
            out DynatraceEventData meta)
        {
            Requires.NotNull(eventData, nameof(eventData));
            Requires.NotNull(eventMetadata, nameof(eventMetadata));

            meta = null;

            if (!MetadataKind.Equals(eventMetadata.MetadataType, System.StringComparison.OrdinalIgnoreCase))
            {
                return DataRetrievalResult.InvalidMetadataType(eventMetadata.MetadataType, MetadataKind);
            }

            meta = new DynatraceEventData();
            meta.Source = eventMetadata["source"];
            meta.EventType = eventMetadata["eventType"];
            meta.AnnotationType = eventMetadata["annotationType"];
            meta.AnnotationDescription = eventMetadata["annotationDescription"];
            meta.Description = eventMetadata["description"];
            meta.DeploymentName = eventMetadata["deploymentName"];
            meta.DeploymentVersion = eventMetadata["deploymentVersion"];
            meta.Configuration = eventMetadata["configuration"];

            meta.TagMatchEntityType = eventMetadata["tagMatchEntityType"];
            meta.TagMatchContext = eventMetadata["tagMatchContext"];
            meta.TagMatchKey = eventMetadata["tagMatchKey"];
            meta.TagMatchValue = eventMetadata["tagMatchValue"];

            string val = "";
            string property = eventMetadata["annotationTypeProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.AnnotationType = val;
            }
            property = eventMetadata["annotationDescriptionProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.AnnotationDescription = val;
            }
            property = eventMetadata["descriptionProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.Description = val;
            }

            property = eventMetadata["deploymentNameProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.DeploymentName = val;
            }
            property = eventMetadata["deploymentVersionProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.DeploymentVersion = val;
            }
            property = eventMetadata["configuratonProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.DeploymentVersion = val;
            }

            property = eventMetadata["tagMatchEntityTypeProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.TagMatchEntityType = val;
            }

            property = eventMetadata["tagMatchContextProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.TagMatchContext = val;
            }
            property = eventMetadata["tagMatchKeyProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.TagMatchKey = val;
            }
            property = eventMetadata["tagMatchValueProperty"];
            if (!string.IsNullOrEmpty(property))
            {
                if (eventData.GetValueFromPayload<string>(property, (v) => val = v))
                    meta.TagMatchValue = val;
            }
            return DataRetrievalResult.Success;
        }        
    }
}
