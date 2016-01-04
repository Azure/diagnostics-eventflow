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
    }

    public interface IAtcHubClient
    {
        Task flightStatusUpdate(IEnumerable<AirplaneStateDto> newAirplaneStates);
    }
}
