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
                OmsEventListener omsListener = null;
                ICompositeConfigurationProvider configProvider = new FabricEventListeners.FabricJsonFileConfigurationProvider("Diagnostics.json");
                if (configProvider.HasConfiguration)
                {
                    omsListener = new OmsEventListener(configProvider, new FabricEventListeners.FabricHealthReporter("OmsEventListener"));
                }

                // This is the name of the ServiceType that is registered with FabricRuntime. 
                // This name must match the name defined in the ServiceManifest. If you change
                // this name, please change the name of the ServiceType in the ServiceManifest.
                ServiceRuntime.RegisterServiceAsync("AirTrafficControlWebType", ctx => new AirTrafficControlWeb(ctx)).Wait();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(AirTrafficControlWeb).Name);

                Thread.Sleep(Timeout.Infinite);

                GC.KeepAlive(omsListener);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        
    }
}
