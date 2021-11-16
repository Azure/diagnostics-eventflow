// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class ElasticSearchProxyConfiguration
    {
        public string Uri { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }

        internal ElasticSearchProxyConfiguration DeepClone()
        {
            var other = new ElasticSearchProxyConfiguration();
            other.Uri = this.Uri;
            other.UserName = this.UserName;
            other.UserPassword = this.UserPassword;
            return other;
        }
    }
}
