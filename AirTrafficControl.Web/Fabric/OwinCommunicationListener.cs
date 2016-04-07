using System;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace AirTrafficControl.Web.Fabric
{
    internal class OwinCommunicationListener : ICommunicationListener
    {
        private readonly IOwinAppBuilder startup;
        private readonly string appRoot;
        private readonly ServiceContext serviceContext;
        private IDisposable serverHandle;
        private string listeningAddress;

        public OwinCommunicationListener(IOwinAppBuilder startup, ServiceContext serviceContext) : this(null, startup, serviceContext) { }

        public OwinCommunicationListener(string appRoot, IOwinAppBuilder startup, ServiceContext serviceContext)
        {
            this.startup = startup;
            this.appRoot = appRoot;
            this.serviceContext = serviceContext;
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            EndpointResourceDescription serviceEndpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
            int port = serviceEndpoint.Port;
            EndpointProtocol protocol = serviceEndpoint.Protocol;

            this.listeningAddress = String.Format(
                CultureInfo.InvariantCulture,
                "{0}://+:{1}/{2}",
                protocol,
                port,
                String.IsNullOrWhiteSpace(this.appRoot)
                    ? String.Empty
                    : this.appRoot.TrimEnd('/') + '/');

            try
            {
                this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder => this.startup.Configuration(appBuilder));
            }
            catch (TargetInvocationException tie)
            {
                ServiceEventSource.Current.Message("TargetIncationException during open, {0}", tie);
                throw;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message("Exception during open, {0}", e);
                throw;
            }

            string resultAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            ServiceEventSource.Current.Message("Listening on {0}", resultAddress);

            return Task.FromResult(resultAddress);
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
            if (this.serverHandle != null)
            {
                try
                {
                    this.serverHandle.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
        }
    }
}
