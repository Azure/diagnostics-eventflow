using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.Fabric
{
    internal class ServiceInitializedEventArgs: EventArgs
    {
        public ServiceInitializationParameters InitializationParameters { get; set; }
    }

    internal interface INotifyServiceInitialized
    {
        event EventHandler<ServiceInitializedEventArgs> ServiceInitialized;
    }
}
