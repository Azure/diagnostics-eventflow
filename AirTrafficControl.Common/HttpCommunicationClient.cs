using System;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System.Net.Http;

namespace AirTrafficControl.Common
{
    public class HttpCommunicationClient : ICommunicationClient
    {
        public HttpCommunicationClient(HttpClient client, string address)
        {
            this.HttpClient = client;
            this.Url = new Uri(address);
        }

        public HttpClient HttpClient { get; }

        public Uri Url { get; }

        ResolvedServiceEndpoint ICommunicationClient.Endpoint { get; set; }

        string ICommunicationClient.ListenerName { get; set; }

        ResolvedServicePartition ICommunicationClient.ResolvedServicePartition { get; set; }
    }
}
