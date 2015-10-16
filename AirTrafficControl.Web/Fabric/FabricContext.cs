using System;
using System.Fabric;

namespace AirTrafficControl.Web.Fabric
{
    internal class FabricContext<T> where T : ServiceInitializationParameters
    {
        public FabricContext(T parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            this.InitializationParameters = parameters;
        }

        public T InitializationParameters { get; private set; }
    }
}
