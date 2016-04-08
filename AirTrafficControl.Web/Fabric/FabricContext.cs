using System;
using System.Fabric;

namespace AirTrafficControl.Web.Fabric
{
    internal class FabricContext<T> where T : ServiceContext
    {
        public static FabricContext<T> Current { get; set; }

        public FabricContext(T serviceContext)
        {
            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            this.ServiceContext = serviceContext;
        }

        public T ServiceContext { get; private set; }        
    }
}
