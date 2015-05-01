using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirTrafficControl.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;

namespace AirTrafficControl
{
    public class AirTrafficControl : Actor<AirTrafficControlState>, IAirTrafficControl
    {
        public override Task OnActivateAsync()
        {
            return Task.FromResult(true);
        }        
    }
}
