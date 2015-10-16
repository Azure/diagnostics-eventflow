using Microsoft.ServiceFabric.Services;
using Nancy.TinyIoc;
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
            if (e.InitializationParameters is StatefulServiceInitializationParameters)
            {
                StatefulServiceInitializationParameters statefulInitParams = (StatefulServiceInitializationParameters)e.InitializationParameters;
                var fabricContext = new FabricContext<StatefulServiceInitializationParameters>(statefulInitParams);
                TinyIoCContainer.Current.Register<FabricContext<StatefulServiceInitializationParameters>>(fabricContext).AsSingleton();
            }
            else if (e.InitializationParameters is StatelessServiceInitializationParameters)
            {
                StatelessServiceInitializationParameters statelessInitParams = (StatelessServiceInitializationParameters)e.InitializationParameters;
                var fabricContext = new FabricContext<StatelessServiceInitializationParameters>(statelessInitParams);
                TinyIoCContainer.Current.Register<FabricContext<StatelessServiceInitializationParameters>>(fabricContext).AsSingleton();
            }
        }
    }
}
