
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

    class TrafficSimulationData {
        public SimulatedTrafficCount: number;
    }

    export interface IMainControllerScope extends ng.IScope {
        AirplaneStates: AirplaneState[];
        Airports: Airport[];
        NewFlightData: NewFlightData;
        TrafficSimulationData: TrafficSimulationData;
        Map: Microsoft.Maps.Map;
        AnimationProgress: number; // A number from 0 to 1 that expresses the desired position of an airplane along its track during the current animation period

        OnNewFlight: () => void;
        UpdateSimulation: () => void;
    }

    export class MainController {
        // Number of animation periods per airplane status update period
        private static AirplaneAnimationPeriods: number = 10;

        private updateInterval: ng.IPromise<any>;
        private atcHub: ngSignalr.Hub;
        

        constructor(private $scope: IMainControllerScope, private $interval: ng.IIntervalService, private $http: ng.IHttpService, private Hub: ngSignalr.HubFactory) {
            $scope.AirplaneStates = [];

            $scope.$on('$destroy',() => this.onScopeDestroy());

            $scope.Airports = [];
            this.$http.get("/api/airports").then((response: ng.IHttpPromiseCallbackArg<Airport[]>) => {
                this.$scope.Airports = response.data;
            });

            $scope.NewFlightData = new NewFlightData();
            $scope.TrafficSimulationData = new TrafficSimulationData();

            $scope.OnNewFlight = () => this.onNewFlight();
            $scope.UpdateSimulation = () => this.updateSimulation();

            var atcHubOptions: ngSignalr.HubOptions = {
                listeners: {
                    'flightStatusUpdate': (flightStatus: FlightStatusModel) => this.onFlightStatusUpdate(flightStatus)
                }
            };
            this.atcHub = new Hub('atc', atcHubOptions);
        }

        private onNewFlight() {
            // TODO: validate form parameters before poking the server
            this.$http.post("/api/flights", this.$scope.NewFlightData);
            // TODO: some error indication if the new flight was not created successfully
        }

        private updateSimulation() {
            this.$http.post("/api/simulation/traffic", this.$scope.TrafficSimulationData);
        }

        private onAnimationProgress() {
            if (this.$scope.AnimationProgress < 0.99) {
                this.$scope.AnimationProgress += 1.0 / MainController.AirplaneAnimationPeriods;
            }
        }

        private onFlightStatusUpdate(flightStatus: FlightStatusModel) {
            this.$interval.cancel(this.updateInterval);
            this.$scope.AnimationProgress = 0.0;
            var animationDelay = flightStatus.EstimatedNextStatusUpdateDelayMsec / MainController.AirplaneAnimationPeriods;
            this.updateInterval = this.$interval(() => this.onAnimationProgress(), animationDelay);

            this.$scope.AirplaneStates = flightStatus.AirplaneStates;
            this.$scope.$apply();
        }

        private onScopeDestroy() {
            this.$interval.cancel(this.updateInterval);

            if (this.$scope.Map) {
                this.$scope.Map.dispose();
            }
        }
    }
}