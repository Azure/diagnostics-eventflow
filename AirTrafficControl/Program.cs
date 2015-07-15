using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Actors;

using AirTrafficControl.SharedLib;

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
                    ElasticSearchListener listener = new ElasticSearchListener(new Uri("http://kzelk03.cloudapp.net/es", UriKind.Absolute), "kzadora", "w13XP287!", "atc");
                    
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
