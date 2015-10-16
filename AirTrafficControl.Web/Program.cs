using AirTrafficControl.Interfaces;
using AirTrafficControl.Web.Fabric;
using Microsoft.Diagnostics.EventListeners;

using System;
using System.Configuration;
using System.Diagnostics;
using System.Fabric;
using System.Threading;

namespace AirTrafficControl.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (FabricRuntime fabricRuntime = FabricRuntime.Create())
                {
                    BufferingEventListener listener = null;

                    if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["EsUserName"]))
                    {
                        listener = new ElasticSearchListener(
                            (new FabricDiagnosticChannelContext()).ToString(),
                            new Uri(ConfigurationManager.AppSettings["EsUrl"], UriKind.Absolute),
                            ConfigurationManager.AppSettings["EsUserName"],
                            ConfigurationManager.AppSettings["EsUserPassword"],
                            "atc");
                    }

                    // This is the name of the ServiceType that is registered with FabricRuntime. 
                    // This name must match the name defined in the ServiceManifest. If you change
                    // this name, please change the name of the ServiceType in the ServiceManifest.
                    fabricRuntime.RegisterServiceType("AirTrafficControlWebType", typeof(AirTrafficControlWeb));

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(AirTrafficControlWeb).Name);

                    Thread.Sleep(Timeout.Infinite);

                    GC.KeepAlive(listener);
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e);
                throw;
            }
        }

        
    }
}
