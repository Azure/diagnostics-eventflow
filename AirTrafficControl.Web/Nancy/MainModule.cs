using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.Nancy
{
    public class MainModule: NancyModule
    {
        public MainModule()
        {
            Get["/"] = parameters =>
            {
                return View["atcmain.html", new BasicPageModel(Request)];
            };
        }
    }
}
