using AirTrafficControl.Interfaces;

using Microsoft.Diagnostics.EventListeners;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Configuration;
using System.Fabric;
using System.Threading;

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

                    fabricRuntime.RegisterActor(typeof(AirTrafficControl));
                    fabricRuntime.RegisterActor(typeof(Airplane));

                    Thread.Sleep(Timeout.Infinite);

                    GC.KeepAlive(listener);
                }
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e);
                throw;
            }
        }
    }
}
