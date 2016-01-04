using AirTrafficControl.Interfaces;
using AirTrafficControl.Web.Fabric;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace AirTrafficControl.Web.WebSrv
{
    public class MainModule: NancyModule
    {
        private delegate Task<dynamic> AsyncNancyRequestHandler(dynamic parameters, CancellationToken cancellationToken);

        private StatelessServiceInitializationParameters serviceInitializationParameters;

        private const string NotifyFlightStatusUpdate = "FlightStatusUpdate";

        public MainModule()
        {
            try
            {
                this.serviceInitializationParameters = FabricContext<StatelessServiceInitializationParameters>.Current.InitializationParameters;

                Get["/"] = parameters =>
                {
                    return View["atcmain.html", new MainPageModel(Request)];
                };

                Get["/api/airplanes", runAsync: true] = (p, ct) => PerformRestOperation("GetAirplanes", p, ct, (AsyncNancyRequestHandler) (
                    async (parameters, cancellationToken) =>
                    {
                        var atc = new AtcController();
                        var airplaneStates = await atc.GetFlyingAirplaneStates().ConfigureAwait(false);
                        return Response.AsJson<IEnumerable<AirplaneStateDto>>(airplaneStates).WithHeaders(PublicShortLived());
                    }));
                    

                Get["/api/airplanes/{id}", runAsync: true] = (p, ct) => PerformRestOperation("GetAirplaneState", p, ct, (AsyncNancyRequestHandler) (
                    async (parameters, cancellationToken) =>
                    {
                        AtcController atc = new AtcController();
                        AirplaneActorState airplaneState = await atc.GetAirplaneState((string)parameters.id).ConfigureAwait(false);
                        return Response.AsJson<AirplaneActorState>(airplaneState).WithHeaders(PublicShortLived());
                    }));

                Get["/api/airports", runAsync: true] = (p, ct) => PerformRestOperation("GetAirports", p, ct, (AsyncNancyRequestHandler) (
                    async (parameters, cancellationToken) =>
                    {
                        var atc = new AtcController();
                        var airports = await atc.GetAirports().ConfigureAwait(false);
                        return Response.AsJson<IEnumerable<Airport>>(airports);
                    }));

                Post["/api/flights", runAsync: true] = (p, ct) => PerformRestOperation("StartNewFlight", p, ct, (AsyncNancyRequestHandler) (
                    async (parameters, cancellationToken) =>
                    {
                        var requestModel = this.Bind<FlightPlanRequestModel>();
                        var atc = new AtcController();
                        await atc.StartNewFlight(requestModel.AirplaneID, requestModel.DepartureAirport.Name, requestModel.DestinationAirport.Name).ConfigureAwait(false);
                        return HttpStatusCode.Created;
                        // If the flight was addressable individually, we would return something like this:
                        // return new Response(){StatusCode = HttpStatusCode.Created}.WithHeader("Location", "new flight URL");
                    }));

                Post["/api/notify/flight-status", runAsync: true] = (p, ct) => PerformRestOperation("FlightStatusUpdate", p, ct, (AsyncNancyRequestHandler) (
                    async (parameters, cancellationToken) => 
                    {
    #if DEBUG
                        string requestBody = (new StreamReader(this.Request.Body)).ReadToEnd();
    #endif
                        var newAirplaneStates = this.Bind<IEnumerable<AirplaneStateDto>>();
                        var atc = new AtcController();
                        await atc.PerformFlightStatusUpdate(newAirplaneStates).ConfigureAwait(false);
                        return HttpStatusCode.NoContent; // Success, just nothing to report back.
                    }));
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

        private async Task<dynamic> PerformRestOperation(string operationName, dynamic parameters, CancellationToken cancellationToken, AsyncNancyRequestHandler inner)
        {
            Requires.NotNullOrWhiteSpace(operationName, nameof(operationName));
            Assumes.NotNull(this.serviceInitializationParameters);

            ServiceEventSource.Current.RestApiOperationStart(this.serviceInitializationParameters, operationName);
            try
            {
                var retval = await inner(parameters, cancellationToken).ConfigureAwait(false);
                ServiceEventSource.Current.RestApiOperationStop(operationName);
                return retval;
            }
            catch(Exception e)
            {
                ServiceEventSource.Current.RestApiOperationError(e.ToString(), operationName);
                throw;
            }
        }
    }
}
