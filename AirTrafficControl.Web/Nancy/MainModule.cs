using AirTrafficControl.Interfaces;
using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.Nancy
{
    public class MainModule: NancyModule
    {
        public MainModule()
        {
            Get["/"] = parameters =>
            {
                return View["atcmain.html", new BasicPageModel(Request)];
            };

            Get["/api/airplanes", runAsync:true] = async(parameters, cancellationToken) =>
            {
                var atc = new AtcController();
                var airplaneIDs = await atc.GetFlyingAirplaneIDs();
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                return JsonConvert.SerializeObject(airplaneIDs);
            };

            Post["/api/newFlight", runAsync: true] = async (parameters, cancellationToken) =>
            {
                var requestModel = this.BindAndValidate<FlightPlanRequestModel>();
                var atc = new AtcController();
                await atc.StartNewFlight(requestModel.airplaneID, requestModel.departurePoint, requestModel.destination);
                return HttpStatusCode.Created;
            };
        }
    }
}
