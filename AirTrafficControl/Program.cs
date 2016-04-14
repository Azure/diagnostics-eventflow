
using Microsoft.Diagnostics.EventListeners;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;
using FabricEventListeners = Microsoft.Diagnostics.EventListeners.Fabric;

namespace AirTrafficControl
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                const string ElasticSearchEventListenerId = "ElasticSearchEventListener";
                FabricEventListeners.FabricConfigurationProvider configProvider = new FabricEventListeners.FabricConfigurationProvider(ElasticSearchEventListenerId);
                ElasticSearchListener listener = null;
                if (configProvider.HasConfiguration)
                {
                    listener = new ElasticSearchListener(configProvider, new FabricEventListeners.FabricHealthReporter(ElasticSearchEventListenerId));
                }

                Task.WhenAll(                    
                    ActorRuntime.RegisterActorAsync<Airplane>()
                ).Wait();

                Thread.Sleep(Timeout.Infinite);

                GC.KeepAlive(listener);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
