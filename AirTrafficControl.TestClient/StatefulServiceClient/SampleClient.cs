using Microsoft.ServiceFabric.Services;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.TestClient.StatefulServiceClient
{
    class SampleClient
    {
        void DoStuff(string[] args)
        {
            var clientFactory = new HttpCommunicationClientFactory();
            var servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                clientFactory,
                new Uri("fabric:/OsakaStore/Store"),
                0);

            string result = servicePartitionClient.InvokeWithRetry<string>(
                    client =>
                    {
                        return client.GetStringAsync(new Uri("api/events/count", UriKind.Relative)).Result;
                    }
                );

            Console.WriteLine("Result: " + result);
            Console.ReadKey();
        }
    }
}
