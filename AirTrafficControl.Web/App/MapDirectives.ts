
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />
/// <reference path="AirplaneDepictionFactory.ts" />

module AirTrafficControl {
    import Maps = Microsoft.Maps;

    (function () {
        var app = angular.module('AtcAppDirectives', []);
        var seattleLocation = new Microsoft.Maps.Location(47.0, -120.5)

        app.directive('bingMap',(): ng.IDirective => {

            return {
                restrict: 'AC',

                link: (
                    scope: IMainControllerScope,
                    instanceElement: ng.IAugmentedJQuery,
                    instanceAttributes: ng.IAttributes,
                    controller: any,
                    transclude: ng.ITranscludeFunction
                ) => {

                    function onViewChanged() {
                        scope.Map.entities.clear();

                        for (var i = 0; i < scope.AirplaneStates.length; i++) {
                            var airplaneState = scope.AirplaneStates[i];
                            var location = new Maps.Location(airplaneState.Location.Latitude, airplaneState.Location.Longitude, airplaneState.Location.Altitude);
                            
                            var airplaneDepiction = AirplaneDepictionFactory.GetAirplaneDepiction(scope.Map, location, airplaneState.Heading);
                            scope.Map.entities.push(airplaneDepiction);
                        }                        
                    }

                    scope.Map = new Maps.Map(instanceElement[0], {
                        credentials: instanceAttributes["bingMapsKey"],
                        zoom: 7,
                        disableZooming: true,
                        center: seattleLocation
                    });

                    Maps.Events.addHandler(scope.Map, 'viewchangeend', onViewChanged);
                    
                    scope.$watch("AirplaneStates",(newAirplaneStates, oldAirplaneStates, scope: IMainControllerScope) => {
                        onViewChanged();
                    });
                }
            };
        });
    })();
}