using AirTrafficControl.Interfaces;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.Configuration;
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
                return View["atcmain.html", new MainPageModel(Request)];
            };

            Get["/api/airplanes", runAsync:true] = async(parameters, cancellationToken) =>
            {
                var atc = new AtcController();
                var airplaneStates = await atc.GetFlyingAirplaneStates();
                return Response.AsJson<IEnumerable<AirplaneStateModel>>(airplaneStates);
            };

            Get["/api/airplanes/{id}", runAsync: true] = async (parameters, cancellationToken) =>
            {
                AtcController atc = new AtcController();
                AirplaneActorState airplaneState = await atc.GetAirplaneState((string) parameters.id);
                return Response.AsJson<AirplaneActorState>(airplaneState);
            };

            Get["/api/airports", runAsync: true] = async (parameters, cancellationToken) =>
            {
                var atc = new AtcController();
                var airports = await atc.GetAirports();
                return Response.AsJson<IEnumerable<Airport>>(airports);
            };

            Post["/api/flights", runAsync: true] = async (parameters, cancellationToken) =>
            {
                var requestModel = this.Bind<FlightPlanRequestModel>();
                var atc = new AtcController();
                await atc.StartNewFlight(requestModel.AirplaneID, requestModel.DepartureAirport.Name, requestModel.DestinationAirport.Name);
                return HttpStatusCode.Created;
                // If the flight was addressable individually, we would return something like this:
                // return new Response(){StatusCode = HttpStatusCode.Created}.WithHeader("Location", "new flight URL");
            };
        }
    }
}
