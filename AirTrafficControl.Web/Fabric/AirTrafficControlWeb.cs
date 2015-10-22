using Microsoft.ServiceFabric.Services;
using System.Fabric;

namespace AirTrafficControl.Web.Fabric
{
    public class AirTrafficControlWeb : StatelessService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            var fabricContext = new FabricContext<StatelessServiceInitializationParameters>(this.ServiceInitializationParameters);
            FabricContext<StatelessServiceInitializationParameters>.Current = fabricContext;

            ServiceEventSource.Current.ServiceExecutionStarted(this.ServiceInitializationParameters);

            var listener = new OwinCommunicationListener(new OwinStartup());            
            return listener;
        }
    }
}
