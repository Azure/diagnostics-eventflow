using Microsoft;
using Nancy.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Validation;

namespace AirTrafficControl.Web.Fabric
{
    internal class OwinStartup : IOwinAppBuilder
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            Requires.NotNull(appBuilder, "appBuilder");

            System.Net.ServicePointManager.DefaultConnectionLimit = 256;

            // Since AirTrafficControl.Web service is configured as a single-instance service, there is nothing extra
            // we have to do to make the exposed SignalR endpoint work in Fabric environment.
            // If it were a multi-instance service, we would have to use/develop a SignalR backplane linking all the instances
            // of the service, so that all clients (websites displaying airplane movement) would receive the same set of messages.
            //
            // Ideally, that would be a custom, Fabric-based backplane. Custom backplanes are possible with SignalR, 
            // but the documentation is scarce.
            appBuilder.MapSignalR();

            appBuilder.UseNancy();

            // THIS IS HOW ASP.NET WEB API CAN BE BOOTSTRAPPED FOR AN OWIN-BASED APPLICATION 
            //
            //HttpConfiguration config = new HttpConfiguration();

            //FormatterConfig.ConfigureFormatters(config.Formatters);
            //RouteConfig.RegisterRoutes(config.Routes);

            //appBuilder.UseWebApi(config);
        }
    }
}
