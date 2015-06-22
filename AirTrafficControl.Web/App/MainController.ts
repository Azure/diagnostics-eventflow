
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
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
        GetBingMapsKey: () => ng.IPromise<string>;
    }

    export class MainController {
        private updateInterval: ng.IPromise<any>;

        constructor(private $scope: IMainControllerScope, private $interval: ng.IIntervalService, private $http: ng.IHttpService) {
            $scope.AirplaneStates = [];

            this.updateInterval = $interval(() => this.onUpdateAirplaneStates(), 2000);

            $scope.$on('$destroy',() => $interval.cancel(this.updateInterval));

            $scope.Airports = [];
            this.$http.get("/api/airports").then((response: ng.IHttpPromiseCallbackArg<Airport[]>) => {
                this.$scope.Airports = response.data;
            });

            $scope.NewFlightData = new NewFlightData();

            $scope.OnNewFlight = () => this.onNewFlight();

            $scope.GetBingMapsKey = () => this.getBingMapsKey();
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

        private getBingMapsKey(): ng.IPromise<string> {
            return this.$http.get("/api/bingmapskey").then((response: ng.IHttpPromiseCallbackArg<string>) => {
                return response.data;
            });
        }
    }
}