using Microsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    internal class AirplaneStateModel
    {
        public AirplaneStateModel(string id, string stateDescription)
        {
            Requires.NotNullOrWhiteSpace(id, "id");

            this.ID = id;
            this.StateDescription = stateDescription;
        }

        public string ID { get; private set; }
        public string StateDescription { get; private set; }
    }
}
