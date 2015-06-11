
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="AirplaneState.ts" />

module AirTrafficControl {
    class NewFlightData {
        public AirplaneID: string;
        public DepartureAirport: Airport;
        public DestinationAirport: Airport;
    }

    interface IMainControllerScope extends ng.IScope {
        AirplaneStates: AirplaneState[];
        Airports: Airport[];

        NewFlightData: NewFlightData;
        OnNewFlight: () => void;
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
    }
}