/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
var AirTrafficControl;
(function (AirTrafficControl) {
    (function () {
        var app = angular.module('AtcApp', ['AtcAppDirectives', 'SignalR']);
        app.controller("MainController", ["$scope", "$interval", "$http", "Hub", AirTrafficControl.MainController]);
    })();
})(AirTrafficControl || (AirTrafficControl = {}));
