using AirTrafficControl.Interfaces;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    [HubName("atc")]
    public class AtcHub: Hub<IAtcHubClient>
    {
        public Task UpdateFlightStatus(IEnumerable<AirplaneStateDto> newAirplaneStates)
        {
            // TODO: look at the context and ensure that only internal server code can execute this method
            return Clients.All.flightStatusUpdate(newAirplaneStates);
        }
    }

    public interface IAtcHubClient
    {
        Task flightStatusUpdate(IEnumerable<AirplaneStateDto> newAirplaneStates);
    }
}
