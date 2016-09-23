// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    // !!ACTION!!
    // If you make any changes here, please update the README.md file to reflect the new configuration
    public class ElasticSearchOutputConfiguration: ItemConfiguration, IDeepCloneable<ElasticSearchOutputConfiguration>
    {
        public static readonly string DefaultEventDocumentTypeName = "event";

        public string IndexNamePrefix { get; set; }
        public string ServiceUri { get; set; }
        public string BasicAuthenticationUserName { get; set; }
        public string BasicAuthenticationUserPassword { get; set; }
        public string EventDocumentTypeName { get; set; }

        public ElasticSearchOutputConfiguration()
        {
            EventDocumentTypeName = DefaultEventDocumentTypeName;
        }

        public ElasticSearchOutputConfiguration DeepClone()
        {
            var other = new ElasticSearchOutputConfiguration()
            {
                IndexNamePrefix = this.IndexNamePrefix,
                ServiceUri = this.ServiceUri,
                BasicAuthenticationUserName = this.BasicAuthenticationUserName,
                BasicAuthenticationUserPassword = this.BasicAuthenticationUserPassword,
                EventDocumentTypeName = this.EventDocumentTypeName
            };

            return other;
        }
    }
}
