using AirTrafficControl.Interfaces;
using AirTrafficControl.Web.Fabric;
using Nancy;
using Nancy.ModelBinding;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirTrafficControl.Web.WebSrv
{
    public class MainModule: NancyModule
    {
        private StatelessServiceInitializationParameters serviceInitializationParameters;

        private const string GetAirplanesOperation = "GetAirplanes";
        private const string GetAirplaneStateOperation = "GetAirplaneState";
        private const string GetAirportsOperation = "GetAirports";
        private const string GetFlightsOperation = "GetFlights";
        private const string StartNewFlightOperation = "StartNewFlight";

        public MainModule()
        {
            try
            {
                this.serviceInitializationParameters = FabricContext<StatelessServiceInitializationParameters>.Current.InitializationParameters;

                Get["/"] = parameters =>
                {
                    return View["atcmain.html", new MainPageModel(Request)];
                };

                Get["/api/airplanes", runAsync: true] = async (parameters, cancellationToken) =>
                 {
                     ServiceEventSource.Current.RestApiOperationStart(this.serviceInitializationParameters, GetAirplanesOperation);
                     var atc = new AtcController();
                     var airplaneStates = await atc.GetFlyingAirplaneStates().ConfigureAwait(false);
                     ServiceEventSource.Current.RestApiOperationStop(GetAirplanesOperation);
                     return Response.AsJson<IEnumerable<AirplaneStateModel>>(airplaneStates).WithHeaders(PublicShortLived());
                 };

                Get["/api/airplanes/{id}", runAsync: true] = async (parameters, cancellationToken) =>
                {
                    ServiceEventSource.Current.RestApiOperationStart(this.serviceInitializationParameters, GetAirplaneStateOperation);
                    AtcController atc = new AtcController();
                    AirplaneActorState airplaneState = await atc.GetAirplaneState((string)parameters.id).ConfigureAwait(false);
                    ServiceEventSource.Current.RestApiOperationStop(GetAirplaneStateOperation);
                    return Response.AsJson<AirplaneActorState>(airplaneState).WithHeaders(PublicShortLived());
                };

                Get["/api/airports", runAsync: true] = async (parameters, cancellationToken) =>
                {
                    ServiceEventSource.Current.RestApiOperationStart(this.serviceInitializationParameters, GetAirportsOperation);
                    var atc = new AtcController();
                    var airports = await atc.GetAirports().ConfigureAwait(false);
                    ServiceEventSource.Current.RestApiOperationStop(GetAirportsOperation);
                    return Response.AsJson<IEnumerable<Airport>>(airports);
                };

                Post["/api/flights", runAsync: true] = async (parameters, cancellationToken) =>
                {
                    ServiceEventSource.Current.RestApiOperationStart(this.serviceInitializationParameters, StartNewFlightOperation);
                    var requestModel = this.Bind<FlightPlanRequestModel>();
                    var atc = new AtcController();
                    await atc.StartNewFlight(requestModel.AirplaneID, requestModel.DepartureAirport.Name, requestModel.DestinationAirport.Name).ConfigureAwait(false);
                    ServiceEventSource.Current.RestApiOperationStop(StartNewFlightOperation);
                    return HttpStatusCode.Created;
                // If the flight was addressable individually, we would return something like this:
                // return new Response(){StatusCode = HttpStatusCode.Created}.WithHeader("Location", "new flight URL");
                };
            }
            catch(Exception e)
            {
                ServiceEventSource.Current.RestApiInitializationError(e.ToString());
                throw;
            }
        }

        private object[] PublicShortLived()
        {
            return new[] { new { Header = "Cache-Control", Value = "public,max-age=1"} };
        }
    }
}
