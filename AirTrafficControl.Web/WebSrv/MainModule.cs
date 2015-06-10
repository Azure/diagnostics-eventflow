using AirTrafficControl.Interfaces;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
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
                return Response.AsJson(airplaneIDs);
            };

            Get["/api/airplanes/{id}", runAsync: true] = async (parameters, cancellationToken) =>
            {
                AtcController atc = new AtcController();
                AirplaneActorState airplaneState = await atc.GetAirplaneState((string) parameters.id);
                return Response.AsJson<AirplaneActorState>(airplaneState);
            };

            Post["/api/flights", runAsync: true] = async (parameters, cancellationToken) =>
            {
                var requestModel = this.Bind<FlightPlanRequestModel>();
                var atc = new AtcController();
                await atc.StartNewFlight(requestModel.airplaneID, requestModel.departurePoint, requestModel.destination);
                return HttpStatusCode.Created;
                // If the flight was addressable individually, we would return something like this:
                // return new Response(){StatusCode = HttpStatusCode.Created}.WithHeader("Location", "new flight URL");
            };
        }
    }
}
