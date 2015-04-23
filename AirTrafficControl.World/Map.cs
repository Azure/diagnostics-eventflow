using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace AirTrafficControl.World
{
    public class Map
    {
        public ReadOnlyCollection<Airport> Airports { get; private set; }
        public ReadOnlyCollection<Route> Routes { get; private set; }

        public RouteFix GetStartingFix(Airport from, Airport destination)
        {
            if (from == null)
            {
                throw new ArgumentNullException("from");
            }

            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            RouteFix connectingRouteFix = from.RouteConnections
                .Where(rc => destination.RouteConnections.Any(destRc => destRc.Route.Name == rc.Route.Name))
                .FirstOrDefault();

            if (connectingRouteFix == null)
            {
                return null;
            }

            return connectingRouteFix;
        }

        public void Initialize()
        {

            var ksea = new Airport("KSEA", "Seattle-Tacoma International");
            var kgeg = new Airport("KGEG", "Spokane International");
            var kpdx = new Airport("KPDX", "Portland International");

            var eph = new Fix("EPH", "Ephrata VORTAC");
            var mwh = new Fix("MWH", "Moses Lake VOR-DME");
            var ykm = new Fix("YKM", "Yakima VORTAC");
            var malay = new Fix("MALAY", "MALAY intersection");

            var v120 = new Route("V120");
            v120.Fixes = new ReadOnlyCollection<Fix>(new Fix[] { ksea, eph, kgeg });



            // V448 KGEG to KPDX via MWH VOR-DME, YKM VORTAC
            // V23 KSEA to KPDX via MALAY intersection
        }
    }
}
