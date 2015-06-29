/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    (function () {
        var app = angular.module('AtcAppDirectives', []);
        var seattleLocation = new Microsoft.Maps.Location(47.610, -122.232);
        app.directive('bingMap', function () {
            return {
                restrict: 'AC',
                link: function (scope, instanceElement, instanceAttributes, controller, transclude) {
                    var onAirplaneStateChanged = function () {
                    };
                    scope.Map = new Microsoft.Maps.Map(instanceElement[0], {
                        credentials: instanceAttributes["bingMapsKey"],
                        zoom: 10,
                        disableZooming: true,
                        center: seattleLocation
                    });
                    var options = { strokeColor: new Microsoft.Maps.Color(255, 0, 0, 255), strokeThickness: 3 };
                    var polyline = new Microsoft.Maps.Polyline([
                        new Microsoft.Maps.Location(seattleLocation.latitude - 0.1, seattleLocation.longitude - 0.1),
                        new Microsoft.Maps.Location(seattleLocation.latitude + 0.1, seattleLocation.longitude - 0.1),
                        new Microsoft.Maps.Location(seattleLocation.latitude + 0.1, seattleLocation.longitude),
                        new Microsoft.Maps.Location(seattleLocation.latitude - 0.1, seattleLocation.longitude),
                        new Microsoft.Maps.Location(seattleLocation.latitude - 0.1, seattleLocation.longitude + 0.1),
                        new Microsoft.Maps.Location(seattleLocation.latitude + 0.1, seattleLocation.longitude + 0.1)
                    ], options);
                    scope.Map.entities.push(polyline);
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
