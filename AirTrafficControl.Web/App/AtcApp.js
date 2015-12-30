/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
var AirTrafficControl;
(function (AirTrafficControl) {
    (function () {
        var app = angular.module('AtcApp', ['AtcAppDirectives']);
        app.controller("MainController", ["$scope", "$interval", "$http", "SignalR", AirTrafficControl.MainController]);
    })();
})(AirTrafficControl || (AirTrafficControl = {}));
