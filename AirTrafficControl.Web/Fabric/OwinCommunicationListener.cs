using Microsoft;
using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services;
using Nancy.TinyIoc;
using System;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.Fabric
{
    internal class OwinCommunicationListener : ICommunicationListener
    {
        private IDisposable serverHandle;

        private IOwinAppBuilder startup;
        private string publishAddress;
        private string listeningAddress;
        private string appRoot;

        public OwinCommunicationListener(IOwinAppBuilder startup)
            : this(null, startup)
        {
        }

        public OwinCommunicationListener(string appRoot, IOwinAppBuilder startup)
        {
            Requires.NotNull(startup, "startup");
            this.startup = startup;
            this.appRoot = appRoot;
        }

        public void Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            EndpointResourceDescription serviceEndpoint = serviceInitializationParameters.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
            int port = serviceEndpoint.Port;

            if (serviceInitializationParameters is StatefulServiceInitializationParameters)
            {
                StatefulServiceInitializationParameters statefulInitParams = (StatefulServiceInitializationParameters)serviceInitializationParameters;

                this.listeningAddress = String.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}/{2}/{3}",
                    port,
                    statefulInitParams.PartitionId,
                    statefulInitParams.ReplicaId,
                    Guid.NewGuid());
            }
            else if (serviceInitializationParameters is StatelessServiceInitializationParameters)
            {
                this.listeningAddress = String.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    String.IsNullOrWhiteSpace(this.appRoot)
                        ? String.Empty
                        : this.appRoot.TrimEnd('/') + '/');
            }
            else
            {
                throw new InvalidOperationException();
            }

            this.publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder => this.startup.Configuration(appBuilder));

                return Task.FromResult(this.publishAddress);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(ex);

                this.StopWebServer();

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            this.StopWebServer();
        }

        private void StopWebServer()
        {
            var tempHandle = this.serverHandle;
            this.serverHandle = null;

            if (tempHandle != null)
            {
                tempHandle.Dispose();
            }
        }
    }
}
