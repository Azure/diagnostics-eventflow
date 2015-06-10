
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="AirplaneState.ts" />

module AirTrafficControl {
    interface IMainControllerScope extends ng.IScope {
        AirplaneStates: AirplaneState[];
    }

    export class MainController {
        private updateInterval: ng.IPromise<any>;

        constructor(private $scope: IMainControllerScope, private $interval: ng.IIntervalService) {
            $scope.AirplaneStates = [];

            this.updateInterval = $interval(() => this.onUpdateAirplaneStates(), 2000);

            $scope.$on('$destroy',() => $interval.cancel(this.updateInterval));
        }

        private onUpdateAirplaneStates() {
            this.$scope.AirplaneStates = [new AirplaneState("489Y", "Flying. Time is " + new Date().toISOString())];            
        }
    }
}