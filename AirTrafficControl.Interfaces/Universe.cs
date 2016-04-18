using Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Validation;

namespace AirTrafficControl.Interfaces
{
    public class Universe
    {
        public static Universe Current = new Universe();

        public ReadOnlyCollection<Airport> Airports { get; private set; }
        public ReadOnlyCollection<Route> Routes { get; private set; }

        public Universe()
        {
            Initialize();
        }

        //public Route GetRouteBetween(Fix from, Fix to)
        //{
        //    Requires.NotNull(from, "from");
        //    Requires.NotNull(to, "to");

        //    Route connectingRoute = Routes.Where(r => r.Fixes.Contains(from) && r.Fixes.Contains(to)).FirstOrDefault();
        //    return connectingRoute;
        //}

        public IEnumerable<Fix> GetAdjacentFixes(Fix reference)
        {
            Requires.NotNull(reference, nameof(reference));

            var containingRoutes = Routes.Where(route => route.Fixes.Contains(reference)).ToList();
            if (containingRoutes.Count == 0)
            {
                throw new ArgumentException("The starting fix does not belong to any route in the universe", nameof(reference));
            }

            var retval = containingRoutes.SelectMany(route => route.GetAdjacentFixes(reference)).Distinct();
            return retval;
        }

        private void Initialize()
        {
            var ksea = new Airport("KSEA", "Seattle-Tacoma International", Direction.West, new Location(47.4505, -122.3074));
            var kgeg = new Airport("KGEG", "Spokane International", Direction.North, new Location(47.6249, -117.5364));
            var kpdx = new Airport("KPDX", "Portland International", Direction.Southeast, new Location(45.5887, -122.5924));
            var kmfr = new Airport("KMFR", "Medford - Rogue Valley International", Direction.Southwest, new Location(42.3742, -122.8735));
            var kboi = new Airport("KBOI", "Boise Ari Terminal", Direction.Northwest, new Location(43.5643, -116.2228));
            var kbzn = new Airport("KBZN", "Bozeman Yellowstone International", Direction.Southwest, new Location(45.7775, -111.1520));


            var eph = new Fix("EPH", "Ephrata VORTAC", new Location(47.3779, -119.4240));
            var mwh = new Fix("MWH", "Moses Lake VOR-DME", new Location(47.2109, -119.3168));
            var ykm = new Fix("YKM", "Yakima VORTAC", new Location(46.5702, -120.4446));
            var malay = new Fix("MALAY", "MALAY intersection", new Location(46.4228, -122.7609));
            var eug = new Fix("EUG", "Eugene VORTAC", new Location(44.1208, -123.2228));
            var lkv = new Fix("LKV", "Lakeview VORTAC", new Location(42.4928, -120.5072));
            var reo = new Fix("REO", "Rome VOR-DME", new Location(42.5905, -117.8682));
            var dnj = new Fix("DNJ", "Donelly VOR-DME", new Location(44.7672, -116.2063));
            var mqg = new Fix("MQG", "Nez Perce VOR-DME", new Location(46.3815, -116.8695));
            var mso = new Fix("MSO", "Missoula VOR-DME", new Location(46.9080, -114.0837));
            var hln = new Fix("HLN", "Helena VORTAC", new Location(46.6083, -111.9535));
            var hia = new Fix("HIA", "Whitehall VOR-DME", new Location(45.8618, -112.1697));


            var v120 = new Route("V120");
            v120.Fixes = new ReadOnlyCollection<Fix>(new Fix[] { ksea, eph, kgeg });

            var v448 = new Route("V448");
            v448.Fixes = new ReadOnlyCollection<Fix>(new Fix[] { kgeg, mwh, ykm, kpdx });

            var v23 = new Route("V23");
            v23.Fixes = new ReadOnlyCollection<Fix>(new Fix[] { ksea, malay, kpdx, eug, kmfr });

            var v122 = new Route("V122");
            v122.Fixes = new ReadOnlyCollection<Fix>(new Fix[] { kmfr, lkv, reo, kboi });

            var v253 = new Route("V253");
            v253.Fixes = new ReadOnlyCollection<Fix>(new Fix[] { kboi, dnj, mqg, kgeg });

            var v2 = new Route("V2");
            v2.Fixes = new ReadOnlyCollection<Fix>(new Fix[] { kgeg, mso, hln, kbzn });

            var v121 = new Route("V121");
            v121.Fixes = new ReadOnlyCollection<Fix>(new Fix[] { dnj, hia, kbzn });

            this.Airports = new ReadOnlyCollection<Airport>(new Airport[] { ksea, kgeg, kpdx, kmfr, kboi, kbzn });
            this.Routes = new ReadOnlyCollection<Route>(new Route[] { v120, v23, v448, v122, v253, v2, v121 });
        }
    }
}
