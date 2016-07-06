// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventListeners
{
    public class HttpExponentialRetryMessageHandler: DelegatingHandler
    {
        private Polly.Retry.RetryPolicy<HttpResponseMessage> retryPolicy;
        private Polly.CircuitBreaker.CircuitBreakerPolicy<HttpResponseMessage> breakerPolicy;

        public HttpExponentialRetryMessageHandler(): base(new HttpClientHandler())
        {
            this.retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(responseMessage => ((int)responseMessage.StatusCode) >= 500)
                .WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)});

            this.breakerPolicy = Policy
                .HandleResult<HttpResponseMessage>(responseMessage => ((int)responseMessage.StatusCode) >= 500)
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromMinutes(2));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.breakerPolicy.ExecuteAsync(() => this.retryPolicy.ExecuteAsync(() => base.SendAsync(request, cancellationToken)));
        }
    }
}
