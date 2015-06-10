using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    internal class FlightPlanRequestModel
    {
        public string airplaneID { get; set; }
        public string departurePoint { get; set; }
        public string destination { get; set; }
    }
}
