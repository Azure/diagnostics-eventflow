/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />
/// <reference path="AirplaneDepictionFactory.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var Maps = Microsoft.Maps;
    (function () {
        var app = angular.module('AtcAppDirectives', []);
        var seattleLocation = new Microsoft.Maps.Location(47.610, -122.232);
        app.directive('bingMap', function () {
            return {
                restrict: 'AC',
                link: function (scope, instanceElement, instanceAttributes, controller, transclude) {
                    function onViewChanged() {
                        scope.Map.entities.clear();
                        for (var i = 0; i < scope.AirplaneStates.length; i++) {
                            var airplaneState = scope.AirplaneStates[i];
                            var location = new Maps.Location(airplaneState.Location.Latitude, airplaneState.Location.Longitude, airplaneState.Location.Altitude);
                            var airplaneDepiction = AirTrafficControl.AirplaneDepictionFactory.GetAirplaneDepiction(scope.Map, location, airplaneState.Heading);
                            scope.Map.entities.push(airplaneDepiction);
                        }
                    }
                    scope.Map = new Maps.Map(instanceElement[0], {
                        credentials: instanceAttributes["bingMapsKey"],
                        zoom: 10,
                        disableZooming: true,
                        center: seattleLocation
                    });
                    Maps.Events.addHandler(scope.Map, 'viewchangeend', onViewChanged);
                    scope.$watch("AirplaneStates", function (newAirplaneStates, oldAirplaneStates, scope) {
                        onViewChanged();
                    });
                }
            };
        });
    })();
})(AirTrafficControl || (AirTrafficControl = {}));
