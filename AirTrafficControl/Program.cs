
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
                Task.WhenAll(                    
                    ActorRuntime.RegisterActorAsync<Airplane>()
                ).Wait();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
