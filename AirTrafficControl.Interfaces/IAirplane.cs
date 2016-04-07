using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Interfaces
{
    public interface IAirplane: IActor // , ITimeAwareObject
    {
        Task ReceiveInstructionAsync(AtcInstruction instruction);
        Task<AirplaneActorState> GetStateAsync();
        Task StartFlightAsync(FlightPlan flightPlan);

        // Should not be required, but there is a bug in Actor FX that prevents us from using ITimeAwareObject in this interface definition
        Task TimePassedAsync(int currentTime);
    }
}
