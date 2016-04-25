using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using System.ServiceModel;

namespace AirTrafficControl.Interfaces
{
    [ServiceContract]
    public interface IAirTrafficControl
    {
        [OperationContract]
        Task<IEnumerable<string>> GetFlyingAirplaneIDs();

        [OperationContract]
        Task StartNewFlight(FlightPlan flightPlan);

        [OperationContract]
        Task<long> GetFlyingAirplaneCount();
    }
}
