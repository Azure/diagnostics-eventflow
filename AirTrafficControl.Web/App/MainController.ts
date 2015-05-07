
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="AirplaneState.ts" />

module AirTrafficControl {
    export class MainController {
        public AirplaneStates: AirplaneState[];

        constructor(private $scope: ng.IScope) {
            this.AirplaneStates = [];

            this.AirplaneStates.push(new AirplaneState("489Y", "Flying from Seattle to Portland"));
            this.AirplaneStates.push(new AirplaneState("705JA", "Flying from Spokane to Moses Lake"));
        }        
    }
}