using AirTrafficControl.Interfaces;

using Microsoft.Diagnostics.EventListeners;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Configuration;
using System.Fabric;
using System.Threading;
using FabricEventListeners = Microsoft.Diagnostics.EventListeners.Fabric;

namespace AirTrafficControl
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (FabricRuntime fabricRuntime = FabricRuntime.Create())
                {
                    const string ElasticSearchEventListenerId = "ElasticSearchEventListener";
                    FabricEventListeners.FabricConfigurationProvider configProvider = new FabricEventListeners.FabricConfigurationProvider(ElasticSearchEventListenerId);
                    ElasticSearchListener listener = null;
                    if (configProvider.HasConfiguration)
                    {
                        listener = new ElasticSearchListener(configProvider, new FabricEventListeners.FabricHealthReporter(ElasticSearchEventListenerId));
                    }

                    fabricRuntime.RegisterActor<AirTrafficControl>();
                    fabricRuntime.RegisterActor<Airplane>();

                    Thread.Sleep(Timeout.Infinite);

                    GC.KeepAlive(listener);
                }
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
