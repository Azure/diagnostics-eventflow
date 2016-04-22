using AirTrafficControl.Web.WebSrv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.TrafficSimulator
{
    internal interface ITrafficSimulator
    {
        Task ChangeTrafficSimulation(TrafficSimulationModel trafficSimulationSettings);
    }
}
