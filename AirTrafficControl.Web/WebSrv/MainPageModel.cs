using Nancy;
using System.Configuration;

namespace AirTrafficControl.Web.WebSrv
{
    internal class MainPageModel: BasicPageModel
    {
        public MainPageModel(Request request): base(request)
        { }

        public string BingMapsKey
        {
            get { return ConfigurationManager.AppSettings["BingMapsKey"]; }
        }
    }
}
