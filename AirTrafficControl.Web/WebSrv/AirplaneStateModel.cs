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

            this.iD = id;
            this.stateDescription = stateDescription;
        }

        public string iD { get; private set; }
        public string stateDescription { get; private set; }
    }
}
