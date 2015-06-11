using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    internal class FlightPlanRequestModel
    {
        public string AirplaneID { get; set; }
        public string DeparturePoint { get; set; }
        public string Destination { get; set; }
    }
}
