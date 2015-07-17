using Newtonsoft.Json;
using System.Collections.Generic;
using System.Fabric;

namespace AirTrafficControl.Interfaces
{
    public class FabricDiagnosticChannelContext
    {
        public string NodeName { get; private set; }
        public string ApplicationName { get; private set; }
        public IEnumerable<string> ServiceTypes { get; private set; }

        public FabricDiagnosticChannelContext()
        {
            NodeContext nodeContext = FabricRuntime.GetNodeContext();
            this.NodeName = nodeContext.NodeName;
            
            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            this.ApplicationName = activationContext.ApplicationName;

            var serviceTypes = new List<string>();
            foreach(var serviceDescriptor in activationContext.GetServiceTypes())
            {
                serviceTypes.Add(serviceDescriptor.ServiceTypeName);
            }
            this.ServiceTypes = serviceTypes;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
