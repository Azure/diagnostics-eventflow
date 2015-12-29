using Microsoft.ServiceFabric.Services;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace AirTrafficControl.TestClient.StatefulServiceClient
{
    public class HttpCommunicationClientFactory : CommunicationClientFactoryBase<HttpCommunicationClient>
    {
        protected override void AbortClient(HttpCommunicationClient client)
        {
            if (client == null)
                return;

            try
            {
                client.CancelPendingRequests();
                client.Dispose();
            }
            catch { }
        }

        protected override Task<HttpCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            Requires.NotNullOrWhiteSpace(endpoint, "endpoint");
            HttpCommunicationClient client = new HttpCommunicationClient();
            string baseServiceAddress = endpoint;
            if (!baseServiceAddress.EndsWith("/", StringComparison.Ordinal))
            {
                baseServiceAddress += "/";
            }
            client.BaseAddress = new Uri(baseServiceAddress, UriKind.Absolute);
            return Task.FromResult(client);
        }

        protected override bool ValidateClient(HttpCommunicationClient clientChannel)
        {
            return true;
        }

        protected override bool ValidateClient(string endpoint, HttpCommunicationClient client)
        {
            return true;
        }
    }
}
