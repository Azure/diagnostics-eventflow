using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace AirTrafficControl.Web.WebSrv
{
    [HubName("atc")]
    public class AtcHub: Hub
    {
    }
}
