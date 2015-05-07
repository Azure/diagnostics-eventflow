using Microsoft;
using Nancy.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web
{
    internal class OwinStartup : IOwinAppBuilder
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            Requires.NotNull(appBuilder, "appBuilder");

            System.Net.ServicePointManager.DefaultConnectionLimit = 256;

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
