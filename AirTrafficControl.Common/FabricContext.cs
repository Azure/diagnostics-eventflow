using System;
using System.Fabric;
using Validation;

namespace AirTrafficControl.Common
{
    public class FabricContext<TContext, TService> 
        where TContext : ServiceContext 
        where TService : class
    {
        public static FabricContext<TContext, TService> Current { get; set; }

        public FabricContext(TContext serviceContext, TService serviceInstance)
        {
            Requires.NotNull(serviceContext, nameof(serviceContext));
            Requires.NotNull(serviceInstance, nameof(serviceInstance));

            this.ServiceContext = serviceContext;
            this.ServiceInstance = serviceInstance;
        }

        public TContext ServiceContext { get; private set; }
        public TService ServiceInstance { get; private set; }
    }
}
