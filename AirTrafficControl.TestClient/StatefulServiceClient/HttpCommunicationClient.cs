using Microsoft.ServiceFabric.Services;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.TestClient.StatefulServiceClient
{
    public class HttpCommunicationClient : HttpClient, ICommunicationClient
    {
        public HttpCommunicationClient() : base() { }
        public HttpCommunicationClient(HttpMessageHandler handler) : base(handler) { }
        public HttpCommunicationClient(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler) { }

        public ResolvedServicePartition ResolvedServicePartition { get; set; }
    }
}
