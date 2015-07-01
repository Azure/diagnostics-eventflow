/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />
/// <reference path="AirplaneDepictionFactory.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    (function () {
        var app = angular.module('AtcAppDirectives', []);
        var seattleLocation = new Microsoft.Maps.Location(47.610, -122.232);
        app.directive('bingMap', function () {
            return {
                restrict: 'AC',
                link: function (scope, instanceElement, instanceAttributes, controller, transclude) {
                    function onViewChanged() {
                        var currentZoom = scope.Map.getZoom();
                        console.log("Current zoom is %f", currentZoom);
                        scope.Map.entities.clear();
                        var airplaneDepiction = AirTrafficControl.AirplaneDepictionFactory.GetAirplaneDepiction(scope.Map, seattleLocation, 0.0, currentZoom);
                        scope.Map.entities.push(airplaneDepiction);
                    }
                    scope.Map = new Microsoft.Maps.Map(instanceElement[0], {
                        credentials: instanceAttributes["bingMapsKey"],
                        zoom: 10,
                        disableZooming: true,
                        center: seattleLocation
                    });
                    Microsoft.Maps.Events.addHandler(scope.Map, 'viewchangeend', onViewChanged);
                    scope.$watch("AirplaneStates", function (newAirplaneStates, oldAirplaneStates, scope) {
                        var map = scope.Map;
                        // map.entities.clear();
                        // TODO update airplane positions based on what is in scope.AirplaneStates
                    });
                }
            };
        });
    })();
})(AirTrafficControl || (AirTrafficControl = {}));
