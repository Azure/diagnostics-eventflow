using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.EventFlow.Outputs.Splunk
{
    /// <summary>
    /// Implements a message handler that employs exponential backoff retry policy for HTTP requests
    /// </summary>
    internal class HttpExponentialRetryMessageHandler : DelegatingHandler
    {
        private const int MaxRetries = 3;
        private readonly TimeSpan[] AttemptDelays = new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) };        

        public HttpExponentialRetryMessageHandler() : base(new HttpClientHandler())
        {            
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {            
            int attempt = 0;

            while (true)
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (attempt == MaxRetries || (int)response.StatusCode < 500)
                {
                    return response;
                }
                else
                {
                    await Task.Delay(AttemptDelays[attempt++], cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}