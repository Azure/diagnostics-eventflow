using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    internal class Location
    {
        public double Lattitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
    }
}
