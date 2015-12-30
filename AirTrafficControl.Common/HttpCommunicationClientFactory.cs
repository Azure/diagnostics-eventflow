using System;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Client;

namespace AirTrafficControl.Common
{
    public class HttpCommunicationClientFactory : CommunicationClientFactoryBase<HttpCommunicationClient>
    {
        private static TimeSpan MaxRetryBackoffIntervalOnNonTransientErrors = TimeSpan.FromSeconds(3);

        public HttpCommunicationClientFactory(ServicePartitionResolver resolver, TimeSpan operationTimeout, TimeSpan readWriteTimeout)
            : base(resolver, null, null)
        {
            this.OperationTimeout = operationTimeout;
            this.ReadWriteTimeout = readWriteTimeout;
        }

        /// <summary>
        /// Represents the value for operation timeout. Passed to clients.
        /// </summary>
        public TimeSpan OperationTimeout { get; set; }

        /// <summary>
        /// Represents the value for the timeout used to read/write from a stream. Passed to clients.
        /// </summary>
        public TimeSpan ReadWriteTimeout { get; set; }

        protected override void AbortClient(HttpCommunicationClient client)
        {
            // Http communication doesn't maintain a communication channel, so nothing to abort.
        }

        protected override bool ValidateClient(string endpoint, HttpCommunicationClient client)
        {
            if (client.BaseAddress.ToString() == endpoint)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override bool ValidateClient(HttpCommunicationClient clientChannel)
        {
            // Http communication doesn't maintain a communication channel, so nothing to validate.
            return true;
        }

        protected override Task<HttpCommunicationClient> CreateClientAsync(
            string endpoint,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(endpoint) || !endpoint.StartsWith("http"))
            {
                throw new InvalidOperationException("The endpoint address is not valid. Please resolve again.");
            }

            if (!endpoint.EndsWith("/"))
            {
                endpoint = endpoint + "/";
            }

            // Create a communication client. This doesn't establish a session with the server.
            return Task.FromResult(new HttpCommunicationClient(new Uri(endpoint), this.OperationTimeout, this.ReadWriteTimeout));
        }

        protected override bool OnHandleException(Exception e, out ExceptionHandlingResult result)
        {
            if (e is TimeoutException)
            {
                return this.CreateExceptionHandlingResult(false, out result);
            }
            else if (e is ProtocolViolationException)
            {
                return this.CreateExceptionHandlingResult(false, out result);
            }
            else if (e is WebException)
            {
                WebException we = e as WebException;
                HttpWebResponse errorResponse = we.Response as HttpWebResponse;

                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        // This could either mean we requested an endpoint that does not exist in the service API (a user error)
                        // or the address that was resolved by fabric client is stale (transient runtime error) in which we should re-resolve.
                        return this.CreateExceptionHandlingResult(false, out result);
                    }

                    if (errorResponse.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        // The address is correct, but the server processing failed.
                        // This could be due to conflicts when writing the word to the dictionary.
                        // Retry the operation without re-resolving the address.
                        return this.CreateExceptionHandlingResult(true, out result);
                    }
                }

                if (we.Status == WebExceptionStatus.Timeout ||
                    we.Status == WebExceptionStatus.RequestCanceled ||
                    we.Status == WebExceptionStatus.ConnectionClosed ||
                    we.Status == WebExceptionStatus.ConnectFailure)
                {
                    return this.CreateExceptionHandlingResult(false, out result);
                }
            }

            return base.OnHandleException(e, out result);
        }

        private bool CreateExceptionHandlingResult(bool isTransient, out ExceptionHandlingResult result)
        {
            result = new ExceptionHandlingRetryResult()
            {
                IsTransient = isTransient,
                RetryDelay = TimeSpan.FromMilliseconds(MaxRetryBackoffIntervalOnNonTransientErrors.TotalMilliseconds),
            };

            return true;
        }
    }

}
