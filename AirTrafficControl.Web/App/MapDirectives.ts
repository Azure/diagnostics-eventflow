
/// <reference path="../Scripts/Typings/angularjs/angularjs.d.ts" /> 
/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
/// <reference path="MainController.ts" />
/// <reference path="AirplaneDepictionFactory.ts" />

module AirTrafficControl {
    (function () {
        var app = angular.module('AtcAppDirectives', []);
        var seattleLocation = new Microsoft.Maps.Location(47.610, -122.232)

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
                        var currentZoom = scope.Map.getZoom();
                        console.log("Current zoom is %f", currentZoom);

                        scope.Map.entities.clear();
                        var airplaneDepiction = AirplaneDepictionFactory.GetAirplaneDepiction(seattleLocation, new Direction(0, 1), currentZoom);
                        scope.Map.entities.push(airplaneDepiction);
                    }

                    scope.Map = new Microsoft.Maps.Map(instanceElement[0], {
                        credentials: instanceAttributes["bingMapsKey"],
                        zoom: 10,
                        disableZooming: true,
                        center: seattleLocation
                    });

                    Microsoft.Maps.Events.addHandler(scope.Map, 'viewchangeend', onViewChanged);
                    
                    scope.$watch("AirplaneStates",(newAirplaneStates, oldAirplaneStates, scope: IMainControllerScope) => {
                        
                        var map = scope.Map;
                        // map.entities.clear();

                        // TODO update airplane positions based on what is in scope.AirplaneStates
                    });
                }
            };
        });
    })();
}