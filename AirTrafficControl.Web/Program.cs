using AirTrafficControl.Web.Fabric;
using Microsoft.Diagnostics.EventListeners;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Fabric;
using System.Threading;

using FabricEventListeners = Microsoft.Diagnostics.EventListeners.Fabric;

namespace AirTrafficControl.Web
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

                // This is the name of the ServiceType that is registered with FabricRuntime. 
                // This name must match the name defined in the ServiceManifest. If you change
                // this name, please change the name of the ServiceType in the ServiceManifest.
                ServiceRuntime.RegisterServiceAsync("AirTrafficControlWebType", ctx => new AirTrafficControlWeb(ctx)).Wait();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(AirTrafficControlWeb).Name);

                Thread.Sleep(Timeout.Infinite);

                GC.KeepAlive(listener);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        
    }
}
