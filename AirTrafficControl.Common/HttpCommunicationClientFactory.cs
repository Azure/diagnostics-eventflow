using System;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Client;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace AirTrafficControl.Common
{
    public class HttpCommunicationClientFactory : CommunicationClientFactoryBase<HttpCommunicationClient>
    {
        private HttpClient httpClient = new HttpClient();

        public HttpCommunicationClientFactory(IServicePartitionResolver resolver = null, IEnumerable<IExceptionHandler> exceptionHandlers = null)
            : base(resolver, CreateExceptionHandlers(exceptionHandlers))
        {
        }

        protected override void AbortClient(HttpCommunicationClient client)
        {
            // client with persistent connections should be abort their connections here.
            // HTTP clients don't hold persistent connections, so no action is taken.
        }

        protected override Task<HttpCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            // clients that maintain persistent connections to a service should 
            // create that connection here.
            // an HTTP client doesn't maintain a persistent connection.
            return Task.FromResult(new HttpCommunicationClient(this.httpClient, endpoint));
        }

        protected override bool ValidateClient(HttpCommunicationClient client)
        {
            // client with persistent connections should be validated here.
            // HTTP clients don't hold persistent connections, so no validation needs to be done.
            return true;
        }

        protected override bool ValidateClient(string endpoint, HttpCommunicationClient client)
        {
            // client with persistent connections should be validated here.
            // HTTP clients don't hold persistent connections, so no validation needs to be done.
            return true;
        }

        private static IEnumerable<IExceptionHandler> CreateExceptionHandlers(IEnumerable<IExceptionHandler> additionalHandlers)
        {
            return new[] { new HttpExceptionHandler() }.Union(additionalHandlers ?? Enumerable.Empty<IExceptionHandler>());
        }
    }

}
