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
        public AirportModel DepartureAirport { get; set; }
        public AirportModel DestinationAirport { get; set; }
    }

    internal class AirportModel
    {
        public string Name { get; set; }
    }
}
