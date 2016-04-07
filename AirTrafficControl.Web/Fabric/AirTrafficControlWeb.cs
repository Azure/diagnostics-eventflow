using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System.Fabric;
using System.Collections.Generic;
using System;

namespace AirTrafficControl.Web.Fabric
{
    public class AirTrafficControlWeb : StatelessService
    {
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[] { new ServiceInstanceListener(CreateCommunicationListener) };
        }

        private ICommunicationListener CreateCommunicationListener(StatelessServiceContext ctx)
        {
            var fabricContext = new FabricContext<StatelessServiceContext>(ctx);
            FabricContext<StatelessServiceContext>.Current = fabricContext;

            var listener = new OwinCommunicationListener(new OwinStartup(), ctx);

            ServiceEventSource.Current.ServiceMessage(this, "Communication listener created");
            return listener;
        }
    }
}
