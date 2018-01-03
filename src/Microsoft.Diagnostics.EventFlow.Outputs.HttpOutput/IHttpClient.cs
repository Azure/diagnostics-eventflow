// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Implementation
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content);
        HttpRequestHeaders DefaultRequestHeaders { get; }
    }
}
