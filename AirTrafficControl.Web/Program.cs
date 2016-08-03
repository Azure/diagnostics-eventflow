using AirTrafficControl.Web.Fabric;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using Microsoft.Extensions.Diagnostics.Fabric;

namespace AirTrafficControl.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (var diagnosticsPipeline = EventSourceToAppInsightsPipelineFactory.CreatePipeline("DiagnosticsPipeline-FrontendService"))
                {
                    // This is the name of the ServiceType that is registered with FabricRuntime. 
                    // This name must match the name defined in the ServiceManifest. If you change
                    // this name, please change the name of the ServiceType in the ServiceManifest.
                    ServiceRuntime.RegisterServiceAsync("AirTrafficControlWebType", ctx => new AirTrafficControlWeb(ctx)).Wait();

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(AirTrafficControlWeb).Name);

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        
    }
}
