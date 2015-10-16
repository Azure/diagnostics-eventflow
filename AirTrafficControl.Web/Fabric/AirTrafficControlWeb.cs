using Microsoft.ServiceFabric.Services;
using System.Fabric;

namespace AirTrafficControl.Web.Fabric
{
    public class AirTrafficControlWeb : StatelessService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            var listener = new OwinCommunicationListener(new OwinStartup());
            listener.ServiceInitialized += OnServiceInitialized;
            return listener;
        }

        private void OnServiceInitialized(object sender, ServiceInitializedEventArgs e)
        {
            StatelessServiceInitializationParameters statelessInitParams = (StatelessServiceInitializationParameters)e.InitializationParameters;
            var fabricContext = new FabricContext<StatelessServiceInitializationParameters>(statelessInitParams);
            FabricContext<StatelessServiceInitializationParameters>.Current = fabricContext;
        }
    }
}
