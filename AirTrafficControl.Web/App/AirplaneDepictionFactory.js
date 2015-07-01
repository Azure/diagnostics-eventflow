/// <reference path="../Scripts/Typings/bingmaps/Microsoft.Maps.d.ts" />
var AirTrafficControl;
(function (AirTrafficControl) {
    var Maps = Microsoft.Maps;
    var AirplaneDepictionFactory = (function () {
        function AirplaneDepictionFactory() {
        }
        AirplaneDepictionFactory.GetAirplaneOutline = function () {
            var retval = [];
            var nose = new Maps.Point(0, 180);
            retval.push(nose);
            var rCockpit = new Maps.Point(20, 140);
            retval.push(rCockpit);
            var rWingRoot = new Maps.Point(20, 60);
            retval.push(rWingRoot);
            var rWingFwdTip = new Maps.Point(280, 0);
            retval.push(rWingFwdTip);
            var rWingBackTip = new Maps.Point(280, -30);
            retval.push(rWingBackTip);
            var rWingRootBack = new Maps.Point(20, -20);
            retval.push(rWingRootBack);
            var rHorizontalStabilizerRoot = new Maps.Point(20, -140);
            retval.push(rHorizontalStabilizerRoot);
            var rHorizontalStabilizerFwdTip = new Maps.Point(100, -150);
            retval.push(rHorizontalStabilizerFwdTip);
            var rHorizontalStabilizerBackTip = new Maps.Point(100, -170);
            retval.push(rHorizontalStabilizerBackTip);
            var tail = new Maps.Point(0, -160);
            retval.push(tail);
            var lHorizontalStabilizerBackTip = new Maps.Point(-100, -170);
            retval.push(lHorizontalStabilizerBackTip);
            var lHorizontalStabilizerFwdTip = new Maps.Point(-100, -150);
            retval.push(lHorizontalStabilizerFwdTip);
            var lHorizontalStabilizerRoot = new Maps.Point(-20, -140);
            retval.push(lHorizontalStabilizerRoot);
            var lWingRootBack = new Maps.Point(-20, -20);
            retval.push(lWingRootBack);
            var lWingBackTip = new Maps.Point(-280, -30);
            retval.push(lWingBackTip);
            var lWingFwdTip = new Maps.Point(-280, 0);
            retval.push(lWingFwdTip);
            var lWingRoot = new Maps.Point(-20, 60);
            retval.push(lWingRoot);
            var lCockpit = new Maps.Point(-20, 140);
            retval.push(lCockpit);
            return retval;
        };
        AirplaneDepictionFactory.GetAirplaneDepiction = function (map, location, heading, currentMapZoom) {
            var collectionOptions = { bubble: true, visible: true, zIndex: 100 };
            var collection = new Maps.EntityCollection(collectionOptions);
            var airplaneOutlinePoints = AirplaneDepictionFactory.GetAirplaneOutline();
            var vertices = [];
            var scalingLonFactor = map.getBounds().width / AirplaneDepictionFactory.ScalingLongitudeFactor;
            var scalingLatFactor = map.getBounds().height / AirplaneDepictionFactory.ScalingLattitudeFactor;
            for (var i = 0; i < airplaneOutlinePoints.length; i++) {
                vertices.push(new Maps.Location(location.latitude + airplaneOutlinePoints[i].y * scalingLatFactor, location.longitude + airplaneOutlinePoints[i].x * scalingLonFactor));
            }
            var options = { strokeColor: new Microsoft.Maps.Color(255, 0, 0, 255), strokeThickness: 3 };
            var polygon = new Maps.Polygon(vertices, options);
            // TODO: add Infobox with airplane id
            collection.push(polygon);
            return collection;
        };
        AirplaneDepictionFactory.ScalingLattitudeFactor = 5000.0;
        AirplaneDepictionFactory.ScalingLongitudeFactor = 4000.0;
        return AirplaneDepictionFactory;
    })();
    AirTrafficControl.AirplaneDepictionFactory = AirplaneDepictionFactory;
})(AirTrafficControl || (AirTrafficControl = {}));
