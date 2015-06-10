
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="AirplaneState.ts" />

module AirTrafficControl {
    interface IMainControllerScope extends ng.IScope {
        AirplaneStates: AirplaneState[];
    }

    export class MainController {
        private updateInterval: ng.IPromise<any>;

        constructor(private $scope: IMainControllerScope, private $interval: ng.IIntervalService, private $http: ng.IHttpService) {
            $scope.AirplaneStates = [];

            this.updateInterval = $interval(() => this.onUpdateAirplaneStates(), 2000);

            $scope.$on('$destroy',() => $interval.cancel(this.updateInterval));
        }

        private onUpdateAirplaneStates() {
            this.$http.get("/api/airplanes").then((response: ng.IHttpPromiseCallbackArg<AirplaneState[]>) => {
                this.$scope.AirplaneStates = response.data;
            });
            
            // TODO: some error indication if the data is stale and cannot be refreshed
        }
    }
}