/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />
/// <reference path="AirplaneDepictionFactory.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var Maps = Microsoft.Maps;
    (function () {
        var app = angular.module('AtcAppDirectives', []);
        var mapCenter = new Microsoft.Maps.Location(45.4, -117.0);
        var airplaneDepictionFactory = new AirTrafficControl.AirplaneDepictionFactory();
        app.directive('bingMap', function () {
            return {
                restrict: 'AC',
                link: function (scope, instanceElement, instanceAttributes, controller, transclude) {
                    function onViewChanged() {
                        scope.Map.entities.clear();
                        for (var i = 0; i < scope.AirplaneStates.length; i++) {
                            var airplaneState = scope.AirplaneStates[i];
                            var latitude;
                            var longitude;
                            if (airplaneState.EnrouteTo && airplaneState.EnrouteFrom) {
                                latitude = airplaneState.EnrouteFrom.Location.Latitude + (airplaneState.EnrouteTo.Latitude - airplaneState.EnrouteFrom.Latitude) * scope.AnimationProgress;
                                longitude = airplaneState.EnrouteFrom.Longitude + (airplaneState.EnrouteTo.Longitude - airplaneState.EnrouteFrom.Longitude) * scope.AnimationProgress;
                            }
                            else {
                                latitude = airplaneState.Location.Latitude;
                                longitude = airplaneState.Location.Longitude;
                            }
                            var location = new Maps.Location(latitude, longitude, airplaneState.Location.Altitude);
                            var airplaneDepiction = airplaneDepictionFactory.GetAirplaneDepiction(scope.Map, location, airplaneState.Heading, airplaneState.ID);
                            scope.Map.entities.push(airplaneDepiction);
                        }
                    }
                    scope.Map = new Maps.Map(instanceElement[0], {
                        credentials: instanceAttributes["bingMapsKey"],
                        zoom: 6,
                        disableZooming: true,
                        center: mapCenter
                    });
                    Maps.Events.addHandler(scope.Map, 'viewchangeend', onViewChanged);
                    scope.$watch("AirplaneStates", function (newAirplaneStates, oldAirplaneStates, scope) {
                        onViewChanged();
                    });
                    scope.$watch("AnimationProgress", function (newAnimationProgress, oldAnimationProgress, scope) {
                        onViewChanged();
                    });
                }
            };
        });
    })();
})(AirTrafficControl || (AirTrafficControl = {}));
