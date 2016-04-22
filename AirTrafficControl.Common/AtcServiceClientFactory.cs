using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using System;

namespace AirTrafficControl.Common
{
    public static class AtcServiceClientFactory
    {
        private static readonly Uri AtcServiceUri = new Uri("fabric:/AirTrafficControlApplication/Atc");
        public static ServicePartitionClient<WcfCommunicationClient<IAirTrafficControl>> CreateClient()
        {
            var wcfClientFactory = new WcfCommunicationClientFactory<IAirTrafficControl>(
                       clientBinding: WcfUtility.CreateTcpClientBinding(),
                       servicePartitionResolver: ServicePartitionResolver.GetDefault());

            var newAtcClient = new ServicePartitionClient<WcfCommunicationClient<IAirTrafficControl>>(
                wcfClientFactory,
                AtcServiceUri,
                ServicePartitionKey.Singleton,
                TargetReplicaSelector.Default,
                WellKnownIdentifiers.AtcServiceListenerName);
            return newAtcClient;
        }
    }
}
