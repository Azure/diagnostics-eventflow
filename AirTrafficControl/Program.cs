
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Fabric;

namespace AirTrafficControl
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (var diagnosticsPipeline = EventSourceToAppInsightsPipelineFactory.CreatePipeline("DiagnosticsPipeline-AirplaneService"))
                {
                    Task.WhenAll(
                        ActorRuntime.RegisterActorAsync<Airplane>()
                    ).Wait();

                    Thread.Sleep(Timeout.Infinite);
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
