
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="../Scripts/Typings/signalr/signalr.d.ts" />
/// <reference path="../Scripts/Typings/angular-signalr-hub/angular-signalr-hub.d.ts" />
/// <reference path="AirplaneState.ts" />

module AirTrafficControl {
    class NewFlightData {
        public AirplaneID: string;
        public DepartureAirport: Airport;
        public DestinationAirport: Airport;
    }

    export interface IMainControllerScope extends ng.IScope {
        AirplaneStates: AirplaneState[];
        Airports: Airport[];
        NewFlightData: NewFlightData;
        Map: Microsoft.Maps.Map;

        OnNewFlight: () => void;
    }

    export class MainController {
        private updateInterval: ng.IPromise<any>;
        private atcHub: ngSignalr.Hub;

        constructor(private $scope: IMainControllerScope, private $interval: ng.IIntervalService, private $http: ng.IHttpService, private Hub: ngSignalr.HubFactory) {
            $scope.AirplaneStates = [];

            // Temporarily disable polling while experimenting with SignalR
            // this.updateInterval = $interval(() => this.onUpdateAirplaneStates(), 2000);

            $scope.$on('$destroy',() => this.onScopeDestroy());

            $scope.Airports = [];
            this.$http.get("/api/airports").then((response: ng.IHttpPromiseCallbackArg<Airport[]>) => {
                this.$scope.Airports = response.data;
            });

            $scope.NewFlightData = new NewFlightData();

            $scope.OnNewFlight = () => this.onNewFlight();

            var atcHubOptions: ngSignalr.HubOptions = {
                listeners: {
                    'flightStatusUpdate': (newAirplaneStates: AirplaneState[]) => {
                        this.$scope.AirplaneStates = newAirplaneStates;
                        this.$scope.$apply();
                    }
                }
            };
            this.atcHub = new Hub('atc', atcHubOptions);
        }

        private onUpdateAirplaneStates() {
            this.$http.get("/api/airplanes").then((response: ng.IHttpPromiseCallbackArg<AirplaneState[]>) => {
                this.$scope.AirplaneStates = response.data;
            });
            
            // TODO: some error indication if the data is stale and cannot be refreshed
        }

        private onNewFlight() {
            // TODO: validate form parameters before poking the server
            this.$http.post("/api/flights", this.$scope.NewFlightData);
            // TODO: some error indication if the new flight was not created successfully
        }

        private onScopeDestroy() {
            this.$interval.cancel(this.updateInterval);

            if (this.$scope.Map) {
                this.$scope.Map.dispose();
            }
        }
    }
}